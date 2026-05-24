using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthPanelUI : MonoBehaviour
{
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform root;
    [SerializeField] private RectTransform heartsRoot;
    [SerializeField] private RectTransform penaltyRoot;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image delayedHealthFill;
    [SerializeField] private Image healthGlow;
    [SerializeField] private Image expFill;
    [SerializeField] private Image penaltyFill;
    [SerializeField] private Text titleText;
    [SerializeField] private Text hpText;
    [SerializeField] private Text hpPercentText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text rankText;
    [SerializeField] private Text expText;
    [SerializeField] private Text heartCountText;
    [SerializeField] private Text penaltyText;

    private readonly List<Image> heartImages = new List<Image>();
    private readonly List<Coroutine> shakeRoutines = new List<Coroutine>();
    private Font uiFont;
    private KaisenController cachedController;
    private SystemManager cachedSystem;
    private float targetHP = -1f;
    private float targetMaxHP = -1f;
    private float displayedHP = -1f;
    private float delayedHP = -1f;
    private float damagePulse;
    private float penaltyDuration;
    private float penaltyRemaining;
    private Vector2 rootBasePosition;

    private void Awake()
    {
        EnsureUi();
    }

    private void Update()
    {
        AnimateHealth();
        AnimatePanelFeedback();

        if (penaltyRemaining > 0f)
        {
            penaltyRemaining = Mathf.Max(0f, penaltyRemaining - Time.unscaledDeltaTime);
            UpdatePenaltyVisuals();
        }
    }

    public void Refresh(PlayerHealth playerHealth)
    {
        if (playerHealth == null)
        {
            return;
        }

        EnsureUi();
        cachedController = cachedController != null ? cachedController : playerHealth.GetComponent<KaisenController>();
        cachedSystem = cachedSystem != null ? cachedSystem : SystemManager.Instance;

        targetHP = Mathf.Max(0f, playerHealth.CurrentHP);
        targetMaxHP = Mathf.Max(1f, playerHealth.MaxHP);
        if (displayedHP < 0f || delayedHP < 0f)
        {
            displayedHP = targetHP;
            delayedHP = targetHP;
        }

        RefreshIdentity();
        RefreshHearts();
        RefreshExperience();
        UpdateHealthVisuals();
    }

    public void TriggerDamageFeedback(float currentHP)
    {
        EnsureUi();
        targetHP = Mathf.Max(0f, currentHP);
        damagePulse = 1f;

        int heartIndex = Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0f, displayedHP - 0.01f) / 10f), 0, Mathf.Max(0, heartImages.Count - 1));
        if (heartIndex >= 0 && heartIndex < heartImages.Count)
        {
            if (shakeRoutines[heartIndex] != null)
            {
                StopCoroutine(shakeRoutines[heartIndex]);
            }

            shakeRoutines[heartIndex] = StartCoroutine(ShakeHeartRoutine(heartImages[heartIndex].rectTransform, heartIndex));
        }
    }

    public void ActivatePenalty(float duration)
    {
        EnsureUi();
        penaltyDuration = Mathf.Max(0.1f, duration);
        penaltyRemaining = penaltyDuration;
        UpdatePenaltyVisuals();
    }

    private void AnimateHealth()
    {
        if (targetHP < 0f || targetMaxHP <= 0f)
        {
            return;
        }

        float fast = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
        float slow = 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime);
        displayedHP = Mathf.Lerp(displayedHP, targetHP, fast);
        delayedHP = delayedHP > targetHP
            ? Mathf.Lerp(delayedHP, targetHP, slow)
            : Mathf.Lerp(delayedHP, targetHP, fast);

        if (Mathf.Abs(displayedHP - targetHP) < 0.05f)
        {
            displayedHP = targetHP;
        }

        if (Mathf.Abs(delayedHP - targetHP) < 0.05f)
        {
            delayedHP = targetHP;
        }

        UpdateHealthVisuals();
    }

    private void AnimatePanelFeedback()
    {
        if (root == null)
        {
            return;
        }

        damagePulse = Mathf.MoveTowards(damagePulse, 0f, Time.unscaledDeltaTime * 3.6f);
        float lowHealth = targetMaxHP > 0f ? 1f - Mathf.Clamp01(displayedHP / targetMaxHP) : 0f;
        float pulse = Mathf.Sin(Time.unscaledTime * 8f) * 0.5f + 0.5f;
        float scale = 1f + (damagePulse * 0.035f) + (lowHealth > 0.68f ? pulse * 0.012f : 0f);
        root.localScale = new Vector3(scale, scale, 1f);
        root.anchoredPosition = rootBasePosition + new Vector2(Mathf.Sin(Time.unscaledTime * 52f) * damagePulse * 3f, 0f);

        if (healthGlow != null)
        {
            Color glowColor = Color.Lerp(new Color(0f, 0.86f, 1f, 0.28f), new Color(1f, 0.1f, 0.14f, 0.45f), lowHealth);
            glowColor.a += damagePulse * 0.28f;
            healthGlow.color = glowColor;
        }
    }

    private void UpdateHealthVisuals()
    {
        if (healthFill == null || delayedHealthFill == null || hpText == null || hpPercentText == null || targetMaxHP <= 0f)
        {
            return;
        }

        float normalized = Mathf.Clamp01(displayedHP / targetMaxHP);
        float delayedNormalized = Mathf.Clamp01(delayedHP / targetMaxHP);
        healthFill.fillAmount = normalized;
        delayedHealthFill.fillAmount = delayedNormalized;

        Color healthy = new Color(0.1f, 1f, 0.72f, 1f);
        Color wounded = new Color(1f, 0.76f, 0.16f, 1f);
        Color critical = new Color(1f, 0.12f, 0.16f, 1f);
        healthFill.color = normalized > 0.45f
            ? Color.Lerp(wounded, healthy, Mathf.InverseLerp(0.45f, 1f, normalized))
            : Color.Lerp(critical, wounded, Mathf.InverseLerp(0.12f, 0.45f, normalized));

        hpText.text = Mathf.CeilToInt(displayedHP) + " / " + Mathf.CeilToInt(targetMaxHP);
        hpPercentText.text = Mathf.RoundToInt(normalized * 100f) + "%";
    }

    private void RefreshIdentity()
    {
        if (cachedController == null || cachedController.Stats == null)
        {
            return;
        }

        if (titleText != null)
        {
            titleText.text = string.Empty;
            titleText.gameObject.SetActive(false);
        }

        levelText.text = "NV " + cachedController.Stats.systemLevel;
        rankText.text = "RANGO " + cachedController.Stats.officialRank;

        SpriteRenderer visual = cachedController.VisualSprite;
        if (portraitImage != null && visual != null && visual.sprite != null)
        {
            portraitImage.sprite = visual.sprite;
            portraitImage.color = Color.white;
        }
    }

    private void RefreshExperience()
    {
        if (cachedSystem == null || expFill == null || expText == null)
        {
            expFill.fillAmount = 0f;
            expText.text = "XP --";
            return;
        }

        float required = Mathf.Max(1f, cachedSystem.ExpToNextLevel);
        float current = Mathf.Clamp(cachedSystem.CurrentEXP, 0f, required);
        expFill.fillAmount = current / required;
        expText.text = "XP " + Mathf.RoundToInt(current) + " / " + Mathf.RoundToInt(required);
    }

    private void RefreshHearts()
    {
        int maxHearts = Mathf.Max(1, Mathf.CeilToInt(targetMaxHP / 10f));
        int visibleHearts = maxHearts > 10 ? 8 : Mathf.Min(10, maxHearts);
        EnsureHeartCapacity(visibleHearts);

        for (int i = 0; i < heartImages.Count; i++)
        {
            bool visible = i < visibleHearts;
            heartImages[i].gameObject.SetActive(visible);
            heartImages[i].rectTransform.anchoredPosition = new Vector2(i * 15f, 0f);
            heartImages[i].rectTransform.sizeDelta = new Vector2(14f, 14f);
            if (!visible)
            {
                continue;
            }

            float segmentStart = i * 10f;
            float segmentValue = Mathf.Clamp(targetHP - segmentStart, 0f, 10f);
            PixelHeartState state = PixelHeartState.Empty;
            if (segmentValue >= 9.99f)
            {
                state = PixelHeartState.Full;
            }
            else if (segmentValue > 0f)
            {
                state = PixelHeartState.Half;
            }

            heartImages[i].sprite = HUDSpriteFactory.GetHeartSprite(state);
        }

        heartCountText.gameObject.SetActive(maxHearts > 10);
        if (maxHearts > 10)
        {
            heartCountText.text = "x" + maxHearts;
        }
    }

    private IEnumerator ShakeHeartRoutine(RectTransform target, int index)
    {
        Vector2 basePosition = new Vector2(index * 15f, 0f);
        float elapsed = 0f;
        while (elapsed < 0.22f)
        {
            elapsed += Time.unscaledDeltaTime;
            float offsetX = Mathf.Sin(elapsed * 52f) * 2.2f;
            target.anchoredPosition = basePosition + new Vector2(offsetX, 0f);
            yield return null;
        }

        target.anchoredPosition = basePosition;
        shakeRoutines[index] = null;
    }

    private void EnsureHeartCapacity(int count)
    {
        while (heartImages.Count < count)
        {
            GameObject heartObject = new GameObject("Heart_" + heartImages.Count, typeof(RectTransform), typeof(Image));
            heartObject.transform.SetParent(heartsRoot, false);
            Image heartImage = heartObject.GetComponent<Image>();
            RectTransform rect = heartImage.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(14f, 14f);
            rect.anchoredPosition = new Vector2(heartImages.Count * 15f, 0f);
            heartImage.raycastTarget = false;
            heartImages.Add(heartImage);
            shakeRoutines.Add(null);
        }
    }

    private void UpdatePenaltyVisuals()
    {
        bool active = penaltyRemaining > 0f;
        penaltyRoot.gameObject.SetActive(active);
        if (!active)
        {
            return;
        }

        penaltyFill.fillAmount = Mathf.Clamp01(penaltyRemaining / Mathf.Max(0.1f, penaltyDuration));
        penaltyText.text = "DOLOR " + Mathf.CeilToInt(penaltyRemaining) + "s";
    }

    private void EnsureUi()
    {
        if (overlayCanvas != null && root != null && heartsRoot != null && portraitImage != null && healthFill != null && delayedHealthFill != null && healthGlow != null && expFill != null && hpText != null && levelText != null && rankText != null && penaltyRoot != null && penaltyFill != null && penaltyText != null)
        {
            ApplyHeaderHeartsLayout();
            return;
        }

        uiFont = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Canvas hostCanvas = FindOverlayCanvas();
        Transform overlay = hostCanvas.transform.Find("HealthOverlay");
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject("HealthOverlay", typeof(RectTransform));
            overlay = overlayObject.transform;
            overlay.SetParent(hostCanvas.transform, false);
        }

        overlayCanvas = overlay.GetComponent<Canvas>();
        if (overlayCanvas == null)
        {
            overlayCanvas = overlay.gameObject.AddComponent<Canvas>();
        }
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 112;

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        root = EnsureRect(overlay, "Panel", new Vector2(16f, -16f), new Vector2(342f, 132f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        rootBasePosition = root.anchoredPosition;
        Image rootImage = root.GetComponent<Image>();
        if (rootImage == null)
        {
            rootImage = root.gameObject.AddComponent<Image>();
        }
        rootImage.sprite = HUDSpriteFactory.WhiteSprite;
        rootImage.color = new Color(0.015f, 0.025f, 0.038f, 0.88f);
        rootImage.raycastTarget = false;
        ApplyOutline(root, new Color(0f, 0.85f, 1f, 0.95f), new Vector2(1.4f, -1.4f));

        Image topGlow = EnsureImage(root, "TopGlow", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -3f), new Vector2(0f, 3f));
        topGlow.sprite = HUDSpriteFactory.WhiteSprite;
        topGlow.color = new Color(0f, 0.9f, 1f, 0.82f);

        Image sideGlow = EnsureImage(root, "SideGlow", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, -1f), new Vector2(4f, 0f));
        sideGlow.sprite = HUDSpriteFactory.WhiteSprite;
        sideGlow.color = new Color(0.62f, 0f, 1f, 0.7f);

        RectTransform portraitRoot = EnsureRect(root, "Portrait", new Vector2(16f, -22f), new Vector2(68f, 82f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        Image portraitFrame = portraitRoot.GetComponent<Image>();
        if (portraitFrame == null)
        {
            portraitFrame = portraitRoot.gameObject.AddComponent<Image>();
        }
        portraitFrame.sprite = HUDSpriteFactory.WhiteSprite;
        portraitFrame.color = new Color(0.02f, 0.08f, 0.11f, 0.96f);
        ApplyOutline(portraitRoot, new Color(0f, 0.82f, 1f, 0.85f), new Vector2(1f, -1f));

        portraitImage = EnsureImage(portraitRoot, "Sprite", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -3f), new Vector2(-12f, -12f));
        portraitImage.preserveAspect = true;
        portraitImage.color = new Color(0.65f, 0.95f, 1f, 0.85f);

        titleText = EnsureText(root, "Title", 12, TextAnchor.MiddleLeft, new Color(0.8f, 0.98f, 1f, 1f), new Vector2(98f, -18f), new Vector2(128f, 18f));
        levelText = EnsureText(root, "Level", 18, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.28f, 1f), new Vector2(270f, -18f), new Vector2(54f, 26f));
        rankText = EnsureText(root, "Rank", 12, TextAnchor.MiddleLeft, new Color(0.74f, 0.78f, 0.86f, 1f), new Vector2(98f, -38f), new Vector2(124f, 18f));

        RectTransform healthRoot = EnsureRect(root, "HealthBar", new Vector2(98f, -62f), new Vector2(218f, 18f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        Image healthBackground = healthRoot.GetComponent<Image>();
        if (healthBackground == null)
        {
            healthBackground = healthRoot.gameObject.AddComponent<Image>();
        }
        healthBackground.sprite = HUDSpriteFactory.WhiteSprite;
        healthBackground.color = new Color(0.02f, 0.01f, 0.018f, 0.95f);
        ApplyOutline(healthRoot, new Color(0f, 0f, 0f, 0.85f), new Vector2(1f, -1f));

        delayedHealthFill = EnsureImage(healthRoot, "DelayFill", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        delayedHealthFill.sprite = HUDSpriteFactory.WhiteSprite;
        delayedHealthFill.type = Image.Type.Filled;
        delayedHealthFill.fillMethod = Image.FillMethod.Horizontal;
        delayedHealthFill.fillOrigin = 0;
        delayedHealthFill.color = new Color(0.95f, 0.18f, 0.18f, 0.75f);

        healthFill = EnsureImage(healthRoot, "Fill", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        healthFill.sprite = HUDSpriteFactory.WhiteSprite;
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillOrigin = 0;

        healthGlow = EnsureImage(healthRoot, "Glow", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 8f));
        healthGlow.sprite = HUDSpriteFactory.WhiteSprite;
        healthGlow.raycastTarget = false;

        hpText = EnsureText(root, "HpText", 16, TextAnchor.MiddleLeft, Color.white, new Vector2(98f, -86f), new Vector2(120f, 20f));
        hpPercentText = EnsureText(root, "HpPercent", 14, TextAnchor.MiddleRight, new Color(0.76f, 1f, 0.92f, 1f), new Vector2(230f, -86f), new Vector2(86f, 20f));

        RectTransform expRoot = EnsureRect(root, "ExpBar", new Vector2(98f, -108f), new Vector2(218f, 9f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        Image expBackground = expRoot.GetComponent<Image>();
        if (expBackground == null)
        {
            expBackground = expRoot.gameObject.AddComponent<Image>();
        }
        expBackground.sprite = HUDSpriteFactory.WhiteSprite;
        expBackground.color = new Color(0.02f, 0.02f, 0.04f, 0.92f);

        expFill = EnsureImage(expRoot, "Fill", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        expFill.sprite = HUDSpriteFactory.WhiteSprite;
        expFill.type = Image.Type.Filled;
        expFill.fillMethod = Image.FillMethod.Horizontal;
        expFill.fillOrigin = 0;
        expFill.color = new Color(0.72f, 0.24f, 1f, 0.95f);
        expText = EnsureText(root, "ExpText", 11, TextAnchor.MiddleLeft, new Color(0.82f, 0.74f, 1f, 1f), new Vector2(98f, -119f), new Vector2(164f, 14f));

        heartsRoot = EnsureRect(root, "Hearts", new Vector2(98f, -20f), new Vector2(130f, 16f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        heartCountText = EnsureText(root, "HeartCount", 13, TextAnchor.MiddleLeft, Color.white, new Vector2(222f, -20f), new Vector2(38f, 16f));

        penaltyRoot = EnsureRect(root, "Penalty", new Vector2(18f, 10f), new Vector2(306f, 20f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        Image penaltyBackground = penaltyRoot.GetComponent<Image>();
        if (penaltyBackground == null)
        {
            penaltyBackground = penaltyRoot.gameObject.AddComponent<Image>();
        }
        penaltyBackground.sprite = HUDSpriteFactory.WhiteSprite;
        penaltyBackground.color = new Color(0.14f, 0f, 0f, 0.92f);

        penaltyFill = EnsureImage(penaltyRoot, "Fill", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        penaltyFill.sprite = HUDSpriteFactory.WhiteSprite;
        penaltyFill.type = Image.Type.Filled;
        penaltyFill.fillMethod = Image.FillMethod.Horizontal;
        penaltyFill.fillOrigin = 0;
        penaltyFill.color = new Color(1f, 0.12f, 0.12f, 1f);

        penaltyText = EnsureText(penaltyRoot, "Text", 12, TextAnchor.MiddleCenter, Color.white, Vector2.zero, penaltyRoot.sizeDelta);
        penaltyRoot.gameObject.SetActive(false);
        ApplyHeaderHeartsLayout();
    }

    private void ApplyHeaderHeartsLayout()
    {
        if (titleText != null)
        {
            titleText.text = string.Empty;
            titleText.gameObject.SetActive(false);
        }

        if (heartsRoot != null)
        {
            heartsRoot.anchorMin = new Vector2(0f, 1f);
            heartsRoot.anchorMax = new Vector2(0f, 1f);
            heartsRoot.pivot = new Vector2(0f, 1f);
            heartsRoot.anchoredPosition = new Vector2(98f, -20f);
            heartsRoot.sizeDelta = new Vector2(130f, 16f);
        }

        if (heartCountText != null)
        {
            RectTransform countRect = heartCountText.rectTransform;
            countRect.anchorMin = new Vector2(0f, 1f);
            countRect.anchorMax = new Vector2(0f, 1f);
            countRect.pivot = new Vector2(0f, 1f);
            countRect.anchoredPosition = new Vector2(222f, -20f);
            countRect.sizeDelta = new Vector2(38f, 16f);
        }
    }

    private Canvas FindOverlayCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas fallback = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                continue;
            }

            if (canvas.name == "HUDCanvas")
            {
                return canvas;
            }

            if (fallback == null)
            {
                fallback = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            }
        }

        if (fallback != null)
        {
            return fallback;
        }

        GameObject canvasObject = new GameObject("HUDCanvas", typeof(RectTransform));
        Canvas newCanvas = canvasObject.AddComponent<Canvas>();
        newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvasObject.AddComponent<GraphicRaycaster>();
        return newCanvas;
    }

    private RectTransform EnsureRect(Transform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        Transform target = parent.Find(name);
        if (target == null)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            target = child.transform;
            target.SetParent(parent, false);
        }

        RectTransform rect = target as RectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private Image EnsureImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Transform target = parent.Find(name);
        if (target == null)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(Image));
            target = child.transform;
            target.SetParent(parent, false);
        }

        Image image = target.GetComponent<Image>();
        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        image.raycastTarget = false;
        return image;
    }

    private Text EnsureText(Transform parent, string name, int fontSize, TextAnchor alignment, Color color, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Transform target = parent.Find(name);
        if (target == null)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(Text));
            target = child.transform;
            target.SetParent(parent, false);
        }

        Text text = target.GetComponent<Text>();
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        text.font = uiFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        Outline outline = text.GetComponent<Outline>();
        if (outline == null)
        {
            outline = text.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0f, 0f, 0f, 0.92f);
        outline.effectDistance = new Vector2(1f, -1f);
        return text;
    }

    private void ApplyOutline(RectTransform rect, Color color, Vector2 distance)
    {
        Outline outline = rect.GetComponent<Outline>();
        if (outline == null)
        {
            outline = rect.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = color;
        outline.effectDistance = distance;
    }
}
