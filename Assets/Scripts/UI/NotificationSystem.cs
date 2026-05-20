using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NotificationSystem : MonoBehaviour
{
    private struct NotificationRequest
    {
        public string message;
        public Color color;
        public float duration;
    }

    public static NotificationSystem Instance { get; private set; }

    [SerializeField] private Canvas notificationCanvas;
    [SerializeField] private RectTransform notificationRoot;
    [SerializeField] private CanvasGroup notificationCanvasGroup;
    [SerializeField] private Text notificationText;
    [SerializeField] private Image painVignette;

    private readonly Queue<NotificationRequest> queuedNotifications = new Queue<NotificationRequest>();

    private Coroutine queueRoutine;
    private Coroutine painRoutine;
    private Font uiFont;
    private Vector2 baseNotificationPosition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureUiExists();
    }

    public void ShowNotification(string message, Color color, float duration)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        EnsureUiExists();
        NotificationRequest request = new NotificationRequest();
        request.message = message;
        request.color = color;
        request.duration = Mathf.Max(0.1f, duration);
        queuedNotifications.Enqueue(request);

        if (queueRoutine == null)
        {
            queueRoutine = StartCoroutine(ProcessQueueRoutine());
        }
    }

    public void ShowNotification(string message, Color color)
    {
        ShowNotification(message, color, 2f);
    }

    public void ShowPainVignette(float duration)
    {
        EnsureUiExists();
        if (painRoutine != null)
        {
            StopCoroutine(painRoutine);
        }

        painRoutine = StartCoroutine(PainVignetteRoutine(duration));
    }

    private IEnumerator ProcessQueueRoutine()
    {
        while (queuedNotifications.Count > 0)
        {
            NotificationRequest request = queuedNotifications.Dequeue();
            notificationText.text = request.message;
            notificationText.color = request.color;
            notificationCanvasGroup.alpha = 0f;
            notificationRoot.anchoredPosition = baseNotificationPosition;

            float fadeInElapsed = 0f;
            while (fadeInElapsed < 0.2f)
            {
                fadeInElapsed += Time.unscaledDeltaTime;
                notificationCanvasGroup.alpha = Mathf.Clamp01(fadeInElapsed / 0.2f);
                yield return null;
            }

            notificationCanvasGroup.alpha = 1f;
            float waitElapsed = 0f;
            while (waitElapsed < request.duration)
            {
                waitElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            float fadeOutElapsed = 0f;
            while (fadeOutElapsed < 0.3f)
            {
                fadeOutElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(fadeOutElapsed / 0.3f);
                notificationCanvasGroup.alpha = 1f - t;
                notificationRoot.anchoredPosition = baseNotificationPosition + Vector2.up * (20f * t);
                yield return null;
            }

            notificationCanvasGroup.alpha = 0f;
            notificationRoot.anchoredPosition = baseNotificationPosition;
        }

        queueRoutine = null;
    }

    private IEnumerator PainVignetteRoutine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = 0.1f + Mathf.PingPong(Time.unscaledTime * 0.6f, 0.12f);
            painVignette.color = new Color(0.75f, 0.05f, 0.05f, pulse);
            yield return null;
        }

        painVignette.color = new Color(0.75f, 0.05f, 0.05f, 0f);
        painRoutine = null;
    }

    private void EnsureUiExists()
    {
        if (notificationCanvas != null && notificationRoot != null && notificationCanvasGroup != null && notificationText != null && painVignette != null)
        {
            return;
        }

        uiFont = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Sprite panelSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));

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

        Transform overlay = hostCanvas.transform.Find("NotificationOverlay");
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject("NotificationOverlay", typeof(RectTransform));
            overlay = overlayObject.transform;
            overlay.SetParent(hostCanvas.transform, false);
        }

        notificationCanvas = overlay.GetComponent<Canvas>();
        if (notificationCanvas == null)
        {
            notificationCanvas = overlay.gameObject.AddComponent<Canvas>();
        }
        notificationCanvas.overrideSorting = true;
        notificationCanvas.sortingOrder = 130;

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        if (overlayRect == null)
        {
            overlayRect = overlay.gameObject.AddComponent<RectTransform>();
        }
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        if (overlay.GetComponent<GraphicRaycaster>() == null)
        {
            overlay.gameObject.AddComponent<GraphicRaycaster>();
        }

        Transform vignetteTransform = overlay.Find("PainVignette");
        if (vignetteTransform == null)
        {
            GameObject vignetteObject = new GameObject("PainVignette", typeof(RectTransform));
            vignetteTransform = vignetteObject.transform;
            vignetteTransform.SetParent(overlay, false);
        }

        painVignette = vignetteTransform.GetComponent<Image>();
        if (painVignette == null)
        {
            painVignette = vignetteTransform.gameObject.AddComponent<Image>();
        }
        RectTransform vignetteRect = painVignette.rectTransform;
        vignetteRect.anchorMin = Vector2.zero;
        vignetteRect.anchorMax = Vector2.one;
        vignetteRect.offsetMin = Vector2.zero;
        vignetteRect.offsetMax = Vector2.zero;
        painVignette.sprite = panelSprite;
        painVignette.color = new Color(0.75f, 0.05f, 0.05f, 0f);
        painVignette.raycastTarget = false;

        Transform rootTransform = overlay.Find("NotificationRoot");
        if (rootTransform == null)
        {
            GameObject rootObject = new GameObject("NotificationRoot", typeof(RectTransform));
            rootTransform = rootObject.transform;
            rootTransform.SetParent(overlay, false);
        }

        notificationRoot = rootTransform as RectTransform;
        if (notificationRoot == null)
        {
            notificationRoot = rootTransform.gameObject.AddComponent<RectTransform>();
        }
        notificationRoot.anchorMin = new Vector2(0.5f, 1f);
        notificationRoot.anchorMax = new Vector2(0.5f, 1f);
        notificationRoot.pivot = new Vector2(0.5f, 1f);
        notificationRoot.sizeDelta = new Vector2(560f, 64f);
        notificationRoot.anchoredPosition = new Vector2(0f, -48f);
        baseNotificationPosition = notificationRoot.anchoredPosition;

        notificationCanvasGroup = notificationRoot.GetComponent<CanvasGroup>();
        if (notificationCanvasGroup == null)
        {
            notificationCanvasGroup = notificationRoot.gameObject.AddComponent<CanvasGroup>();
        }
        notificationCanvasGroup.alpha = 0f;

        Transform textTransform = notificationRoot.Find("Text");
        if (textTransform == null)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textTransform = textObject.transform;
            textTransform.SetParent(notificationRoot, false);
        }

        notificationText = textTransform.GetComponent<Text>();
        if (notificationText == null)
        {
            notificationText = textTransform.gameObject.AddComponent<Text>();
        }
        RectTransform textRect = notificationText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        notificationText.font = uiFont;
        notificationText.alignment = TextAnchor.MiddleCenter;
        notificationText.horizontalOverflow = HorizontalWrapMode.Wrap;
        notificationText.verticalOverflow = VerticalWrapMode.Overflow;
        notificationText.fontSize = 28;
        notificationText.text = string.Empty;
        notificationText.raycastTarget = false;

        Outline outline = notificationText.GetComponent<Outline>();
        if (outline == null)
        {
            outline = notificationText.gameObject.AddComponent<Outline>();
        }
        outline.effectDistance = new Vector2(2f, -2f);
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
    }
}

