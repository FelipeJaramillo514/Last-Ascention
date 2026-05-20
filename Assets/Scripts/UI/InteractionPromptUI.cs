using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private Text promptText;

    private Object currentOwner;
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

    public void ShowPrompt(Object owner, string message)
    {
        if (owner == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        EnsureUi();
        currentOwner = owner;
        promptText.text = message;
        promptCanvasGroup.alpha = 1f;
    }

    public void HidePrompt(Object owner)
    {
        if (owner != null && currentOwner != owner)
        {
            return;
        }

        currentOwner = null;
        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 0f;
        }
    }

    private void EnsureUi()
    {
        if (overlayCanvas != null && promptCanvasGroup != null && promptText != null)
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

        Transform overlay = hostCanvas.transform.Find("InteractionPromptOverlay");
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject("InteractionPromptOverlay", typeof(RectTransform));
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

        Transform promptRoot = overlay.Find("PromptRoot");
        if (promptRoot == null)
        {
            GameObject promptRootObject = new GameObject("PromptRoot", typeof(RectTransform));
            promptRoot = promptRootObject.transform;
            promptRoot.SetParent(overlay, false);
        }

        RectTransform promptRect = promptRoot.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0f);
        promptRect.anchorMax = new Vector2(0.5f, 0f);
        promptRect.pivot = new Vector2(0.5f, 0f);
        promptRect.sizeDelta = new Vector2(420f, 42f);
        promptRect.anchoredPosition = new Vector2(0f, 32f);

        Image background = promptRoot.GetComponent<Image>();
        if (background == null)
        {
            background = promptRoot.gameObject.AddComponent<Image>();
        }
        background.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        background.color = new Color(0f, 0f, 0f, 0.78f);

        promptCanvasGroup = promptRoot.GetComponent<CanvasGroup>();
        if (promptCanvasGroup == null)
        {
            promptCanvasGroup = promptRoot.gameObject.AddComponent<CanvasGroup>();
        }
        promptCanvasGroup.alpha = 0f;

        Transform textTransform = promptRoot.Find("PromptText");
        if (textTransform == null)
        {
            GameObject textObject = new GameObject("PromptText", typeof(RectTransform));
            textTransform = textObject.transform;
            textTransform.SetParent(promptRoot, false);
        }

        promptText = textTransform.GetComponent<Text>();
        if (promptText == null)
        {
            promptText = textTransform.gameObject.AddComponent<Text>();
        }

        RectTransform textRect = promptText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        promptText.font = uiFont;
        promptText.fontSize = 22;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = Color.white;
        promptText.raycastTarget = false;

        Outline outline = promptText.GetComponent<Outline>();
        if (outline == null)
        {
            outline = promptText.gameObject.AddComponent<Outline>();
        }
        outline.effectDistance = new Vector2(1f, -1f);
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
    }
}

