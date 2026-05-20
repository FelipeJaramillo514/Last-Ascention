using UnityEngine;
using UnityEngine.UI;

public class AssociationUI : MonoBehaviour
{
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private Text npcDialogueText;
    [SerializeField] private Text missionsText;
    [SerializeField] private Button startRunButton;

    private readonly string[] npcLines =
    {
        "Otra vez tu? Rango E, no te cansas de perder?",
        "Hoy hay misiones de nivel D... demasiado peligrosas para ti.",
        "El rango E no da para mucho, verdad?"
    };

    private Font uiFont;
    private bool visible;

    public bool IsVisible => visible;

    private void Awake()
    {
        EnsureUi();
    }

    public void Show()
    {
        EnsureUi();
        visible = true;
        panelRoot.gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        visible = false;
        panelRoot.gameObject.SetActive(false);
    }

    public void Refresh()
    {
        EnsureUi();
        RunManager.Instance.EnsureAssociationMissions();
        npcDialogueText.text = npcLines[Random.Range(0, npcLines.Length)];

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.AppendLine("RANGO OFICIAL: E");
        builder.AppendLine();
        for (int i = 0; i < RunManager.Instance.CurrentAssociationMissions.Count; i++)
        {
            AssociationMissionOffer mission = RunManager.Instance.CurrentAssociationMissions[i];
            builder.AppendLine((i + 1) + ". " + mission.title + " - " + mission.rewardGold + " cristales");
            builder.AppendLine("   " + mission.description);
        }

        missionsText.text = builder.ToString();

        startRunButton.onClick.RemoveAllListeners();
        startRunButton.onClick.AddListener(() =>
        {
            Hide();
            RunManager.Instance.StartRun();
        });
    }

    private void EnsureUi()
    {
        if (overlayCanvas != null && panelRoot != null && npcDialogueText != null && missionsText != null && startRunButton != null)
        {
            return;
        }

        uiFont = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Canvas canvas = FindOverlayCanvas();
        EnsureEventSystem();

        Transform overlay = canvas.transform.Find("AssociationOverlay");
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject("AssociationOverlay", typeof(RectTransform));
            overlay = overlayObject.transform;
            overlay.SetParent(canvas.transform, false);
        }

        overlayCanvas = overlay.GetComponent<Canvas>();
        if (overlayCanvas == null)
        {
            overlayCanvas = overlay.gameObject.AddComponent<Canvas>();
        }

        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 220;
        if (overlay.GetComponent<GraphicRaycaster>() == null)
        {
            overlay.gameObject.AddComponent<GraphicRaycaster>();
        }


        panelRoot = EnsureRect(overlay, "Panel", Vector2.zero, new Vector2(760f, 420f), new Vector2(0.5f, 0.5f));
        Image background = panelRoot.GetComponent<Image>();
        if (background == null)
        {
            background = panelRoot.gameObject.AddComponent<Image>();
        }

        background.sprite = HUDSpriteFactory.WhiteSprite;
        background.color = new Color(0.08f, 0.08f, 0.1f, 0.96f);

        Text title = EnsureText(panelRoot, "Title", 28, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, 162f), new Vector2(520f, 34f), new Vector2(0.5f, 0.5f));
        title.text = "ASOCIACION DE CAZADORES";
        npcDialogueText = EnsureText(panelRoot, "NpcLine", 18, TextAnchor.MiddleLeft, new Color(0.92f, 0.65f, 0.65f, 1f), new Vector2(-300f, 110f), new Vector2(600f, 30f), new Vector2(0f, 0.5f));
        missionsText = EnsureText(panelRoot, "MissionList", 17, TextAnchor.UpperLeft, Color.white, new Vector2(-300f, 74f), new Vector2(600f, 250f), new Vector2(0f, 1f));

        RectTransform buttonRect = EnsureRect(panelRoot, "StartRun", new Vector2(0f, -170f), new Vector2(180f, 42f), new Vector2(0.5f, 0.5f));
        Image buttonImage = buttonRect.GetComponent<Image>();
        if (buttonImage == null)
        {
            buttonImage = buttonRect.gameObject.AddComponent<Image>();
        }

        buttonImage.sprite = HUDSpriteFactory.WhiteSprite;
        buttonImage.color = new Color(0.25f, 0.45f, 0.85f, 0.95f);
        startRunButton = buttonRect.GetComponent<Button>();
        if (startRunButton == null)
        {
            startRunButton = buttonRect.gameObject.AddComponent<Button>();
        }

        Text buttonText = EnsureText(buttonRect, "Text", 18, TextAnchor.MiddleCenter, Color.white, Vector2.zero, buttonRect.sizeDelta, new Vector2(0.5f, 0.5f));
        buttonText.text = "Iniciar Run";

        panelRoot.gameObject.SetActive(false);
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

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
    }

    private RectTransform EnsureRect(Transform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
    {
        Transform target = parent.Find(name);
        if (target == null)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            target = child.transform;
            target.SetParent(parent, false);
        }

        RectTransform rect = target as RectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private Text EnsureText(Transform parent, string name, int fontSize, TextAnchor anchor, Color color, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
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
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        text.font = uiFont;
        text.fontSize = fontSize;
        text.alignment = anchor;
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
