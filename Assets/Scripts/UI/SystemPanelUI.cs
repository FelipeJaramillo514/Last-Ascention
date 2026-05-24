using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SystemPanelUI : MonoBehaviour
{
    private sealed class StatRowWidgets
    {
        public string statName;
        public Text nameText;
        public Text deltaText;
        public Image fillImage;
        public Text valueText;
    }

    public static SystemPanelUI Instance { get; private set; }

    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private RectTransform revealMaskRect;
    [SerializeField] private RectTransform scanlineRect;
    [SerializeField] private Text titleText;
    [SerializeField] private Text subtitleText;
    [SerializeField] private Text skillPointsText;
    [SerializeField] private ParticleSystem panelParticles;

    private readonly List<StatRowWidgets> statRows = new List<StatRowWidgets>();

    private Coroutine sequenceRoutine;
    private Font uiFont;
    private float revealTargetHeight = 224f;
    private int lastHandledLevel = -1;

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

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
    }

    public void ShowLevelUp(StatChanges changes)
    {
        EnsureUiExists();
        if (UIModalGate.IsBlockingInteraction)
        {
            return;
        }

        if (!changes.HasAnyChange)
        {
            return;
        }

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
        }

        sequenceRoutine = StartCoroutine(ShowLevelUpRoutine(changes));
    }

    private void OnPlayerLevelUp(PlayerLevelUpEvent levelUpEvent)
    {
        if (levelUpEvent == null || !levelUpEvent.statChanges.HasAnyChange)
        {
            return;
        }

        if (sequenceRoutine != null && lastHandledLevel == levelUpEvent.newLevel)
        {
            return;
        }

        ShowLevelUp(levelUpEvent.statChanges);
    }

    private IEnumerator ShowLevelUpRoutine(StatChanges changes)
    {
        lastHandledLevel = changes.newLevel;
        float previousTimeScale = Time.timeScale;
        Time.timeScale = 0.05f;

        panelRoot.gameObject.SetActive(true);
        panelCanvasGroup.alpha = 0f;
        revealMaskRect.sizeDelta = new Vector2(revealMaskRect.sizeDelta.x, 0f);
        scanlineRect.gameObject.SetActive(true);
        scanlineRect.anchoredPosition = new Vector2(0f, revealTargetHeight * 0.5f);
        titleText.text = "■ NIVEL AUMENTADO ■";
        subtitleText.text = string.Format("NIVEL: {0} → {1}", changes.previousLevel, changes.newLevel);
        skillPointsText.gameObject.SetActive(changes.skillPointsDelta > 0);
        skillPointsText.text = changes.skillPointsDelta > 0 ? "PUNTOS DE HABILIDAD: +1" : string.Empty;

        SetupRowsInitial(changes);
        if (panelParticles != null)
        {
            panelParticles.Play();
        }

        float fadeInElapsed = 0f;
        while (fadeInElapsed < 0.3f)
        {
            fadeInElapsed += Time.unscaledDeltaTime;
            panelCanvasGroup.alpha = Mathf.Clamp01(fadeInElapsed / 0.3f);
            yield return null;
        }
        panelCanvasGroup.alpha = 1f;

        float revealElapsed = 0f;
        while (revealElapsed < 0.5f)
        {
            revealElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(revealElapsed / 0.5f);
            revealMaskRect.sizeDelta = new Vector2(revealMaskRect.sizeDelta.x, Mathf.Lerp(0f, revealTargetHeight, t));
            scanlineRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(revealTargetHeight * 0.5f, -revealTargetHeight * 0.5f, t));
            yield return null;
        }
        revealMaskRect.sizeDelta = new Vector2(revealMaskRect.sizeDelta.x, revealTargetHeight);
        scanlineRect.gameObject.SetActive(false);

        float countElapsed = 0f;
        while (countElapsed < 0.4f)
        {
            countElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(countElapsed / 0.4f);
            UpdateRowValues(changes, t);
            yield return null;
        }
        UpdateRowValues(changes, 1f);

        float waitElapsed = 0f;
        while (waitElapsed < 4f)
        {
            if (AnyAdvanceInputPressed())
            {
                break;
            }

            waitElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        float fadeOutElapsed = 0f;
        while (fadeOutElapsed < 0.2f)
        {
            fadeOutElapsed += Time.unscaledDeltaTime;
            panelCanvasGroup.alpha = 1f - Mathf.Clamp01(fadeOutElapsed / 0.2f);
            yield return null;
        }

        if (panelParticles != null)
        {
            panelParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        panelCanvasGroup.alpha = 0f;
        panelRoot.gameObject.SetActive(false);
        Time.timeScale = previousTimeScale;
        sequenceRoutine = null;
    }

    private void SetupRowsInitial(StatChanges changes)
    {
        for (int i = 0; i < statRows.Count; i++)
        {
            StatRowWidgets row = statRows[i];
            row.valueText.text = "0";
            row.fillImage.fillAmount = 0f;
        }

        ConfigureRow(statRows[0], "Fuerza", changes.strengthDelta, changes.currentStrength);
        ConfigureRow(statRows[1], "Agilidad", changes.agilityDelta, changes.currentAgility);
        ConfigureRow(statRows[2], "Resistencia", changes.resistanceDelta, changes.currentResistance);
        ConfigureRow(statRows[3], "Percepcion", changes.perceptionDelta, changes.currentPerception);
    }

    private void ConfigureRow(StatRowWidgets row, string label, float delta, float currentValue)
    {
        row.statName = label;
        row.nameText.text = label;
        row.deltaText.text = delta > 0f ? "+" + delta.ToString("0") : "+0";
        row.deltaText.color = new Color(0f, 1f, 0.53f, 1f);
        row.valueText.text = "0";
        row.fillImage.fillAmount = 0f;
        row.fillImage.color = new Color(0.29f, 0.56f, 0.85f, 1f);
    }

    private void UpdateRowValues(StatChanges changes, float t)
    {
        SetRowValue(statRows[0], Mathf.Lerp(0f, changes.currentStrength, t));
        SetRowValue(statRows[1], Mathf.Lerp(0f, changes.currentAgility, t));
        SetRowValue(statRows[2], Mathf.Lerp(0f, changes.currentResistance, t));
        SetRowValue(statRows[3], Mathf.Lerp(0f, changes.currentPerception, t));
    }

    private void SetRowValue(StatRowWidgets row, float value)
    {
        row.valueText.text = value.ToString("0.0");
        row.fillImage.fillAmount = Mathf.Clamp01(value / 20f);
    }

    private bool AnyAdvanceInputPressed()
    {
        bool keyboardPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool mousePressed = Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame);
        bool gamepadPressed = Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.buttonNorth.wasPressedThisFrame || Gamepad.current.buttonEast.wasPressedThisFrame || Gamepad.current.buttonWest.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame);
        return keyboardPressed || mousePressed || gamepadPressed;
    }

    private void EnsureUiExists()
    {
        if (overlayCanvas != null && panelCanvasGroup != null && panelRoot != null && revealMaskRect != null && titleText != null && subtitleText != null && skillPointsText != null && panelParticles != null && statRows.Count == 4)
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

        Transform overlay = hostCanvas.transform.Find("SystemPanelOverlay");
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject("SystemPanelOverlay", typeof(RectTransform));
            overlay = overlayObject.transform;
            overlay.SetParent(hostCanvas.transform, false);
        }

        overlayCanvas = overlay.GetComponent<Canvas>();
        if (overlayCanvas == null)
        {
            overlayCanvas = overlay.gameObject.AddComponent<Canvas>();
        }
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 100;
        if (overlay.GetComponent<GraphicRaycaster>() == null)
        {
            overlay.gameObject.AddComponent<GraphicRaycaster>();
        }

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        if (overlayRect == null)
        {
            overlayRect = overlay.gameObject.AddComponent<RectTransform>();
        }
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        if (panelRoot == null)
        {
            Transform panelTransform = overlay.Find("LevelUpPanel");
            if (panelTransform == null)
            {
            GameObject panelObject = new GameObject("LevelUpPanel", typeof(RectTransform));
                panelTransform = panelObject.transform;
                panelTransform.SetParent(overlay, false);
            }
            panelRoot = panelTransform as RectTransform;
            if (panelRoot == null)
            {
                panelRoot = panelTransform.gameObject.AddComponent<RectTransform>();
            }
        }

        panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
        panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
        panelRoot.pivot = new Vector2(0.5f, 0.5f);
        panelRoot.sizeDelta = new Vector2(620f, 340f);
        panelRoot.anchoredPosition = Vector2.zero;

        Image panelImage = panelRoot.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = panelRoot.gameObject.AddComponent<Image>();
        }
        panelImage.sprite = panelSprite;
        panelImage.color = new Color(0f, 0f, 0f, 0.85f);

        Outline panelOutline = panelRoot.GetComponent<Outline>();
        if (panelOutline == null)
        {
            panelOutline = panelRoot.gameObject.AddComponent<Outline>();
        }
        panelOutline.effectColor = new Color(0f, 0.75f, 1f, 1f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();
        }

        Transform titleTransform = panelRoot.Find("Title");
        if (titleTransform == null)
        {
            GameObject titleObject = new GameObject("Title", typeof(RectTransform));
            titleTransform = titleObject.transform;
            titleTransform.SetParent(panelRoot, false);
        }
        titleText = EnsureText(titleTransform.gameObject, uiFont, 30, TextAnchor.MiddleCenter, Color.white);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(520f, 40f);
        titleRect.anchoredPosition = new Vector2(0f, -18f);

        Transform subtitleTransform = panelRoot.Find("Subtitle");
        if (subtitleTransform == null)
        {
            GameObject subtitleObject = new GameObject("Subtitle", typeof(RectTransform));
            subtitleTransform = subtitleObject.transform;
            subtitleTransform.SetParent(panelRoot, false);
        }
        subtitleText = EnsureText(subtitleTransform.gameObject, uiFont, 22, TextAnchor.MiddleCenter, new Color(0.5f, 0.85f, 1f, 1f));
        RectTransform subtitleRect = subtitleText.rectTransform;
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.sizeDelta = new Vector2(520f, 28f);
        subtitleRect.anchoredPosition = new Vector2(0f, -56f);

        Transform separatorTransform = panelRoot.Find("Separator");
        if (separatorTransform == null)
        {
            GameObject separatorObject = new GameObject("Separator", typeof(RectTransform));
            separatorTransform = separatorObject.transform;
            separatorTransform.SetParent(panelRoot, false);
        }
        Image separatorImage = separatorTransform.GetComponent<Image>();
        if (separatorImage == null)
        {
            separatorImage = separatorTransform.gameObject.AddComponent<Image>();
        }
        separatorImage.sprite = panelSprite;
        separatorImage.color = new Color(0f, 0.75f, 1f, 1f);
        RectTransform separatorRect = separatorImage.rectTransform;
        separatorRect.anchorMin = new Vector2(0.5f, 1f);
        separatorRect.anchorMax = new Vector2(0.5f, 1f);
        separatorRect.pivot = new Vector2(0.5f, 1f);
        separatorRect.sizeDelta = new Vector2(520f, 3f);
        separatorRect.anchoredPosition = new Vector2(0f, -92f);

        Transform maskTransform = panelRoot.Find("RevealMask");
        if (maskTransform == null)
        {
            GameObject maskObject = new GameObject("RevealMask", typeof(RectTransform));
            maskTransform = maskObject.transform;
            maskTransform.SetParent(panelRoot, false);
        }
        revealMaskRect = maskTransform as RectTransform;
        if (revealMaskRect == null)
        {
            revealMaskRect = maskTransform.gameObject.AddComponent<RectTransform>();
        }
        revealMaskRect.anchorMin = new Vector2(0.5f, 1f);
        revealMaskRect.anchorMax = new Vector2(0.5f, 1f);
        revealMaskRect.pivot = new Vector2(0.5f, 1f);
        revealMaskRect.sizeDelta = new Vector2(540f, 0f);
        revealMaskRect.anchoredPosition = new Vector2(0f, -104f);
        Image maskImage = revealMaskRect.GetComponent<Image>();
        if (maskImage == null)
        {
            maskImage = revealMaskRect.gameObject.AddComponent<Image>();
        }
        maskImage.sprite = panelSprite;
        maskImage.color = new Color(1f, 1f, 1f, 0.02f);
        Mask mask = revealMaskRect.GetComponent<Mask>();
        if (mask == null)
        {
            mask = revealMaskRect.gameObject.AddComponent<Mask>();
        }
        mask.showMaskGraphic = false;

        Transform contentTransform = revealMaskRect.Find("Content");
        if (contentTransform == null)
        {
            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentTransform = contentObject.transform;
            contentTransform.SetParent(revealMaskRect, false);
        }
        RectTransform contentRect = contentTransform as RectTransform;
        if (contentRect == null)
        {
            contentRect = contentTransform.gameObject.AddComponent<RectTransform>();
        }
        contentRect.anchorMin = new Vector2(0.5f, 1f);
        contentRect.anchorMax = new Vector2(0.5f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(520f, revealTargetHeight);
        contentRect.anchoredPosition = new Vector2(0f, 0f);

        statRows.Clear();
        string[] labels = { "Fuerza", "Agilidad", "Resistencia", "Percepcion" };
        for (int i = 0; i < labels.Length; i++)
        {
            Transform rowTransform = contentRect.Find("Row_" + labels[i]);
            if (rowTransform == null)
            {
            GameObject rowObject = new GameObject("Row_" + labels[i], typeof(RectTransform));
                rowTransform = rowObject.transform;
                rowTransform.SetParent(contentRect, false);
            }

            RectTransform rowRect = rowTransform as RectTransform;
            if (rowRect == null)
            {
                rowRect = rowTransform.gameObject.AddComponent<RectTransform>();
            }
            rowRect.anchorMin = new Vector2(0.5f, 1f);
            rowRect.anchorMax = new Vector2(0.5f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(500f, 42f);
            rowRect.anchoredPosition = new Vector2(0f, -8f - (i * 48f));

            StatRowWidgets widgets = new StatRowWidgets();
            widgets.statName = labels[i];

            Transform nameTransform = rowTransform.Find("Name");
            if (nameTransform == null)
            {
            GameObject nameObject = new GameObject("Name", typeof(RectTransform));
                nameTransform = nameObject.transform;
                nameTransform.SetParent(rowTransform, false);
            }
            widgets.nameText = EnsureText(nameTransform.gameObject, uiFont, 20, TextAnchor.MiddleLeft, Color.white);
            RectTransform nameRect = widgets.nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(0f, 0.5f);
            nameRect.pivot = new Vector2(0f, 0.5f);
            nameRect.sizeDelta = new Vector2(150f, 24f);
            nameRect.anchoredPosition = new Vector2(0f, 0f);

            Transform deltaTransform = rowTransform.Find("Delta");
            if (deltaTransform == null)
            {
            GameObject deltaObject = new GameObject("Delta", typeof(RectTransform));
                deltaTransform = deltaObject.transform;
                deltaTransform.SetParent(rowTransform, false);
            }
            widgets.deltaText = EnsureText(deltaTransform.gameObject, uiFont, 18, TextAnchor.MiddleLeft, new Color(0f, 1f, 0.53f, 1f));
            RectTransform deltaRect = widgets.deltaText.rectTransform;
            deltaRect.anchorMin = new Vector2(0f, 0.5f);
            deltaRect.anchorMax = new Vector2(0f, 0.5f);
            deltaRect.pivot = new Vector2(0f, 0.5f);
            deltaRect.sizeDelta = new Vector2(70f, 24f);
            deltaRect.anchoredPosition = new Vector2(158f, 0f);

            Transform barBackgroundTransform = rowTransform.Find("BarBackground");
            if (barBackgroundTransform == null)
            {
            GameObject barBackgroundObject = new GameObject("BarBackground", typeof(RectTransform));
                barBackgroundTransform = barBackgroundObject.transform;
                barBackgroundTransform.SetParent(rowTransform, false);
            }
            Image barBackground = barBackgroundTransform.GetComponent<Image>();
            if (barBackground == null)
            {
                barBackground = barBackgroundTransform.gameObject.AddComponent<Image>();
            }
            barBackground.sprite = panelSprite;
            barBackground.color = new Color(0.09f, 0.09f, 0.09f, 1f);
            RectTransform barBackgroundRect = barBackground.rectTransform;
            barBackgroundRect.anchorMin = new Vector2(0f, 0.5f);
            barBackgroundRect.anchorMax = new Vector2(0f, 0.5f);
            barBackgroundRect.pivot = new Vector2(0f, 0.5f);
            barBackgroundRect.sizeDelta = new Vector2(180f, 16f);
            barBackgroundRect.anchoredPosition = new Vector2(236f, 0f);

            Transform fillTransform = barBackgroundTransform.Find("Fill");
            if (fillTransform == null)
            {
            GameObject fillObject = new GameObject("Fill", typeof(RectTransform));
                fillTransform = fillObject.transform;
                fillTransform.SetParent(barBackgroundTransform, false);
            }
            widgets.fillImage = fillTransform.GetComponent<Image>();
            if (widgets.fillImage == null)
            {
                widgets.fillImage = fillTransform.gameObject.AddComponent<Image>();
            }
            RectTransform fillRect = widgets.fillImage.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            widgets.fillImage.sprite = panelSprite;
            widgets.fillImage.type = Image.Type.Filled;
            widgets.fillImage.fillMethod = Image.FillMethod.Horizontal;
            widgets.fillImage.fillOrigin = 0;
            widgets.fillImage.fillAmount = 0f;
            widgets.fillImage.color = new Color(0.29f, 0.56f, 0.85f, 1f);

            Transform valueTransform = rowTransform.Find("Value");
            if (valueTransform == null)
            {
            GameObject valueObject = new GameObject("Value", typeof(RectTransform));
                valueTransform = valueObject.transform;
                valueTransform.SetParent(rowTransform, false);
            }
            widgets.valueText = EnsureText(valueTransform.gameObject, uiFont, 18, TextAnchor.MiddleRight, Color.white);
            RectTransform valueRect = widgets.valueText.rectTransform;
            valueRect.anchorMin = new Vector2(1f, 0.5f);
            valueRect.anchorMax = new Vector2(1f, 0.5f);
            valueRect.pivot = new Vector2(1f, 0.5f);
            valueRect.sizeDelta = new Vector2(70f, 24f);
            valueRect.anchoredPosition = new Vector2(0f, 0f);

            statRows.Add(widgets);
        }

        Transform skillTransform = contentRect.Find("SkillPoints");
        if (skillTransform == null)
        {
            GameObject skillObject = new GameObject("SkillPoints", typeof(RectTransform));
            skillTransform = skillObject.transform;
            skillTransform.SetParent(contentRect, false);
        }
        skillPointsText = EnsureText(skillTransform.gameObject, uiFont, 20, TextAnchor.MiddleLeft, new Color(1f, 0.84f, 0f, 1f));
        RectTransform skillRect = skillPointsText.rectTransform;
        skillRect.anchorMin = new Vector2(0.5f, 1f);
        skillRect.anchorMax = new Vector2(0.5f, 1f);
        skillRect.pivot = new Vector2(0.5f, 1f);
        skillRect.sizeDelta = new Vector2(500f, 30f);
        skillRect.anchoredPosition = new Vector2(0f, -206f);

        Transform scanlineTransform = panelRoot.Find("Scanline");
        if (scanlineTransform == null)
        {
            GameObject scanlineObject = new GameObject("Scanline", typeof(RectTransform));
            scanlineTransform = scanlineObject.transform;
            scanlineTransform.SetParent(panelRoot, false);
        }
        Image scanlineImage = scanlineTransform.GetComponent<Image>();
        if (scanlineImage == null)
        {
            scanlineImage = scanlineTransform.gameObject.AddComponent<Image>();
        }
        scanlineImage.sprite = panelSprite;
        scanlineImage.color = new Color(1f, 1f, 1f, 0.95f);
        scanlineRect = scanlineTransform as RectTransform;
        if (scanlineRect == null)
        {
            scanlineRect = scanlineTransform.gameObject.AddComponent<RectTransform>();
        }
        scanlineRect.anchorMin = new Vector2(0.5f, 1f);
        scanlineRect.anchorMax = new Vector2(0.5f, 1f);
        scanlineRect.pivot = new Vector2(0.5f, 0.5f);
        scanlineRect.sizeDelta = new Vector2(540f, 3f);

        Transform particleTransform = panelRoot.Find("PanelParticles");
        if (particleTransform == null)
        {
            GameObject particleObject = new GameObject("PanelParticles", typeof(RectTransform));
            particleTransform = particleObject.transform;
            particleTransform.SetParent(panelRoot, false);
        }
        panelParticles = particleTransform.GetComponent<ParticleSystem>();
        if (panelParticles == null)
        {
            panelParticles = particleTransform.gameObject.AddComponent<ParticleSystem>();
        }
        panelParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = panelParticles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1.2f;
        main.startLifetime = 1.2f;
        main.startSpeed = 12f;
        main.startSize = 4f;
        main.startColor = new Color(0f, 0.75f, 1f, 0.9f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var emission = panelParticles.emission;
        emission.rateOverTime = 20f;
        var shape = panelParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(520f, 260f, 1f);
        var velocity = panelParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(8f);
        ParticleSystemRenderer particleRenderer = panelParticles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sortingOrder = 101;
        particleRenderer.material = null;
        panelParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        panelRoot.gameObject.SetActive(false);
        panelCanvasGroup.alpha = 0f;
    }

    private Text EnsureText(GameObject target, Font font, int size, TextAnchor alignment, Color color)
    {
        Text text = target.GetComponent<Text>();
        if (text == null)
        {
            text = target.AddComponent<Text>();
        }
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        Outline outline = target.GetComponent<Outline>();
        if (outline == null)
        {
            outline = target.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);
        return text;
    }
}

