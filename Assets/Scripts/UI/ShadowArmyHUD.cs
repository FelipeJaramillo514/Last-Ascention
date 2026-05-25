using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShadowArmyHUD : MonoBehaviour
{
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform container;
    [SerializeField] private RectTransform statusRoot;
    [SerializeField] private Text storedCountText;
    [SerializeField] private Text activeCountText;
    [SerializeField] private Text summonHintText;

    private readonly Dictionary<ShadowSoldier, Image> iconsByShadow = new Dictionary<ShadowSoldier, Image>();
    private Font uiFont;
    private int storedSouls;
    private int activeShadows;
    private int maxActiveShadows;

    private void Awake()
    {
        EnsureUi();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<ShadowSummonedEvent>(OnShadowSummoned);
        EventBus.Subscribe<ShadowDiedEvent>(OnShadowDied);
        EventBus.Subscribe<ShadowInventoryChangedEvent>(OnShadowInventoryChanged);
        RefreshFromSystem();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ShadowSummonedEvent>(OnShadowSummoned);
        EventBus.Unsubscribe<ShadowDiedEvent>(OnShadowDied);
        EventBus.Unsubscribe<ShadowInventoryChangedEvent>(OnShadowInventoryChanged);
    }

    private void OnShadowSummoned(ShadowSummonedEvent summonedEvent)
    {
        if (summonedEvent == null || summonedEvent.soldier == null)
        {
            return;
        }

        EnsureUi();
        if (iconsByShadow.ContainsKey(summonedEvent.soldier))
        {
            return;
        }

        Image icon = CreateIcon();
        iconsByShadow[summonedEvent.soldier] = icon;
        activeShadows = iconsByShadow.Count;
        RefreshLayout();
        RefreshStatusText();
    }

    private void OnShadowDied(ShadowDiedEvent diedEvent)
    {
        if (diedEvent == null || diedEvent.soldier == null)
        {
            return;
        }

        Image icon;
        if (!iconsByShadow.TryGetValue(diedEvent.soldier, out icon) || icon == null)
        {
            return;
        }

        iconsByShadow.Remove(diedEvent.soldier);
        activeShadows = Mathf.Max(0, activeShadows - 1);
        RefreshStatusText();
        StartCoroutine(RemoveIconRoutine(icon));
    }

    private void OnShadowInventoryChanged(ShadowInventoryChangedEvent inventoryEvent)
    {
        if (inventoryEvent == null)
        {
            return;
        }

        EnsureUi();
        storedSouls = inventoryEvent.storedSouls;
        activeShadows = inventoryEvent.activeShadows;
        maxActiveShadows = inventoryEvent.maxActiveShadows;
        RefreshStatusText();
    }

    private Image CreateIcon()
    {
        GameObject iconObject = new GameObject("ShadowIcon", typeof(RectTransform), typeof(Image), typeof(Outline));
        iconObject.transform.SetParent(container, false);
        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = HUDSpriteFactory.GetShadowSprite();
        icon.color = Color.white;
        RectTransform rect = icon.rectTransform;
        rect.sizeDelta = new Vector2(16f, 16f);

        Outline outline = iconObject.GetComponent<Outline>();
        outline.effectDistance = new Vector2(1f, -1f);
        outline.effectColor = new Color(0f, 0.9f, 1f, 1f);
        return icon;
    }

    private IEnumerator RemoveIconRoutine(Image icon)
    {
        if (icon == null)
        {
            yield break;
        }

        Vector3 baseScale = icon.rectTransform.localScale;
        Color baseColor = icon.color;
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.3f);
            icon.color = Color.Lerp(Color.red, baseColor, Mathf.PingPong(t * 6f, 1f));
            icon.rectTransform.localScale = Vector3.Lerp(baseScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(icon.gameObject);
        RefreshLayout();
    }

    private void RefreshLayout()
    {
        int index = 0;
        foreach (Image icon in iconsByShadow.Values)
        {
            if (icon == null)
            {
                continue;
            }

            icon.rectTransform.anchorMin = new Vector2(1f, 0f);
            icon.rectTransform.anchorMax = new Vector2(1f, 0f);
            icon.rectTransform.pivot = new Vector2(1f, 0f);
            icon.rectTransform.anchoredPosition = new Vector2(-(index * 20f), 0f);
            index++;
        }
    }

    private void EnsureUi()
    {
        if (overlayCanvas != null && container != null && statusRoot != null && storedCountText != null && activeCountText != null && summonHintText != null)
        {
            RefreshStatusText();
            return;
        }

        uiFont = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Canvas hostCanvas = FindOverlayCanvas();
        Transform overlay = hostCanvas.transform.Find("ShadowArmyOverlay");
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject("ShadowArmyOverlay", typeof(RectTransform));
            overlay = overlayObject.transform;
            overlay.SetParent(hostCanvas.transform, false);
        }

        overlayCanvas = overlay.GetComponent<Canvas>();
        if (overlayCanvas == null)
        {
            overlayCanvas = overlay.gameObject.AddComponent<Canvas>();
        }
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 110;

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Transform containerTransform = overlay.Find("Icons");
        if (containerTransform == null)
        {
            GameObject containerObject = new GameObject("Icons", typeof(RectTransform));
            containerTransform = containerObject.transform;
            containerTransform.SetParent(overlay, false);
        }

        container = containerTransform as RectTransform;
        container.anchorMin = new Vector2(1f, 0f);
        container.anchorMax = new Vector2(1f, 0f);
        container.pivot = new Vector2(1f, 0f);
        container.anchoredPosition = new Vector2(-18f, 20f);
        container.sizeDelta = new Vector2(220f, 18f);

        Transform statusTransform = overlay.Find("SoulInventoryStatus");
        if (statusTransform == null)
        {
            GameObject statusObject = new GameObject("SoulInventoryStatus", typeof(RectTransform), typeof(Image), typeof(Outline));
            statusTransform = statusObject.transform;
            statusTransform.SetParent(overlay, false);
        }

        statusRoot = statusTransform as RectTransform;
        statusRoot.anchorMin = new Vector2(1f, 0f);
        statusRoot.anchorMax = new Vector2(1f, 0f);
        statusRoot.pivot = new Vector2(1f, 0f);
        statusRoot.anchoredPosition = new Vector2(-18f, 42f);
        statusRoot.sizeDelta = new Vector2(238f, 48f);

        Image statusImage = statusRoot.GetComponent<Image>();
        if (statusImage == null)
        {
            statusImage = statusRoot.gameObject.AddComponent<Image>();
        }
        statusImage.sprite = HUDSpriteFactory.WhiteSprite;
        statusImage.color = new Color(0.008f, 0.018f, 0.026f, 0.82f);
        statusImage.raycastTarget = false;

        Outline outline = statusRoot.GetComponent<Outline>();
        if (outline == null)
        {
            outline = statusRoot.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0f, 0.9f, 1f, 0.62f);
        outline.effectDistance = new Vector2(1f, -1f);

        storedCountText = EnsureText(statusRoot, "StoredSouls", 15, TextAnchor.MiddleLeft, new Color(0.62f, 1f, 0.95f, 1f), new Vector2(12f, 24f), new Vector2(112f, 18f));
        activeCountText = EnsureText(statusRoot, "ActiveShadows", 15, TextAnchor.MiddleLeft, Color.white, new Vector2(12f, 8f), new Vector2(112f, 18f));
        summonHintText = EnsureText(statusRoot, "SummonHint", 13, TextAnchor.MiddleRight, new Color(1f, 0.88f, 0.34f, 1f), new Vector2(126f, 15f), new Vector2(96f, 20f));
        summonHintText.text = "[R] INVOCAR";
        RefreshStatusText();
    }

    private void RefreshFromSystem()
    {
        ShadowExtractionSystem extractionSystem = FindFirstObjectByType<ShadowExtractionSystem>();
        if (extractionSystem == null)
        {
            RefreshStatusText();
            return;
        }

        storedSouls = extractionSystem.StoredSoulCount;
        activeShadows = extractionSystem.ActiveShadowCount;
        maxActiveShadows = extractionSystem.MaxShadows;
        RefreshStatusText();
    }

    private void RefreshStatusText()
    {
        if (storedCountText != null)
        {
            storedCountText.text = "ALMAS " + storedSouls;
        }

        if (activeCountText != null)
        {
            activeCountText.text = "SUBDITOS " + activeShadows + "/" + Mathf.Max(1, maxActiveShadows);
        }

        if (summonHintText != null)
        {
            summonHintText.color = storedSouls > 0 ? new Color(1f, 0.88f, 0.34f, 1f) : new Color(0.55f, 0.6f, 0.66f, 0.72f);
        }
    }

    private Text EnsureText(Transform parent, string name, int fontSize, TextAnchor alignment, Color color, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Transform target = parent.Find(name);
        if (target == null)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            target = textObject.transform;
            target.SetParent(parent, false);
        }

        Text text = target.GetComponent<Text>();
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
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
        outline.effectColor = new Color(0f, 0f, 0f, 0.86f);
        outline.effectDistance = new Vector2(1f, -1f);
        return text;
    }

    private Canvas FindOverlayCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return canvases[i];
            }
        }

        GameObject canvasObject = new GameObject("HUDCanvas", typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }
}
