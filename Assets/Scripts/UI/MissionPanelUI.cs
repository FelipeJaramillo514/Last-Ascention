using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MissionPanelUI : MonoBehaviour
{
    private sealed class MissionEntryWidgets
    {
        public Text nameText;
        public Text descriptionText;
        public Text progressText;
        public Text checkText;
        public Image fillImage;
        public Image strikeImage;
    }

    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private RectTransform handleRoot;

    private readonly List<MissionEntryWidgets> entries = new List<MissionEntryWidgets>();

    private DailyMissionSystem missionSystem;
    private Font uiFont;
    private bool isExpanded;
    private float currentSlide;

    private void Awake()
    {
        EnsureUiExists();
        currentSlide = 0f;
        isExpanded = false;
    }

    private void OnEnable()
    {
        AttachMissionSystemIfNeeded();
    }

    private void OnDisable()
    {
        if (missionSystem != null)
        {
            missionSystem.MissionsChanged -= RefreshEntries;
        }
    }

    private void Update()
    {
        AttachMissionSystemIfNeeded();
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            isExpanded = !isExpanded;
        }

        float targetSlide = isExpanded ? 1f : 0f;
        currentSlide = Mathf.MoveTowards(currentSlide, targetSlide, Time.unscaledDeltaTime * 5f);
        panelCanvasGroup.alpha = Mathf.Lerp(0.35f, 1f, currentSlide);
        panelRoot.anchoredPosition = new Vector2(Mathf.Lerp(-308f, 16f, currentSlide), -304f);
        handleRoot.anchoredPosition = new Vector2(Mathf.Lerp(16f, 316f, currentSlide), -304f);
    }

    private void AttachMissionSystemIfNeeded()
    {
        if (missionSystem == null)
        {
            missionSystem = FindFirstObjectByType<DailyMissionSystem>();
            if (missionSystem != null)
            {
                missionSystem.MissionsChanged += RefreshEntries;
            }
        }

        RefreshEntries();
    }

    private void RefreshEntries()
    {
        EnsureUiExists();
        IReadOnlyList<DailyMissionRuntime> missions = missionSystem != null ? missionSystem.ActiveMissions : null;
        for (int i = 0; i < entries.Count; i++)
        {
            MissionEntryWidgets entry = entries[i];
            if (missions == null || i >= missions.Count)
            {
                entry.nameText.text = "--";
                entry.descriptionText.text = string.Empty;
                entry.progressText.text = "0/0";
                entry.fillImage.fillAmount = 0f;
                entry.checkText.text = string.Empty;
                entry.strikeImage.enabled = false;
                continue;
            }

            DailyMissionRuntime mission = missions[i];
            entry.nameText.text = mission.displayName;
            entry.descriptionText.text = GetDescriptionForMission(mission);
            entry.progressText.text = mission.currentValue + "/" + mission.targetValue;
            entry.fillImage.fillAmount = mission.ProgressNormalized;
            entry.checkText.text = mission.completed ? "OK" : string.Empty;
            entry.strikeImage.enabled = mission.completed;
            entry.nameText.color = mission.completed ? new Color(0.7f, 1f, 0.7f, 1f) : Color.white;
        }
    }

    private string GetDescriptionForMission(DailyMissionRuntime mission)
    {
        if (mission == null)
        {
            return string.Empty;
        }

        if (mission.missionType == DailyMissionType.Resistance)
        {
            return "Derrota 20 enemigos";
        }

        if (mission.missionType == DailyMissionType.Precision)
        {
            return "10 bajas sin recibir dano en una sala";
        }

        if (mission.missionType == DailyMissionType.Exploration)
        {
            return "Visita todas las salas del piso";
        }

        return "Limpia 3 salas seguidas sin usar items";
    }

    private void EnsureUiExists()
    {
        if (overlayCanvas != null && panelRoot != null && panelCanvasGroup != null && handleRoot != null && entries.Count == 3)
        {
            return;
        }

        uiFont = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Canvas hostCanvas = FindOverlayCanvas();
        Transform overlay = hostCanvas.transform.Find("MissionPanelOverlay");
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject("MissionPanelOverlay", typeof(RectTransform));
            overlay = overlayObject.transform;
            overlay.SetParent(hostCanvas.transform, false);
        }

        overlayCanvas = overlay.GetComponent<Canvas>();
        if (overlayCanvas == null)
        {
            overlayCanvas = overlay.gameObject.AddComponent<Canvas>();
        }
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 109;

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        handleRoot = EnsureRect(overlay, "Handle", new Vector2(16f, -304f), new Vector2(118f, 28f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        Image handleImage = handleRoot.GetComponent<Image>();
        if (handleImage == null)
        {
            handleImage = handleRoot.gameObject.AddComponent<Image>();
        }
        handleImage.sprite = HUDSpriteFactory.WhiteSprite;
        handleImage.color = new Color(0f, 0f, 0f, 0.8f);
        Outline handleOutline = handleRoot.GetComponent<Outline>();
        if (handleOutline == null)
        {
            handleOutline = handleRoot.gameObject.AddComponent<Outline>();
        }
        handleOutline.effectColor = new Color(0f, 0.75f, 1f, 1f);
        handleOutline.effectDistance = new Vector2(1f, -1f);
        Text handleText = EnsureText(handleRoot, "Text", 13, TextAnchor.MiddleCenter, Color.white, Vector2.zero, handleRoot.sizeDelta);
        handleText.text = "[TAB] Misiones";

        panelRoot = EnsureRect(overlay, "MissionPanel", new Vector2(-308f, -304f), new Vector2(292f, 236f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        Image panelImage = panelRoot.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = panelRoot.gameObject.AddComponent<Image>();
        }
        panelImage.sprite = HUDSpriteFactory.WhiteSprite;
        panelImage.color = new Color(0f, 0f, 0f, 0.8f);
        Outline panelOutline = panelRoot.GetComponent<Outline>();
        if (panelOutline == null)
        {
            panelOutline = panelRoot.gameObject.AddComponent<Outline>();
        }
        panelOutline.effectColor = new Color(0f, 0.75f, 1f, 1f);
        panelOutline.effectDistance = new Vector2(1f, -1f);

        panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();
        }

        entries.Clear();
        for (int i = 0; i < 3; i++)
        {
            RectTransform entryRoot = EnsureRect(panelRoot, "Entry_" + i, new Vector2(10f, -12f - (i * 72f)), new Vector2(272f, 64f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            MissionEntryWidgets widgets = new MissionEntryWidgets();
            widgets.nameText = EnsureText(entryRoot, "Name", 16, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, 0f), new Vector2(192f, 18f));
            widgets.descriptionText = EnsureText(entryRoot, "Description", 12, TextAnchor.MiddleLeft, new Color(0.78f, 0.78f, 0.78f, 1f), new Vector2(0f, -18f), new Vector2(244f, 16f));
            widgets.progressText = EnsureText(entryRoot, "Progress", 12, TextAnchor.MiddleRight, Color.white, new Vector2(202f, -36f), new Vector2(64f, 14f));
            widgets.checkText = EnsureText(entryRoot, "Check", 20, TextAnchor.MiddleRight, new Color(0.25f, 1f, 0.45f, 1f), new Vector2(236f, 0f), new Vector2(24f, 18f));

            RectTransform barRoot = EnsureRect(entryRoot, "Bar", new Vector2(0f, -42f), new Vector2(192f, 12f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            Image barBackground = barRoot.GetComponent<Image>();
            if (barBackground == null)
            {
                barBackground = barRoot.gameObject.AddComponent<Image>();
            }
            barBackground.sprite = HUDSpriteFactory.WhiteSprite;
            barBackground.color = new Color(0.08f, 0.08f, 0.08f, 1f);

            widgets.fillImage = EnsureImage(barRoot, "Fill", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            widgets.fillImage.sprite = HUDSpriteFactory.WhiteSprite;
            widgets.fillImage.type = Image.Type.Filled;
            widgets.fillImage.fillMethod = Image.FillMethod.Horizontal;
            widgets.fillImage.fillOrigin = 0;
            widgets.fillImage.fillAmount = 0f;
            widgets.fillImage.color = new Color(0.29f, 0.56f, 0.85f, 1f);

            widgets.strikeImage = EnsureImage(entryRoot, "Strike", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, -9f), new Vector2(188f, 2f));
            widgets.strikeImage.sprite = HUDSpriteFactory.WhiteSprite;
            widgets.strikeImage.color = new Color(0.25f, 1f, 0.45f, 1f);
            widgets.strikeImage.enabled = false;

            entries.Add(widgets);
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
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
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
        if (sizeDelta != Vector2.zero)
        {
            rect.sizeDelta = sizeDelta;
        }
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
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);
        return text;
    }
}
