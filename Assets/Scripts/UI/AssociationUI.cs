using UnityEngine;
using UnityEngine.UI;

public class AssociationUI : MonoBehaviour
{
    private const int DefaultScenarioIndex = 1;

    private sealed class ScenarioCardWidgets
    {
        public RectTransform root;
        public Image frame;
        public RawImage art;
        public Text titleText;
        public Text descriptionText;
        public Text statusText;
        public Button button;
    }

    private struct ScenarioOption
    {
        public ScenarioOption(ScenarioId id, string title, string description, string resourcePath, string statusText, string actionLabel, bool startsRun, Color accent)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.resourcePath = resourcePath;
            this.statusText = statusText;
            this.actionLabel = actionLabel;
            this.startsRun = startsRun;
            this.accent = accent;
        }

        public readonly ScenarioId id;
        public readonly string title;
        public readonly string description;
        public readonly string resourcePath;
        public readonly string statusText;
        public readonly string actionLabel;
        public readonly bool startsRun;
        public readonly Color accent;
    }

    private static readonly ScenarioOption[] ScenarioOptions =
    {
        new ScenarioOption(
            ScenarioId.CityOfArken,
            "Ciudad de Arken",
            "Zona segura. Revisa mejoras, curacion y contratos antes de entrar a la grieta.",
            "ScenarioCards/city_of_arken",
            "HUB",
            "Permanecer",
            false,
            new Color(0.55f, 0.78f, 1f, 1f)),
        new ScenarioOption(
            ScenarioId.CursedDungeon,
            "Mazmorra maldita",
            "Ruta principal de combate. Salas corruptas, enemigos rapidos y recompensas estables.",
            "ScenarioCards/cursed_dungeon",
            "RUN",
            "Entrar",
            true,
            new Color(0.2f, 0.85f, 1f, 1f)),
        new ScenarioOption(
            ScenarioId.AbyssSanctuary,
            "Santuario del abismo",
            "Zona final de alto riesgo. Energia inestable, guardianes mayores y mejor botin.",
            "ScenarioCards/final_dark_fantasy_abyss_sanctuary",
            "ALTO RIESGO",
            "Entrar",
            true,
            new Color(0.86f, 0.28f, 1f, 1f))
    };

    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private RectTransform cardsRoot;
    [SerializeField] private Text npcDialogueText;
    [SerializeField] private Text missionsText;
    [SerializeField] private Button startRunButton;
    [SerializeField] private Text startRunButtonText;

    private readonly string[] npcLines =
    {
        "Elige bien. Cada ruta cobra distinto.",
        "La grieta cambia, pero tu deuda no.",
        "Tres rutas abiertas. Una mala decision tambien cuenta como valentia."
    };

    private readonly ScenarioCardWidgets[] scenarioCards = new ScenarioCardWidgets[ScenarioOptions.Length];
    private Font uiFont;
    private bool visible;
    private int selectedScenarioIndex = DefaultScenarioIndex;

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
        if (panelRoot != null)
        {
            panelRoot.gameObject.SetActive(false);
        }
    }

    public void Refresh()
    {
        EnsureUi();
        RunManager.Instance.EnsureAssociationMissions();
        selectedScenarioIndex = GetScenarioIndex(RunManager.Instance.SelectedScenario);
        npcDialogueText.text = npcLines[Random.Range(0, npcLines.Length)];
        RefreshMissionList();
        RefreshScenarioCards();
        ConfigureStartButton();
    }

    private void RefreshMissionList()
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.AppendLine("RANGO OFICIAL: E");
        builder.AppendLine("CONTRATOS ACTIVOS:");
        for (int i = 0; i < RunManager.Instance.CurrentAssociationMissions.Count; i++)
        {
            AssociationMissionOffer mission = RunManager.Instance.CurrentAssociationMissions[i];
            builder.AppendLine((i + 1) + ". " + mission.title + " - " + mission.rewardGold + " cristales");
        }

        missionsText.text = builder.ToString();
    }

    private void SelectScenario(int scenarioIndex)
    {
        selectedScenarioIndex = Mathf.Clamp(scenarioIndex, 0, ScenarioOptions.Length - 1);
        RunManager.Instance.SelectScenario(ScenarioOptions[selectedScenarioIndex].id);
        RefreshScenarioCards();
        ConfigureStartButton();
    }

    private void ConfigureStartButton()
    {
        ScenarioOption option = ScenarioOptions[selectedScenarioIndex];
        startRunButtonText.text = option.actionLabel;
        startRunButton.onClick.RemoveAllListeners();
        startRunButton.onClick.AddListener(() =>
        {
            Hide();
            if (option.startsRun)
            {
                RunManager.Instance.StartRun(option.id);
            }
        });

        Image buttonImage = startRunButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = option.startsRun
                ? new Color(option.accent.r, option.accent.g, option.accent.b, 0.92f)
                : new Color(0.28f, 0.34f, 0.44f, 0.92f);
        }
    }

    private void RefreshScenarioCards()
    {
        for (int i = 0; i < ScenarioOptions.Length; i++)
        {
            ScenarioCardWidgets card = scenarioCards[i];
            if (card == null)
            {
                continue;
            }

            ScenarioOption option = ScenarioOptions[i];
            bool selected = i == selectedScenarioIndex;
            card.frame.color = selected ? new Color(0.12f, 0.16f, 0.22f, 0.98f) : new Color(0.055f, 0.06f, 0.075f, 0.96f);
            card.titleText.color = selected ? Color.white : new Color(0.78f, 0.82f, 0.9f, 1f);
            card.statusText.color = selected ? option.accent : new Color(0.52f, 0.56f, 0.64f, 1f);

            Outline outline = card.root.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = selected ? option.accent : new Color(0f, 0f, 0f, 0.75f);
                outline.effectDistance = selected ? new Vector2(3f, -3f) : new Vector2(1f, -1f);
            }
        }
    }

    private void EnsureUi()
    {
        if (overlayCanvas != null && panelRoot != null && cardsRoot != null && npcDialogueText != null && missionsText != null && startRunButton != null && startRunButtonText != null)
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

        panelRoot = EnsureRect(overlay, "Panel", Vector2.zero, new Vector2(1120f, 620f), new Vector2(0.5f, 0.5f));
        Image background = panelRoot.GetComponent<Image>();
        if (background == null)
        {
            background = panelRoot.gameObject.AddComponent<Image>();
        }

        background.sprite = HUDSpriteFactory.WhiteSprite;
        background.color = new Color(0.045f, 0.047f, 0.06f, 0.97f);

        Text title = EnsureText(panelRoot, "Title", 28, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, 270f), new Vector2(680f, 36f), new Vector2(0.5f, 0.5f));
        title.text = "SELECCION DE ESCENARIO";

        npcDialogueText = EnsureText(panelRoot, "NpcLine", 17, TextAnchor.MiddleCenter, new Color(0.78f, 0.87f, 1f, 1f), new Vector2(0f, 235f), new Vector2(900f, 28f), new Vector2(0.5f, 0.5f));

        cardsRoot = EnsureRect(panelRoot, "ScenarioCards", new Vector2(0f, 62f), new Vector2(1040f, 286f), new Vector2(0.5f, 0.5f));
        EnsureScenarioCards();

        missionsText = EnsureText(panelRoot, "MissionList", 15, TextAnchor.UpperLeft, new Color(0.88f, 0.91f, 0.96f, 1f), new Vector2(-510f, -170f), new Vector2(700f, 132f), new Vector2(0f, 1f));

        RectTransform buttonRect = EnsureRect(panelRoot, "StartRun", new Vector2(390f, -246f), new Vector2(230f, 46f), new Vector2(0.5f, 0.5f));
        Image buttonImage = buttonRect.GetComponent<Image>();
        if (buttonImage == null)
        {
            buttonImage = buttonRect.gameObject.AddComponent<Image>();
        }

        buttonImage.sprite = HUDSpriteFactory.WhiteSprite;
        startRunButton = buttonRect.GetComponent<Button>();
        if (startRunButton == null)
        {
            startRunButton = buttonRect.gameObject.AddComponent<Button>();
        }

        startRunButtonText = EnsureText(buttonRect, "Text", 18, TextAnchor.MiddleCenter, Color.white, Vector2.zero, buttonRect.sizeDelta, new Vector2(0.5f, 0.5f));
        panelRoot.gameObject.SetActive(false);
    }

    private void EnsureScenarioCards()
    {
        for (int i = 0; i < ScenarioOptions.Length; i++)
        {
            int capturedIndex = i;
            ScenarioOption option = ScenarioOptions[i];
            RectTransform cardRoot = EnsureRect(cardsRoot, "ScenarioCard" + i, new Vector2(-350f + (i * 350f), 0f), new Vector2(320f, 270f), new Vector2(0.5f, 0.5f));

            Image frame = cardRoot.GetComponent<Image>();
            if (frame == null)
            {
                frame = cardRoot.gameObject.AddComponent<Image>();
            }

            frame.sprite = HUDSpriteFactory.WhiteSprite;

            Outline outline = cardRoot.GetComponent<Outline>();
            if (outline == null)
            {
                outline = cardRoot.gameObject.AddComponent<Outline>();
            }

            Button button = cardRoot.GetComponent<Button>();
            if (button == null)
            {
                button = cardRoot.gameObject.AddComponent<Button>();
            }

            button.targetGraphic = frame;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectScenario(capturedIndex));

            RectTransform artRect = EnsureRect(cardRoot, "Art", new Vector2(0f, 42f), new Vector2(300f, 168f), new Vector2(0.5f, 0.5f));
            RawImage art = artRect.GetComponent<RawImage>();
            if (art == null)
            {
                art = artRect.gameObject.AddComponent<RawImage>();
            }

            Texture2D texture = Resources.Load<Texture2D>(option.resourcePath);
            art.texture = texture;
            art.color = texture != null ? Color.white : new Color(0.09f, 0.1f, 0.13f, 1f);
            art.raycastTarget = false;

            Text title = EnsureText(cardRoot, "Title", 18, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, -58f), new Vector2(286f, 26f), new Vector2(0.5f, 0.5f));
            title.text = option.title;

            Text description = EnsureText(cardRoot, "Description", 12, TextAnchor.UpperCenter, new Color(0.78f, 0.81f, 0.86f, 1f), new Vector2(0f, -94f), new Vector2(284f, 58f), new Vector2(0.5f, 1f));
            description.text = option.description;
            description.resizeTextForBestFit = true;
            description.resizeTextMinSize = 9;
            description.resizeTextMaxSize = 12;

            Text status = EnsureText(cardRoot, "Status", 12, TextAnchor.MiddleCenter, option.accent, new Vector2(0f, -130f), new Vector2(260f, 18f), new Vector2(0.5f, 0.5f));
            status.text = option.statusText;

            scenarioCards[i] = new ScenarioCardWidgets
            {
                root = cardRoot,
                frame = frame,
                art = art,
                titleText = title,
                descriptionText = description,
                statusText = status,
                button = button
            };
        }
    }

    private int GetScenarioIndex(ScenarioId scenarioId)
    {
        for (int i = 0; i < ScenarioOptions.Length; i++)
        {
            if (ScenarioOptions[i].id == scenarioId)
            {
                return i;
            }
        }

        return DefaultScenarioIndex;
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
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

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
