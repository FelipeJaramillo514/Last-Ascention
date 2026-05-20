using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ResourcePanelUI : MonoBehaviour
{
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform root;
    [SerializeField] private Image crystalIcon;
    [SerializeField] private Text goldText;

    private Font uiFont;
    private float displayedGold = -1f;
    private Coroutine bounceRoutine;

    private void Awake()
    {
        EnsureUi();
    }

    public void Refresh(SystemManager systemManager)
    {
        if (systemManager == null || systemManager.Stats == null)
        {
            return;
        }

        EnsureUi();
        float currentGold = systemManager.Stats.currentGold;
        if (Mathf.Approximately(displayedGold, currentGold))
        {
            return;
        }

        displayedGold = currentGold;
        goldText.text = Mathf.RoundToInt(currentGold).ToString();
    }

    public void OnGoldChanged(float currentGold)
    {
        EnsureUi();
        displayedGold = currentGold;
        goldText.text = Mathf.RoundToInt(currentGold).ToString();
        if (bounceRoutine != null)
        {
            StopCoroutine(bounceRoutine);
        }

        bounceRoutine = StartCoroutine(BounceRoutine());
    }

    private IEnumerator BounceRoutine()
    {
        Vector3 baseScale = root.localScale;
        float elapsed = 0f;
        while (elapsed < 0.12f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.12f);
            root.localScale = Vector3.Lerp(baseScale, baseScale * 1.3f, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 0.12f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.12f);
            root.localScale = Vector3.Lerp(baseScale * 1.3f, baseScale, t);
            yield return null;
        }

        root.localScale = baseScale;
        bounceRoutine = null;
    }

    private void EnsureUi()
    {
        if (overlayCanvas != null && root != null && crystalIcon != null && goldText != null)
        {
            return;
        }

        uiFont = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Canvas hostCanvas = FindOverlayCanvas();
        Transform overlay = hostCanvas.transform.Find("ResourceOverlay");
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject("ResourceOverlay", typeof(RectTransform));
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

        Transform rootTransform = overlay.Find("Panel");
        if (rootTransform == null)
        {
            GameObject rootObject = new GameObject("Panel", typeof(RectTransform));
            rootTransform = rootObject.transform;
            rootTransform.SetParent(overlay, false);
        }

        root = rootTransform as RectTransform;
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(0f, 0f);
        root.pivot = new Vector2(0f, 0f);
        root.anchoredPosition = new Vector2(16f, 18f);
        root.sizeDelta = new Vector2(140f, 28f);

        Transform iconTransform = root.Find("Crystal");
        if (iconTransform == null)
        {
            GameObject iconObject = new GameObject("Crystal", typeof(RectTransform), typeof(Image));
            iconTransform = iconObject.transform;
            iconTransform.SetParent(root, false);
        }
        crystalIcon = iconTransform.GetComponent<Image>();
        RectTransform iconRect = crystalIcon.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0f, 0f);
        iconRect.sizeDelta = new Vector2(16f, 16f);
        crystalIcon.sprite = HUDSpriteFactory.GetCrystalSprite();

        Transform textTransform = root.Find("GoldText");
        if (textTransform == null)
        {
            GameObject textObject = new GameObject("GoldText", typeof(RectTransform), typeof(Text));
            textTransform = textObject.transform;
            textTransform.SetParent(root, false);
        }
        goldText = textTransform.GetComponent<Text>();
        RectTransform textRect = goldText.rectTransform;
        textRect.anchorMin = new Vector2(0f, 0.5f);
        textRect.anchorMax = new Vector2(0f, 0.5f);
        textRect.pivot = new Vector2(0f, 0.5f);
        textRect.anchoredPosition = new Vector2(22f, 0f);
        textRect.sizeDelta = new Vector2(120f, 22f);
        goldText.font = uiFont;
        goldText.fontSize = 18;
        goldText.alignment = TextAnchor.MiddleLeft;
        goldText.color = new Color(1f, 0.84f, 0f, 1f);
        goldText.raycastTarget = false;
        Outline outline = goldText.GetComponent<Outline>();
        if (outline == null)
        {
            outline = goldText.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);
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
