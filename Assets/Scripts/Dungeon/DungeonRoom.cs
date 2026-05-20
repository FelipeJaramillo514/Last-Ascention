using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DungeonRoom : MonoBehaviour
{
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

        if (doorCoroutines[doorIndex] != null)
        {
            StopCoroutine(doorCoroutines[doorIndex]);
            doorCoroutines[doorIndex] = null;
        }

        BoxCollider2D solidCollider = doorObject.GetComponent<BoxCollider2D>();
        Vector3 targetPosition = open ? openDoorLocalPositions[doorIndex] : closedDoorLocalPositions[doorIndex];
        if (!open && solidCollider != null)
        {
            solidCollider.enabled = true;
        }

        if (immediate)
        {
            doorObject.transform.localPosition = targetPosition;
            if (solidCollider != null)
            {
                solidCollider.enabled = !open;
            }
            return;
        }

        if (open && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCue(AudioCueId.DoorOpen, doorObject.transform.position, 0.8f, 1f, true, 0.85f);
        }

        doorCoroutines[doorIndex] = StartCoroutine(AnimateDoorRoutine(doorIndex, doorObject.transform, targetPosition, solidCollider, open));
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
        doorCoroutines[doorIndex] = null;
    }
}
