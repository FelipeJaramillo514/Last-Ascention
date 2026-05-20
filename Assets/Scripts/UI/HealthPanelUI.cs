using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthPanelUI : MonoBehaviour
{
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform root;
    [SerializeField] private RectTransform heartsRoot;
    [SerializeField] private Text heartCountText;
    [SerializeField] private Text rankText;
    [SerializeField] private RectTransform penaltyRoot;
    [SerializeField] private Image penaltyFill;
    [SerializeField] private Text penaltyText;

    private readonly List<Image> heartImages = new List<Image>();
    private readonly List<Coroutine> shakeRoutines = new List<Coroutine>();
    private Font uiFont;
    private float displayedHP = -1f;
    private float displayedMaxHP = -1f;
    private float penaltyDuration;
    private float penaltyRemaining;

    private void Awake()
    {
        EnsureUi();
    }

    private void Update()
    {
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
        float currentHP = playerHealth.CurrentHP;
        float maxHP = Mathf.Max(1f, playerHealth.MaxHP);
        displayedHP = currentHP;
        displayedMaxHP = maxHP;

        int maxHearts = Mathf.Max(1, Mathf.CeilToInt(maxHP / 10f));
        int visibleHearts = Mathf.Min(10, maxHearts);
        EnsureHeartCapacity(visibleHearts);

        for (int i = 0; i < heartImages.Count; i++)
        {
            bool visible = i < visibleHearts;
            heartImages[i].gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            float segmentStart = i * 10f;
            float segmentValue = Mathf.Clamp(currentHP - segmentStart, 0f, 10f);
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

    public void TriggerDamageFeedback(float currentHP)
    {
        EnsureUi();
        if (displayedHP < 0f)
        {
            displayedHP = currentHP;
            return;
        }

        if (heartImages.Count == 0)
        {
            displayedHP = currentHP;
            return;
        }

        int heartIndex = Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0f, displayedHP - 0.01f) / 10f), 0, heartImages.Count - 1);
        if (heartIndex >= 0 && heartIndex < heartImages.Count)
        {
            if (shakeRoutines[heartIndex] != null)
            {
                StopCoroutine(shakeRoutines[heartIndex]);
            }

            shakeRoutines[heartIndex] = StartCoroutine(ShakeHeartRoutine(heartImages[heartIndex].rectTransform, heartIndex));
        }

        displayedHP = currentHP;
    }

    public void ActivatePenalty(float duration)
    {
        penaltyDuration = Mathf.Max(0.1f, duration);
        penaltyRemaining = penaltyDuration;
        UpdatePenaltyVisuals();
    }

    private IEnumerator ShakeHeartRoutine(RectTransform target, int index)
    {
        Vector2 basePosition = new Vector2(index * 18f, 0f);
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float offsetX = Mathf.Sin(elapsed * 48f) * 2f;
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
            rect.sizeDelta = new Vector2(16f, 16f);
            rect.anchoredPosition = new Vector2(heartImages.Count * 18f, 0f);
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
        penaltyText.text = "PENALIZACION ACTIVA - " + Mathf.CeilToInt(penaltyRemaining) + "s";
    }

    private void EnsureUi()
    {
        if (overlayCanvas != null && root != null && heartsRoot != null && heartCountText != null && rankText != null && penaltyRoot != null && penaltyFill != null && penaltyText != null)
        {
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

        Transform rootTransform = overlay.Find("Panel");
        if (rootTransform == null)
        {
            GameObject rootObject = new GameObject("Panel", typeof(RectTransform));
            rootTransform = rootObject.transform;
            rootTransform.SetParent(overlay, false);
        }

        root = rootTransform as RectTransform;
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(16f, -16f);
        root.sizeDelta = new Vector2(260f, 92f);

        heartsRoot = EnsureRect(root, "Hearts", new Vector2(0f, 0f), new Vector2(210f, 18f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        heartCountText = EnsureText(root, "HeartCount", 16, TextAnchor.MiddleLeft, Color.white, new Vector2(188f, -8f), new Vector2(64f, 18f));
        rankText = EnsureText(root, "Rank", 12, TextAnchor.MiddleLeft, new Color(0.27f, 0.27f, 0.27f, 1f), new Vector2(0f, -28f), new Vector2(220f, 16f));
        rankText.text = "RANGO OFICIAL: E";

        penaltyRoot = EnsureRect(root, "Penalty", new Vector2(0f, -48f), new Vector2(224f, 20f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        Image penaltyBackground = penaltyRoot.GetComponent<Image>();
        if (penaltyBackground == null)
        {
            penaltyBackground = penaltyRoot.gameObject.AddComponent<Image>();
        }
        penaltyBackground.sprite = HUDSpriteFactory.WhiteSprite;
        penaltyBackground.color = new Color(0.16f, 0f, 0f, 0.88f);

        Transform fillTransform = penaltyRoot.Find("Fill");
        if (fillTransform == null)
        {
            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillTransform = fillObject.transform;
            fillTransform.SetParent(penaltyRoot, false);
        }
        penaltyFill = fillTransform.GetComponent<Image>();
        RectTransform fillRect = penaltyFill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        penaltyFill.sprite = HUDSpriteFactory.WhiteSprite;
        penaltyFill.type = Image.Type.Filled;
        penaltyFill.fillMethod = Image.FillMethod.Horizontal;
        penaltyFill.fillOrigin = 0;
        penaltyFill.color = new Color(0.83f, 0.1f, 0.1f, 1f);

        penaltyText = EnsureText(penaltyRoot, "Text", 12, TextAnchor.MiddleCenter, Color.white, Vector2.zero, penaltyRoot.sizeDelta);
        penaltyRoot.gameObject.SetActive(false);
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
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);
        return text;
    }
}
