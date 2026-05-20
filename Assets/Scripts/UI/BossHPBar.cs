using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossHPBar : MonoBehaviour
{
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private Image recentDamageFill;
    [SerializeField] private Image healthFill;
    [SerializeField] private Text bossNameText;

    private BossBase currentBoss;
    private Coroutine transitionRoutine;
    private bool isVisible;
    private bool isClosing;
    private float recentDamageDelayTimer;
    private Font uiFont;
    private readonly Vector2 hiddenPosition = new Vector2(0f, 86f);
    private readonly Vector2 shownPosition = new Vector2(0f, -8f);

    private void Awake()
    {
        EnsureUi();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<RoomVisitedEvent>(OnRoomVisited);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<RoomVisitedEvent>(OnRoomVisited);
    }

    private void Update()
    {
        if (currentBoss == null)
        {
            return;
        }

        float targetFill = currentBoss.HealthNormalized;
        if (targetFill < healthFill.fillAmount)
        {
            recentDamageDelayTimer = 0.5f;
        }

        healthFill.fillAmount = targetFill;
        if (recentDamageDelayTimer > 0f)
        {
            recentDamageDelayTimer -= Time.unscaledDeltaTime;
        }
        else
        {
            recentDamageFill.fillAmount = Mathf.MoveTowards(recentDamageFill.fillAmount, targetFill, Time.unscaledDeltaTime * 0.6f);
        }

        if (currentBoss.IsDead && !isClosing)
        {
            StartDeathSequence();
        }
    }

    private void OnRoomVisited(RoomVisitedEvent visitedEvent)
    {
        if (visitedEvent == null || visitedEvent.room == null)
        {
            return;
        }

        if (visitedEvent.room.RuntimeRoomType != RoomType.Boss)
        {
            HideBar();
            return;
        }

        BossBase boss = visitedEvent.room.GetComponentInChildren<BossBase>();
        if (boss == null)
        {
            HideBar();
            return;
        }

        ShowBar(boss);
    }

    private void ShowBar(BossBase boss)
    {
        EnsureUi();
        currentBoss = boss;
        isClosing = false;
        bossNameText.text = boss.BossName;
        healthFill.fillAmount = boss.HealthNormalized;
        recentDamageFill.fillAmount = boss.HealthNormalized;
        recentDamageDelayTimer = 0f;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(SlideRoutine(hiddenPosition, shownPosition, 0f, 1f));
        isVisible = true;
    }

    private void HideBar()
    {
        currentBoss = null;
        isClosing = false;
        if (!isVisible)
        {
            return;
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(SlideRoutine(panelRoot.anchoredPosition, hiddenPosition, panelCanvasGroup.alpha, 0f));
        isVisible = false;
    }

    private void StartDeathSequence()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(DeathSequenceRoutine());
    }

    private IEnumerator DeathSequenceRoutine()
    {
        isClosing = true;
        for (int i = 0; i < 3; i++)
        {
            panelCanvasGroup.alpha = 0.2f;
            yield return new WaitForSecondsRealtime(0.08f);
            panelCanvasGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(0.08f);
        }

        yield return SlideRoutine(panelRoot.anchoredPosition, hiddenPosition, panelCanvasGroup.alpha, 0f);
        currentBoss = null;
        isVisible = false;
        isClosing = false;
        transitionRoutine = null;
    }

    private IEnumerator SlideRoutine(Vector2 startPosition, Vector2 endPosition, float startAlpha, float endAlpha)
    {
        float elapsed = 0f;
        const float duration = 0.3f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            panelRoot.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        panelRoot.anchoredPosition = endPosition;
        panelCanvasGroup.alpha = endAlpha;
        transitionRoutine = null;
    }

    private void EnsureUi()
    {
        if (overlayCanvas != null && panelRoot != null && panelCanvasGroup != null && recentDamageFill != null && healthFill != null && bossNameText != null)
        {
            return;
        }

        uiFont = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Sprite whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));

        Canvas hostCanvas = GetComponent<Canvas>();
        if (hostCanvas == null)
        {
            hostCanvas = FindFirstObjectByType<Canvas>();
        }

        if (hostCanvas == null)
        {
            GameObject canvasObject = new GameObject("HUDCanvas", typeof(RectTransform));
            hostCanvas = canvasObject.AddComponent<Canvas>();
            hostCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        Transform overlay = hostCanvas.transform.Find("BossHPOverlay");
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject("BossHPOverlay", typeof(RectTransform));
            overlay = overlayObject.transform;
            overlay.SetParent(hostCanvas.transform, false);
        }

        overlayCanvas = overlay.GetComponent<Canvas>();
        if (overlayCanvas == null)
        {
            overlayCanvas = overlay.gameObject.AddComponent<Canvas>();
        }
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 118;

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Transform panel = overlay.Find("Panel");
        if (panel == null)
        {
            GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            panel = panelObject.transform;
            panel.SetParent(overlay, false);
        }

        panelRoot = panel as RectTransform;
        panelRoot.anchorMin = new Vector2(0.5f, 1f);
        panelRoot.anchorMax = new Vector2(0.5f, 1f);
        panelRoot.pivot = new Vector2(0.5f, 1f);
        panelRoot.sizeDelta = new Vector2(680f, 70f);
        panelRoot.anchoredPosition = hiddenPosition;

        Image panelImage = panelRoot.GetComponent<Image>();
        panelImage.sprite = whiteSprite;
        panelImage.color = new Color(0f, 0f, 0f, 0.88f);

        panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
        panelCanvasGroup.alpha = 0f;

        Transform nameTransform = panelRoot.Find("BossName");
        if (nameTransform == null)
        {
            GameObject nameObject = new GameObject("BossName", typeof(RectTransform), typeof(Text));
            nameTransform = nameObject.transform;
            nameTransform.SetParent(panelRoot, false);
        }

        bossNameText = nameTransform.GetComponent<Text>();
        RectTransform nameRect = bossNameText.rectTransform;
        nameRect.anchorMin = new Vector2(0.05f, 0.62f);
        nameRect.anchorMax = new Vector2(0.95f, 0.95f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        bossNameText.font = uiFont;
        bossNameText.fontSize = 24;
        bossNameText.alignment = TextAnchor.MiddleCenter;
        bossNameText.color = Color.white;

        Transform barRoot = panelRoot.Find("BarRoot");
        if (barRoot == null)
        {
            GameObject barObject = new GameObject("BarRoot", typeof(RectTransform));
            barRoot = barObject.transform;
            barRoot.SetParent(panelRoot, false);
        }

        RectTransform barRect = barRoot as RectTransform;
        barRect.anchorMin = new Vector2(0.05f, 0.18f);
        barRect.anchorMax = new Vector2(0.95f, 0.5f);
        barRect.offsetMin = Vector2.zero;
        barRect.offsetMax = Vector2.zero;

        recentDamageFill = EnsureBarFill(barRoot, "RecentDamage", new Color(1f, 0.55f, 0.55f, 1f), whiteSprite, 0);
        healthFill = EnsureBarFill(barRoot, "Health", new Color(0.75f, 0.05f, 0.05f, 1f), whiteSprite, 1);
    }

    private Image EnsureBarFill(Transform parent, string name, Color color, Sprite sprite, int order)
    {
        Transform fillTransform = parent.Find(name);
        if (fillTransform == null)
        {
            GameObject fillObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            fillTransform = fillObject.transform;
            fillTransform.SetParent(parent, false);
        }

        Image fill = fillTransform.GetComponent<Image>();
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fill.sprite = sprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 1f;
        fill.color = color;
        fill.transform.SetSiblingIndex(order);
        return fill;
    }
}

