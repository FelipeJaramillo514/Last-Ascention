using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShadowExtractionSystem : MonoBehaviour
{
    [Header("Necromancy")]
    [SerializeField] private bool requireNecromancyUnlock;
    [SerializeField] private int maxShadows = 3;
    [SerializeField] private int maxStoredSouls = 12;
    [SerializeField] private float extractRange = 2.75f;
    [SerializeField] private float holdDuration = 1.15f;
    [SerializeField] private GameObject shadowSoldierPrefab;

    [Header("UI")]
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform progressRoot;
    [SerializeField] private Image progressFill;
    [SerializeField] private Text promptText;

    [System.Serializable]
    public class ExtractedSoul
    {
        public EnemyData sourceData;
        public Sprite sprite;
        public Color sourceColor = Color.white;
        public Vector2 colliderSize = new Vector2(0.8f, 0.9f);
        public Vector2 colliderOffset;
        public Vector2 facingDirection = Vector2.right;
        public string displayName = "Sombra";

        public ExtractedSoul(EnemyBase sourceEnemy)
        {
            if (sourceEnemy == null)
            {
                return;
            }

            sourceData = sourceEnemy.Data != null ? ScriptableObject.Instantiate(sourceEnemy.Data) : null;
            displayName = sourceData != null && !string.IsNullOrEmpty(sourceData.enemyName) ? sourceData.enemyName : "Enemigo";
            sprite = sourceEnemy.CurrentSprite;
            facingDirection = sourceEnemy.transform.right;

            SpriteRenderer renderer = sourceEnemy.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                sprite = sprite != null ? sprite : renderer.sprite;
                sourceColor = renderer.color;
            }

            CapsuleCollider2D sourceCollider = sourceEnemy.GetComponent<CapsuleCollider2D>();
            if (sourceCollider != null)
            {
                colliderSize = sourceCollider.size;
                colliderOffset = sourceCollider.offset;
            }
        }
    }

    private readonly List<ShadowSoldier> activeShadows = new List<ShadowSoldier>();
    private readonly List<ExtractedSoul> storedSouls = new List<ExtractedSoul>();
    private readonly List<EnemyBase> deadEnemies = new List<EnemyBase>();

    private KaisenController owner;
    private Camera mainCamera;
    private Coroutine extractRoutine;
    private EnemyBase currentCandidate;
    private float holdProgress;
    private Font uiFont;

    public List<ShadowSoldier> ActiveShadows { get { return activeShadows; } }
    public int ActiveShadowCount { get { return activeShadows.Count; } }
    public int StoredSoulCount { get { return storedSouls.Count; } }
    public int MaxShadows
    {
        get
        {
            int baseSlots = Mathf.Max(maxShadows, PersistentData.maxShadowSlots);
            return baseSlots + ((owner != null ? owner.Stats.systemLevel : 1) / 10) * 2;
        }
    }

    private void Awake()
    {
        owner = GetComponent<KaisenController>();
        mainCamera = Camera.main;
        maxShadows = Mathf.Max(3, maxShadows);
        maxStoredSouls = maxStoredSouls > 0 ? maxStoredSouls : 12;
        extractRange = Mathf.Max(2.5f, extractRange);
        holdDuration = Mathf.Clamp(holdDuration, 0.75f, 1.2f);
        EnsureUi();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Subscribe<ShadowDiedEvent>(OnShadowDied);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Unsubscribe<ShadowDiedEvent>(OnShadowDied);
    }

    private void Update()
    {
        CleanupLists();
        if (requireNecromancyUnlock && !PersistentData.shadowExtractionUnlocked)
        {
            HideUi();
            return;
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            TrySummonStoredSoul();
        }

        currentCandidate = FindNearestCandidate();
        if (currentCandidate == null)
        {
            HideUi();
            return;
        }

        if (extractRoutine == null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            extractRoutine = StartCoroutine(ExtractShadow(currentCandidate));
        }

        if (extractRoutine == null)
        {
            holdProgress = 0f;
            UpdateProgressUi(currentCandidate.transform.position, 0f, "MANTEN [E] EXTRAER");
        }
    }

    private void OnEnemyDied(EnemyDiedEvent enemyDiedEvent)
    {
        if (enemyDiedEvent == null || enemyDiedEvent.enemy == null)
        {
            return;
        }

        EnemyBase enemy = enemyDiedEvent.enemy.GetComponent<EnemyBase>();
        if (!CanRaiseEnemy(enemy))
        {
            return;
        }

        if (!deadEnemies.Contains(enemy))
        {
            deadEnemies.Add(enemy);
        }
    }

    private void OnShadowDied(ShadowDiedEvent shadowDiedEvent)
    {
        if (shadowDiedEvent == null || shadowDiedEvent.soldier == null)
        {
            return;
        }

        activeShadows.Remove(shadowDiedEvent.soldier);
        PublishInventoryChanged();
    }

    private IEnumerator ExtractShadow(EnemyBase deadEnemy)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCue(AudioCueId.ShadowExtract, deadEnemy.transform.position, 1f, 1f, false, 0f);
        }

        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayShadowExtraction(deadEnemy.transform.position, owner != null ? owner.transform : transform, holdDuration);
        }

        holdProgress = 0f;
        while (holdProgress < holdDuration)
        {
            if (!CanExtract(deadEnemy) || Keyboard.current == null || !Keyboard.current.eKey.isPressed)
            {
                HideUi();
                holdProgress = 0f;
                extractRoutine = null;
                yield break;
            }

            holdProgress += Time.deltaTime;
            UpdateProgressUi(deadEnemy.transform.position, holdProgress / holdDuration, "LEVANTANDO ALMA");
            yield return null;
        }

        Vector3 extractionPosition = deadEnemy.transform.position;
        DungeonRoom sourceRoom = deadEnemy.GetComponentInParent<DungeonRoom>();
        ExtractedSoul extractedSoul = new ExtractedSoul(deadEnemy);
        if (storedSouls.Count >= maxStoredSouls)
        {
            storedSouls.RemoveAt(0);
        }

        storedSouls.Add(extractedSoul);
        EventBus.Publish(new ShadowExtractedEvent(sourceRoom, (Vector2)extractionPosition, extractedSoul.displayName, storedSouls.Count));
        PublishInventoryChanged();

        if (NotificationSystem.Instance != null)
        {
            NotificationSystem.Instance.ShowNotification("ALMA EXTRAIDA: " + extractedSoul.displayName + " (" + storedSouls.Count + ")", new Color(0.25f, 1f, 0.9f, 1f), 1.4f);
        }

        SpawnExtractionParticles(extractionPosition);
        deadEnemies.Remove(deadEnemy);
        deadEnemy.MarkShadowExtracted();
        HideUi();
        holdProgress = 0f;
        extractRoutine = null;
    }

    private void TrySummonStoredSoul()
    {
        if (storedSouls.Count == 0)
        {
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowNotification("NO HAY ALMAS EXTRAIDAS", new Color(0.7f, 0.95f, 1f, 1f), 1.1f);
            }
            return;
        }

        if (activeShadows.Count >= MaxShadows)
        {
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowNotification("SUBDITOS AL LIMITE " + activeShadows.Count + "/" + MaxShadows, new Color(1f, 0.76f, 0.25f, 1f), 1.1f);
            }
            PublishInventoryChanged();
            return;
        }

        int soulIndex = storedSouls.Count - 1;
        ExtractedSoul soul = storedSouls[soulIndex];
        storedSouls.RemoveAt(soulIndex);

        Vector3 spawnPosition = GetSummonPosition();
        ShadowSoldier soldier = SpawnShadowSoldier(soul, spawnPosition);
        if (soldier == null)
        {
            storedSouls.Add(soul);
            PublishInventoryChanged();
            return;
        }

        activeShadows.Add(soldier);
        EventBus.Publish(new ShadowSummonedEvent(soldier, null, (Vector2)spawnPosition));
        PublishInventoryChanged();

        if (NotificationSystem.Instance != null)
        {
            NotificationSystem.Instance.ShowNotification("SOMBRA INVOCADA: " + soul.displayName, new Color(0.15f, 0.88f, 1f, 1f), 1.25f);
        }
    }

    private Vector3 GetSummonPosition()
    {
        Vector2 origin = owner != null ? (Vector2)owner.transform.position : (Vector2)transform.position;
        Vector2 direction = owner != null && owner.AimDirection.sqrMagnitude > 0.001f ? owner.AimDirection.normalized : Vector2.right;
        Vector2 sideOffset = Vector2.Perpendicular(direction) * Random.Range(-0.35f, 0.35f);
        return origin + direction * 1.35f + sideOffset;
    }

    private ShadowSoldier SpawnShadowSoldier(ExtractedSoul soul, Vector3 spawnPosition)
    {
        GameObject shadowObject = shadowSoldierPrefab != null ? Instantiate(shadowSoldierPrefab, spawnPosition, Quaternion.identity) : new GameObject("ShadowSoldier");
        ShadowSoldier soldier = shadowObject.GetComponent<ShadowSoldier>();
        if (soldier == null)
        {
            soldier = shadowObject.AddComponent<ShadowSoldier>();
        }

        float orbitOffset = activeShadows.Count * 90f;
        soldier.InitializeFromSoul(soul, owner, orbitOffset, spawnPosition);
        return soldier;
    }

    private EnemyBase FindNearestCandidate()
    {
        EnemyBase nearest = null;
        float nearestDistance = float.MaxValue;
        for (int i = deadEnemies.Count - 1; i >= 0; i--)
        {
            EnemyBase enemy = deadEnemies[i];
            if (!CanExtract(enemy))
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private bool CanExtract(EnemyBase enemy)
    {
        return enemy != null
            && enemy.IsAvailableForShadowExtraction
            && Vector2.Distance(transform.position, enemy.transform.position) <= extractRange;
    }

    private bool CanRaiseEnemy(EnemyBase enemy)
    {
        return enemy != null
            && enemy.Data != null
            && enemy.Data.isExtractable
            && enemy.GetComponent<BossBase>() == null;
    }

    private void CleanupLists()
    {
        for (int i = deadEnemies.Count - 1; i >= 0; i--)
        {
            EnemyBase enemy = deadEnemies[i];
            if (enemy == null || !enemy.IsAvailableForShadowExtraction)
            {
                deadEnemies.RemoveAt(i);
            }
        }

        for (int i = activeShadows.Count - 1; i >= 0; i--)
        {
            if (activeShadows[i] == null)
            {
                activeShadows.RemoveAt(i);
            }
        }
    }

    private void PublishInventoryChanged()
    {
        EventBus.Publish(new ShadowInventoryChangedEvent(storedSouls.Count, activeShadows.Count, MaxShadows));
    }

    private void SpawnExtractionParticles(Vector3 worldPosition)
    {
        GameObject particleObject = new GameObject("ShadowExtractParticles");
        particleObject.transform.position = worldPosition;
        ParticleSystem particleSystem = particleObject.AddComponent<ParticleSystem>();
        var main = particleSystem.main;
        main.duration = 0.45f;
        main.startLifetime = 0.5f;
        main.startSpeed = 2f;
        main.startSize = 0.14f;
        main.startColor = new Color(0.05f, 0.95f, 0.9f, 0.95f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = particleSystem.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 22) });
        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 0.15f;
        particleSystem.Play();
        Destroy(particleObject, 1.5f);
    }

    private void UpdateProgressUi(Vector3 worldPosition, float fillAmount, string label)
    {
        EnsureUi();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition + Vector3.up * 1.1f);
        progressRoot.gameObject.SetActive(true);
        progressRoot.position = screenPosition;
        progressFill.fillAmount = Mathf.Clamp01(fillAmount);
        promptText.text = label;
    }

    private void HideUi()
    {
        if (progressRoot != null)
        {
            progressRoot.gameObject.SetActive(false);
        }
    }

    private void EnsureUi()
    {
        if (overlayCanvas != null && progressRoot != null && progressFill != null && promptText != null)
        {
            return;
        }

        uiFont = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Sprite whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));

        Canvas hostCanvas = FindFirstObjectByType<Canvas>();
        if (hostCanvas == null)
        {
            GameObject canvasObject = new GameObject("HUDCanvas", typeof(RectTransform));
            hostCanvas = canvasObject.AddComponent<Canvas>();
            hostCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        Transform overlay = hostCanvas.transform.Find("ShadowExtractOverlay");
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject("ShadowExtractOverlay", typeof(RectTransform));
            overlay = overlayObject.transform;
            overlay.SetParent(hostCanvas.transform, false);
        }

        overlayCanvas = overlay.GetComponent<Canvas>();
        if (overlayCanvas == null)
        {
            overlayCanvas = overlay.gameObject.AddComponent<Canvas>();
        }
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 135;

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Transform root = overlay.Find("ProgressRoot");
        if (root == null)
        {
            GameObject rootObject = new GameObject("ProgressRoot", typeof(RectTransform));
            root = rootObject.transform;
            root.SetParent(overlay, false);
        }

        progressRoot = root as RectTransform;
        progressRoot.sizeDelta = new Vector2(96f, 96f);
        progressRoot.gameObject.SetActive(false);

        Transform fillTransform = progressRoot.Find("Fill");
        if (fillTransform == null)
        {
            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillTransform = fillObject.transform;
            fillTransform.SetParent(progressRoot, false);
        }

        progressFill = fillTransform.GetComponent<Image>();
        RectTransform fillRect = progressFill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        progressFill.sprite = whiteSprite;
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Radial360;
        progressFill.fillOrigin = 2;
        progressFill.color = new Color(0f, 0.9f, 1f, 0.85f);
        progressFill.fillAmount = 0f;

        Transform textTransform = progressRoot.Find("Prompt");
        if (textTransform == null)
        {
            GameObject textObject = new GameObject("Prompt", typeof(RectTransform), typeof(Text));
            textTransform = textObject.transform;
            textTransform.SetParent(progressRoot, false);
        }

        promptText = textTransform.GetComponent<Text>();
        RectTransform textRect = promptText.rectTransform;
        textRect.anchorMin = new Vector2(-0.5f, -0.45f);
        textRect.anchorMax = new Vector2(1.5f, 0f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        promptText.font = uiFont;
        promptText.fontSize = 18;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = new Color(0.7f, 1f, 0.92f, 1f);
    }
}
