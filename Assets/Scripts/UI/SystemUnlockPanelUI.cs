using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SystemUnlockPanelUI : MonoBehaviour
{
    public static SystemUnlockPanelUI Instance { get; private set; }

    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text messageText;

    private Coroutine displayRoutine;
    private Font uiFont;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureUi();
    }

    public void ShowUnlockMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        EnsureUi();
        if (displayRoutine != null)
        {
            StopCoroutine(displayRoutine);
        }

        displayRoutine = StartCoroutine(DisplayRoutine(message));
    }

    private IEnumerator DisplayRoutine(string message)
    {
        float previousTimeScale = Time.timeScale;
        Time.timeScale = 0.15f;
        panelRoot.gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        messageText.text = string.Empty;

        for (int i = 0; i < message.Length; i++)
        {
            messageText.text += message[i];
            yield return new WaitForSecondsRealtime(0.04f);
        }

        yield return new WaitForSecondsRealtime(2.5f);

        float elapsed = 0f;
        while (elapsed < 0.25f)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / 0.25f);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        panelRoot.gameObject.SetActive(false);
        Time.timeScale = 1f;
        displayRoutine = null;
    }

    private void EnsureUi()
    {
        if (overlayCanvas != null && panelRoot != null && canvasGroup != null && messageText != null)
        {
            return;
        }

        uiFont = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

        Transform overlay = hostCanvas.transform.Find("SystemUnlockOverlay");
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject("SystemUnlockOverlay", typeof(RectTransform));
            overlay = overlayObject.transform;
            overlay.SetParent(hostCanvas.transform, false);
        }

        overlayCanvas = overlay.GetComponent<Canvas>();
        if (overlayCanvas == null)
        {
            overlayCanvas = overlay.gameObject.AddComponent<Canvas>();
        }
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 145;

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
        panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
        panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
        panelRoot.pivot = new Vector2(0.5f, 0.5f);
        panelRoot.sizeDelta = new Vector2(640f, 180f);
        panelRoot.anchoredPosition = Vector2.zero;

        Image panelImage = panelRoot.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.92f);

        canvasGroup = panelRoot.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        panelRoot.gameObject.SetActive(false);

        Transform textTransform = panelRoot.Find("Message");
        if (textTransform == null)
        {
            GameObject textObject = new GameObject("Message", typeof(RectTransform), typeof(Text));
            textTransform = textObject.transform;
            textTransform.SetParent(panelRoot, false);
        }

        messageText = textTransform.GetComponent<Text>();
        RectTransform textRect = messageText.rectTransform;
        textRect.anchorMin = new Vector2(0.08f, 0.2f);
        textRect.anchorMax = new Vector2(0.92f, 0.8f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        messageText.font = uiFont;
        messageText.fontSize = 28;
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.color = new Color(0.5f, 0.95f, 1f, 1f);
        messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        messageText.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = messageText.GetComponent<Outline>();
        if (outline == null)
        {
            outline = messageText.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0f, 0f, 0f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);
    }
}

