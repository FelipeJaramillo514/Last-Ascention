using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DungeonRoom : MonoBehaviour
{
    private const string DoorSpriteResourcePath = "DungeonDoors/door_sprite";
    private const int DoorSpriteRows = 4;
    private const int DoorSpriteColumns = 6;
    private const float DoorSpritePixelsPerUnit = 160f;
    private const float DoorSpriteAnimationDuration = 0.42f;
    private const string DoorVisualChildName = "DoorSpriteVisual";
    private const string EntryOrangePowerResourcePath = "Weapons/PoderNaranja";
    private const string EntryOrangePowerPickupName = "StartingOrangePower";

    private static Sprite[][] doorAnimationFrames;
    private static bool doorAnimationLoadAttempted;

    [SerializeField] private RoomData data;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private List<Transform> propPoints = new List<Transform>();
    [SerializeField] private Transform[] doorPoints = new Transform[4];
    [SerializeField] private GameObject[] doorObjects = new GameObject[4];
    [SerializeField] private bool isCleared;
    [SerializeField] private bool isVisited;

    private readonly HashSet<GameObject> aliveEnemies = new HashSet<GameObject>();
    private readonly List<GameObject> spawnedProps = new List<GameObject>();
    private readonly List<EnemyData> cachedWaveEnemies = new List<EnemyData>();
    private readonly bool[] activeDoors = new bool[4];
    private readonly Vector3[] closedDoorLocalPositions = new Vector3[4];
    private readonly Vector3[] openDoorLocalPositions = new Vector3[4];
    private readonly Coroutine[] doorCoroutines = new Coroutine[4];

    private DungeonBuilder builder;
    private BoxCollider2D roomBounds;
    private RoomType runtimeRoomType;
    private Vector2Int gridPosition;
    private int roomIndex;
    private bool ambushPending;
    private bool ambushTriggered;
    private int activeEnemyCount;

    public RoomData Data { get { return data; } }
    public bool IsCleared { get { return isCleared; } }
    public bool IsVisited { get { return isVisited; } }
    public bool IsCombatLocked { get { return !isCleared && activeEnemyCount > 0; } }
    public RoomType RuntimeRoomType { get { return runtimeRoomType; } }
    public Vector2Int GridPosition { get { return gridPosition; } }
    public int RoomIndex { get { return roomIndex; } }
    public int SpawnPointCount { get { return spawnPoints != null ? spawnPoints.Count : 0; } }

    private void Awake()
    {
        roomBounds = GetComponent<BoxCollider2D>();
        roomBounds.isTrigger = true;

        runtimeRoomType = data != null ? data.roomType : RoomType.Normal;
        CacheDoorPositions();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    public void ConfigureRuntime(DungeonBuilder ownerBuilder, DungeonLayout.RoomNode node)
    {
        builder = ownerBuilder;
        runtimeRoomType = node.type;
        gridPosition = node.gridPos;
        roomIndex = node.roomIndex;
    }

    public void Initialize(List<EnemyData> enemiesToSpawn)
    {
        aliveEnemies.Clear();
        activeEnemyCount = 0;
        ambushTriggered = false;
        ambushPending = false;
        cachedWaveEnemies.Clear();
        isCleared = false;

        ClearSpawnedProps();
        SpawnProps();
        EnsureEntryPowerPickups();

        if (enemiesToSpawn != null)
        {
            cachedWaveEnemies.AddRange(enemiesToSpawn);
        }

        SpawnEnemyWave(enemiesToSpawn);
        ambushPending = data != null && data.canHaveAmbush && activeEnemyCount > 0 && Random.value <= 0.2f;

        if (activeEnemyCount == 0)
        {
            isCleared = true;
        }

        SetDoorsOpenImmediate(true);
    }

    public void ActivateDoors(bool north, bool south, bool east, bool west)
    {
        bool[] values = { north, south, east, west };
        for (int i = 0; i < activeDoors.Length; i++)
        {
            activeDoors[i] = i < values.Length && values[i];
            if (doorObjects == null || i >= doorObjects.Length || doorObjects[i] == null)
            {
                continue;
            }

            doorObjects[i].SetActive(activeDoors[i]);
            DoorTrigger trigger = doorObjects[i].GetComponentInChildren<DoorTrigger>(true);
            if (trigger != null)
            {
                trigger.gameObject.SetActive(activeDoors[i]);
                trigger.SetTraversalEnabled(activeDoors[i]);
            }
        }

        SetDoorsOpenImmediate(true);
    }

    public void ConfigureDoorLink(int doorIndex, DungeonRoom destinationRoom, int destinationDoorIndex)
    {
        if (doorObjects == null || doorIndex < 0 || doorIndex >= doorObjects.Length || doorObjects[doorIndex] == null)
        {
            return;
        }

        DoorTrigger trigger = doorObjects[doorIndex].GetComponentInChildren<DoorTrigger>(true);
        if (trigger != null)
        {
            trigger.Configure(this, destinationRoom, doorIndex, destinationDoorIndex);
        }
    }

    public void HandlePlayerEntered()
    {
        isVisited = true;
        if (!isCleared && activeEnemyCount > 0)
        {
            CloseActiveDoors();
        }
        else
        {
            OpenActiveDoors();
        }
    }

    public void MarkVisited()
    {
        isVisited = true;
    }

    public void SetRuntimeRoomType(RoomType roomType)
    {
        runtimeRoomType = roomType;
    }

    public Vector2 GetRoomCenter()
    {
        return roomBounds != null ? roomBounds.bounds.center : (Vector2)transform.position;
    }

    public bool ContainsPoint(Vector2 point)
    {
        return roomBounds != null && roomBounds.OverlapPoint(point);
    }

    public Vector2 GetDoorArrivalPosition(int doorIndex)
    {
        if (doorPoints == null || doorIndex < 0 || doorIndex >= doorPoints.Length || doorPoints[doorIndex] == null)
        {
            return GetRoomCenter();
        }

        Vector2 inwardOffset = Vector2.zero;
        if (doorIndex == 0) inwardOffset = Vector2.down * 1.5f;
        else if (doorIndex == 1) inwardOffset = Vector2.up * 1.5f;
        else if (doorIndex == 2) inwardOffset = Vector2.left * 1.5f;
        else if (doorIndex == 3) inwardOffset = Vector2.right * 1.5f;

        return (Vector2)doorPoints[doorIndex].position + inwardOffset;
    }

    public void CloseActiveDoors()
    {
        for (int i = 0; i < activeDoors.Length; i++)
        {
            if (activeDoors[i])
            {
                SetDoorOpenState(i, false, false);
            }
        }
    }

    public void OpenActiveDoors()
    {
        for (int i = 0; i < activeDoors.Length; i++)
        {
            if (activeDoors[i])
            {
                SetDoorOpenState(i, true, false);
            }
        }
    }

    private void OnEnemyDied(EnemyDiedEvent enemyDiedEvent)
    {
        if (enemyDiedEvent == null || enemyDiedEvent.enemy == null)
        {
            return;
        }

        if (!aliveEnemies.Remove(enemyDiedEvent.enemy))
        {
            return;
        }

        activeEnemyCount = Mathf.Max(0, activeEnemyCount - 1);
        if (activeEnemyCount > 0)
        {
            return;
        }

        if (ambushPending && !ambushTriggered)
        {
            ambushTriggered = true;
            ambushPending = false;
            StartCoroutine(SpawnAmbushRoutine());
            return;
        }

        OnRoomCleared();
    }

    private IEnumerator SpawnAmbushRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        List<EnemyData> ambushWave = BuildAmbushWave();
        if (ambushWave.Count == 0)
        {
            OnRoomCleared();
            yield break;
        }

        SpawnEnemyWave(ambushWave);
        if (activeEnemyCount == 0)
        {
            OnRoomCleared();
        }
        else
        {
            CloseActiveDoors();
        }
    }

    private List<EnemyData> BuildAmbushWave()
    {
        List<EnemyData> result = new List<EnemyData>();
        if (cachedWaveEnemies.Count == 0)
        {
            return result;
        }

        int targetCount = Mathf.Clamp(Mathf.CeilToInt(cachedWaveEnemies.Count * 0.5f), 1, spawnPoints.Count > 0 ? spawnPoints.Count : cachedWaveEnemies.Count);
        List<EnemyData> pool = new List<EnemyData>(cachedWaveEnemies);
        while (result.Count < targetCount && pool.Count > 0)
        {
            int pick = Random.Range(0, pool.Count);
            result.Add(pool[pick]);
            pool.RemoveAt(pick);
        }

        return result;
    }

    private void SpawnEnemyWave(List<EnemyData> enemiesToSpawn)
    {
        if (enemiesToSpawn == null || enemiesToSpawn.Count == 0 || spawnPoints == null || spawnPoints.Count == 0)
        {
            return;
        }

        DungeonBuilder owner = builder != null ? builder : DungeonBuilder.Instance;
        GameBalanceData balance = GameBalanceData.Instance;
        int floorNumber = owner != null ? owner.CurrentFloorNumber : 1;
        int spawnCount = Mathf.Min(enemiesToSpawn.Count, spawnPoints.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            EnemyData enemyData = enemiesToSpawn[i];
            GameObject prefab = owner != null ? owner.ResolveEnemyPrefab(enemyData) : null;
            if (prefab == null || enemyData == null)
            {
                continue;
            }

            Transform spawnPoint = spawnPoints[i % spawnPoints.Count];
            GameObject enemyObject = Instantiate(prefab, spawnPoint.position, Quaternion.identity, transform);
            EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                EnemyData runtimeData = ScriptableObject.Instantiate(enemyData);
                float difficulty = balance.EvaluateDifficulty(floorNumber);
                runtimeData.damage = balance.GetScaledEnemyDamage(enemyData.damage, floorNumber);
                runtimeData.moveSpeed = enemyData.moveSpeed * Mathf.Lerp(1f, difficulty, 0.25f);
                if (runtimeData.enemyType == EnemyType.StoneGiant)
                {
                    runtimeData.maxHP = balance.GetScaledBossHP(enemyData.maxHP, floorNumber);
                }
                else
                {
                    runtimeData.maxHP = enemyData.maxHP * difficulty;
                }

                enemy.ApplyData(runtimeData);
            }

            aliveEnemies.Add(enemyObject);
            activeEnemyCount++;
        }
    }

    private void SpawnProps()
    {
        if (data == null || data.possiblePropPrefabs == null || data.possiblePropPrefabs.Length == 0 || propPoints == null)
        {
            return;
        }

        for (int i = 0; i < propPoints.Count; i++)
        {
            Transform point = propPoints[i];
            if (point == null || Random.value > 0.45f)
            {
                continue;
            }

            GameObject prefab = data.possiblePropPrefabs[Random.Range(0, data.possiblePropPrefabs.Length)];
            if (prefab == null)
            {
                continue;
            }

            GameObject prop = Instantiate(prefab, point.position, Quaternion.identity, transform);
            spawnedProps.Add(prop);
        }
    }

    private void EnsureEntryPowerPickups()
    {
        if (runtimeRoomType != RoomType.Entry || transform.Find(EntryOrangePowerPickupName) != null)
        {
            return;
        }

        WeaponData orangePower = Resources.Load<WeaponData>(EntryOrangePowerResourcePath);
        if (orangePower == null)
        {
            return;
        }

        WeaponPickup[] pickups = GetComponentsInChildren<WeaponPickup>(true);
        WeaponPickup template = null;
        for (int i = 0; i < pickups.Length; i++)
        {
            WeaponPickup pickup = pickups[i];
            if (pickup == null)
            {
                continue;
            }

            if (pickup.name == EntryOrangePowerPickupName || pickup.GetWeaponData() == orangePower)
            {
                return;
            }

            if (template == null)
            {
                template = pickup;
            }
        }

        if (template == null)
        {
            return;
        }

        GameObject orangePickupObject = Instantiate(template.gameObject, transform);
        orangePickupObject.name = EntryOrangePowerPickupName;
        orangePickupObject.transform.localPosition = template.transform.localPosition + new Vector3(-4f, 0f, 0f);
        orangePickupObject.transform.localScale = template.transform.localScale;

        WeaponPickup orangePickup = orangePickupObject.GetComponent<WeaponPickup>();
        if (orangePickup != null)
        {
            int startingAmmo = orangePower.maxAmmo >= 0 ? orangePower.maxAmmo : int.MinValue;
            orangePickup.AssignWeaponData(orangePower, startingAmmo);
        }
    }

    private void ClearSpawnedProps()
    {
        for (int i = 0; i < spawnedProps.Count; i++)
        {
            if (spawnedProps[i] != null)
            {
                Destroy(spawnedProps[i]);
            }
        }

        spawnedProps.Clear();
    }

    private void OnRoomCleared()
    {
        if (isCleared)
        {
            return;
        }

        isCleared = true;
        OpenActiveDoors();
        EventBus.Publish(new RoomClearedEvent(this));
    }


    private void CacheDoorPositions()
    {
        if (doorObjects == null)
        {
            return;
        }

        for (int i = 0; i < doorObjects.Length; i++)
        {
            if (doorObjects[i] == null)
            {
                continue;
            }

            closedDoorLocalPositions[i] = doorObjects[i].transform.localPosition;
            openDoorLocalPositions[i] = closedDoorLocalPositions[i] + GetDoorOpenOffset(i);
        }
    }

    private Vector3 GetDoorOpenOffset(int doorIndex)
    {
        if (doorIndex == 0 || doorIndex == 1)
        {
            return new Vector3(1.4f, 0f, 0f);
        }

        return new Vector3(0f, 1.4f, 0f);
    }

    private void SetDoorsOpenImmediate(bool open)
    {
        for (int i = 0; i < activeDoors.Length; i++)
        {
            if (activeDoors[i])
            {
                SetDoorOpenState(i, open, true);
            }
        }
    }

    private void SetDoorOpenState(int doorIndex, bool open, bool immediate)
    {
        if (doorObjects == null || doorIndex < 0 || doorIndex >= doorObjects.Length)
        {
            return;
        }

        GameObject doorObject = doorObjects[doorIndex];
        if (doorObject == null || !doorObject.activeSelf)
        {
            return;
        }

        SetDoorTriggerEnabled(doorIndex, false);
        if (doorCoroutines[doorIndex] != null)
        {
            StopCoroutine(doorCoroutines[doorIndex]);
            doorCoroutines[doorIndex] = null;
        }

        BoxCollider2D solidCollider = doorObject.GetComponent<BoxCollider2D>();
        bool useSpriteAnimation = HasDoorSpriteAnimation(doorIndex);
        Vector3 targetPosition = closedDoorLocalPositions[doorIndex];
        if (!useSpriteAnimation && open)
        {
            targetPosition = openDoorLocalPositions[doorIndex];
        }

        if (!open && solidCollider != null)
        {
            solidCollider.enabled = true;
        }

        if (immediate)
        {
            doorObject.transform.localPosition = targetPosition;
            if (useSpriteAnimation)
            {
                SetDoorSpriteFrame(doorIndex, open);
            }

            if (solidCollider != null)
            {
                solidCollider.enabled = !open;
            }
            SetDoorTriggerEnabled(doorIndex, open);
            return;
        }

        if (open && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCue(AudioCueId.DoorOpen, doorObject.transform.position, 0.8f, 1f, true, 0.85f);
        }

        if (useSpriteAnimation)
        {
            doorCoroutines[doorIndex] = StartCoroutine(AnimateDoorSpriteRoutine(doorIndex, doorObject.transform, solidCollider, open));
            return;
        }

        doorCoroutines[doorIndex] = StartCoroutine(AnimateDoorRoutine(doorIndex, doorObject.transform, targetPosition, solidCollider, open));
    }

    private void SetDoorTriggerEnabled(int doorIndex, bool enabled)
    {
        if (doorObjects == null || doorIndex < 0 || doorIndex >= doorObjects.Length || doorObjects[doorIndex] == null)
        {
            return;
        }

        DoorTrigger trigger = doorObjects[doorIndex].GetComponentInChildren<DoorTrigger>(true);
        if (trigger != null)
        {
            trigger.SetTraversalEnabled(enabled);
        }
    }

    private bool HasDoorSpriteAnimation(int doorIndex)
    {
        if (doorObjects == null || doorIndex < 0 || doorIndex >= doorObjects.Length || doorObjects[doorIndex] == null)
        {
            return false;
        }

        Sprite[] frames = GetDoorAnimationFrames(doorIndex);
        return frames != null && frames.Length > 0 && GetDoorVisualRenderer(doorIndex, true) != null;
    }

    private void SetDoorSpriteFrame(int doorIndex, bool open)
    {
        Sprite[] frames = GetDoorAnimationFrames(doorIndex);
        if (frames == null || frames.Length == 0 || doorObjects == null || doorIndex < 0 || doorIndex >= doorObjects.Length || doorObjects[doorIndex] == null)
        {
            return;
        }

        SpriteRenderer renderer = GetDoorVisualRenderer(doorIndex, true);
        if (renderer == null)
        {
            return;
        }

        renderer.color = Color.white;
        renderer.sprite = open ? frames[0] : frames[frames.Length - 1];
        ApplyDoorVisualLayout(doorIndex, renderer);
    }

    private Sprite[] GetDoorAnimationFrames(int doorIndex)
    {
        EnsureDoorAnimationFrames();
        if (doorAnimationFrames == null || doorIndex < 0 || doorIndex >= doorAnimationFrames.Length)
        {
            return null;
        }

        return doorAnimationFrames[doorIndex];
    }

    private static void EnsureDoorAnimationFrames()
    {
        if (doorAnimationLoadAttempted)
        {
            return;
        }

        doorAnimationLoadAttempted = true;
        Texture2D texture = Resources.Load<Texture2D>(DoorSpriteResourcePath);
        if (texture == null)
        {
            Sprite sourceSprite = Resources.Load<Sprite>(DoorSpriteResourcePath);
            texture = sourceSprite != null ? sourceSprite.texture : null;
        }

        if (texture == null)
        {
            return;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        doorAnimationFrames = new Sprite[DoorSpriteRows][];
        for (int row = 0; row < DoorSpriteRows; row++)
        {
            doorAnimationFrames[row] = new Sprite[DoorSpriteColumns];
            for (int column = 0; column < DoorSpriteColumns; column++)
            {
                int left = Mathf.RoundToInt(column * texture.width / (float)DoorSpriteColumns);
                int right = Mathf.RoundToInt((column + 1) * texture.width / (float)DoorSpriteColumns);
                int top = Mathf.RoundToInt(row * texture.height / (float)DoorSpriteRows);
                int bottom = Mathf.RoundToInt((row + 1) * texture.height / (float)DoorSpriteRows);
                int width = Mathf.Max(1, right - left);
                int height = Mathf.Max(1, bottom - top);
                int unityY = texture.height - bottom;
                Rect rect = new Rect(left, unityY, width, height);
                doorAnimationFrames[row][column] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), DoorSpritePixelsPerUnit);
            }
        }
    }

    private IEnumerator AnimateDoorSpriteRoutine(int doorIndex, Transform doorTransform, BoxCollider2D solidCollider, bool opening)
    {
        Sprite[] frames = GetDoorAnimationFrames(doorIndex);
        SpriteRenderer renderer = GetDoorVisualRenderer(doorIndex, true);
        if (doorTransform != null)
        {
            doorTransform.localPosition = closedDoorLocalPositions[doorIndex];
        }

        if (frames == null || frames.Length == 0 || renderer == null)
        {
            if (doorTransform != null)
            {
                doorTransform.localPosition = opening ? openDoorLocalPositions[doorIndex] : closedDoorLocalPositions[doorIndex];
            }

            if (solidCollider != null)
            {
                solidCollider.enabled = !opening;
            }

            SetDoorTriggerEnabled(doorIndex, opening);
            doorCoroutines[doorIndex] = null;
            yield break;
        }

        renderer.color = Color.white;
        renderer.sprite = opening ? frames[frames.Length - 1] : frames[0];
        ApplyDoorVisualLayout(doorIndex, renderer);
        if (!opening && solidCollider != null)
        {
            solidCollider.enabled = true;
        }

        float elapsed = 0f;
        while (elapsed < DoorSpriteAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / DoorSpriteAnimationDuration);
            int frameIndex = Mathf.RoundToInt(t * (frames.Length - 1));
            if (opening)
            {
                frameIndex = frames.Length - 1 - frameIndex;
            }

            renderer.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)];
            yield return null;
        }

        renderer.sprite = opening ? frames[0] : frames[frames.Length - 1];
        ApplyDoorVisualLayout(doorIndex, renderer);
        if (solidCollider != null)
        {
            solidCollider.enabled = !opening;
        }

        SetDoorTriggerEnabled(doorIndex, opening);
        doorCoroutines[doorIndex] = null;
    }

    private SpriteRenderer GetDoorVisualRenderer(int doorIndex, bool create)
    {
        if (doorObjects == null || doorIndex < 0 || doorIndex >= doorObjects.Length || doorObjects[doorIndex] == null)
        {
            return null;
        }

        Transform doorTransform = doorObjects[doorIndex].transform;
        Transform visualTransform = doorTransform.Find(DoorVisualChildName);
        if (visualTransform == null)
        {
            if (!create)
            {
                return null;
            }

            GameObject visualObject = new GameObject(DoorVisualChildName);
            visualTransform = visualObject.transform;
            visualTransform.SetParent(doorTransform, false);
        }

        SpriteRenderer renderer = visualTransform.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = visualTransform.gameObject.AddComponent<SpriteRenderer>();
        }

        SpriteRenderer sourceRenderer = doorObjects[doorIndex].GetComponent<SpriteRenderer>();
        if (sourceRenderer != null)
        {
            renderer.sortingLayerID = sourceRenderer.sortingLayerID;
            renderer.sortingOrder = sourceRenderer.sortingOrder + 1;
            sourceRenderer.enabled = false;
        }
        else
        {
            renderer.sortingOrder = 3;
        }

        renderer.enabled = true;
        return renderer;
    }

    private void ApplyDoorVisualLayout(int doorIndex, SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null || doorObjects == null || doorIndex < 0 || doorIndex >= doorObjects.Length || doorObjects[doorIndex] == null)
        {
            return;
        }

        Transform visualTransform = renderer.transform;
        Transform doorTransform = doorObjects[doorIndex].transform;
        visualTransform.localPosition = GetDoorVisualOffset(doorIndex);
        visualTransform.localRotation = Quaternion.identity;

        Vector2 targetSize = GetDoorVisualTargetSize(doorIndex);
        Vector3 spriteSize = renderer.sprite.bounds.size;
        float parentScaleX = Mathf.Abs(doorTransform.localScale.x) > 0.001f ? Mathf.Abs(doorTransform.localScale.x) : 1f;
        float parentScaleY = Mathf.Abs(doorTransform.localScale.y) > 0.001f ? Mathf.Abs(doorTransform.localScale.y) : 1f;
        float scaleX = spriteSize.x > 0.001f ? targetSize.x / spriteSize.x / parentScaleX : 1f;
        float scaleY = spriteSize.y > 0.001f ? targetSize.y / spriteSize.y / parentScaleY : 1f;
        visualTransform.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    private Vector3 GetDoorVisualOffset(int doorIndex)
    {
        if (doorIndex == 2)
        {
            return new Vector3(0.28f, 0f, 0f);
        }

        if (doorIndex == 3)
        {
            return new Vector3(-0.28f, 0f, 0f);
        }

        return Vector3.zero;
    }

    private Vector2 GetDoorVisualTargetSize(int doorIndex)
    {
        if (doorIndex == 2 || doorIndex == 3)
        {
            return new Vector2(1.85f, 2.45f);
        }

        return new Vector2(2.65f, 2.25f);
    }

    private IEnumerator AnimateDoorRoutine(int doorIndex, Transform doorTransform, Vector3 targetPosition, BoxCollider2D solidCollider, bool opening)
    {
        Vector3 startPosition = doorTransform.localPosition;
        float elapsed = 0f;
        const float duration = 0.5f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            doorTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        doorTransform.localPosition = targetPosition;
        if (solidCollider != null)
        {
            solidCollider.enabled = !opening;
        }
        SetDoorTriggerEnabled(doorIndex, opening);
        doorCoroutines[doorIndex] = null;
    }
}
