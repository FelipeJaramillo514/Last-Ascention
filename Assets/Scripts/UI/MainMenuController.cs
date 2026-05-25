using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    private const string HubSceneName = "CityArken";
    private const string MenuBackgroundResourcePath = "Menu/MenuBackground";
    private const string TitleLogoResourcePath = "Menu/LastAscentionTitle";
    private const float MenuBackgroundPixelsPerUnit = 76.8f;
    private const float MenuCanvasWidth = 1280f;
    private const float MenuCanvasHeight = 720f;
    private const float EmbeddedButtonSlotX = 69f;
    private const float EmbeddedButtonSlotY = 518f;
    private const float EmbeddedButtonSlotWidth = 228f;
    private const float EmbeddedButtonSlotHeight = 38f;
    private const float EmbeddedButtonSlotSpacing = 48.75f;

    [SerializeField] private Camera menuCamera;
    [SerializeField] private Transform backgroundRoot;
    [SerializeField] private SpriteRenderer skyRenderer;
    [SerializeField] private SpriteRenderer skylineRenderer;
    [SerializeField] private ParticleSystem crackParticles;
    [SerializeField] private Image vignetteImage;
    [SerializeField] private Image panelBackgroundImage;
    [SerializeField] private RectTransform titlePanel;
    [SerializeField] private Image titleLogoImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private RectTransform scanlineRect;
    [SerializeField] private RectTransform buttonsPanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private TextMeshProUGUI newGameSubLabel;
    [SerializeField] private GameObject saveInfoPanel;
    [SerializeField] private TextMeshProUGUI saveInfoText;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button closeOptionsButton;
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private RectTransform confirmationCard;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;
    [SerializeField] private AudioSource uiAudioSource;

    private readonly List<SpriteRenderer> crackRenderers = new List<SpriteRenderer>();
    private readonly List<MainMenuButtonFeedback> buttonFeedbacks = new List<MainMenuButtonFeedback>();

    private SaveData loadedSave;
    private bool hasSave;
    private bool usingMenuBackgroundSprite;
    private bool titleUsesLogoSprite;
    private float skyOffset;
    private float skylineOffset;
    private Vector2 titleTargetPosition;
    private Vector2 titleLogoTargetPosition;
    private AudioClip hoverClip;
    private AudioClip clickClip;
    private MainMenuButtonFeedback lastHighlightedButton;
    private Sprite cachedWhiteSprite;
    private Sprite cachedMenuBackdropSprite;
    private Sprite cachedMenuBackgroundSprite;
    private Sprite cachedTitleLogoSprite;
    private static Material sharedMenuParticleMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            return;
        }

        if (FindFirstObjectByType<MainMenuController>() != null)
        {
            return;
        }

        new GameObject("MainMenuController").AddComponent<MainMenuController>();
    }

    private void Awake()
    {
        AutoWireSceneReferences();
        EnsureEventSystem();
        EnsureMenuAudioSource();
        CreateMenuClips();
        ConfigureScenePresentation();
        ConfigureButtons();
        ConfigureOptionsPanel();
        ConfigureConfirmationPanel();
        RefreshSaveState();
    }

    private void Start()
    {
        StartCoroutine(PlayTitleIntro());
        StartCoroutine(SubtitleTypewriterRoutine());
        StartCoroutine(CrackFlashLoop());

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(MusicTrackId.Menu, 1f);
        }
    }

    private void Update()
    {
        AnimateParallax();
        AnimateScanline();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (confirmationPanel != null && confirmationPanel.activeSelf)
            {
                OnCancelNewGamePressed();
                return;
            }

            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                SetOptionsVisible(false);
            }
        }
    }

    public void HandleButtonHighlighted(MainMenuButtonFeedback buttonFeedback)
    {
        if (buttonFeedback == null || buttonFeedback == lastHighlightedButton)
        {
            return;
        }

        lastHighlightedButton = buttonFeedback;
        PlayHoverSfx();
    }

    public void PlayClickSfx()
    {
        if (uiAudioSource != null && clickClip != null)
        {
            uiAudioSource.PlayOneShot(clickClip);
        }
    }

    public void OnContinuePressed()
    {
        if (!hasSave)
        {
            return;
        }

        StartCoroutine(LoadGame());
    }

    public void OnNewGamePressed()
    {
        if (hasSave)
        {
            SetConfirmationVisible(true);
            return;
        }

        StartCoroutine(NewGame());
    }

    public void OnOptionsPressed()
    {
        SetOptionsVisible(true);
    }

    public void OnExitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnConfirmNewGamePressed()
    {
        StartCoroutine(NewGame());
    }

    public void OnCancelNewGamePressed()
    {
        SetConfirmationVisible(false);
    }

    private IEnumerator LoadGame()
    {
        SaveData data = SaveSystem.Load();
        if (data == null)
        {
            hasSave = false;
            RefreshSaveState();
            yield break;
        }

        loadedSave = data;
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.InitFromSave(data);
            GameStateManager.Instance.isNewGame = false;
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.FadeToScene(HubSceneName, 0.5f);
        }
        else
        {
            SceneManager.LoadScene(HubSceneName);
        }

        yield return null;
    }

    private IEnumerator NewGame()
    {
        SetConfirmationVisible(false);
        SaveSystem.DeleteSave();

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.StartNewGame();
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.FadeToScene(HubSceneName, 0.5f);
        }
        else
        {
            SceneManager.LoadScene(HubSceneName);
        }

        yield return null;
    }

    private void RefreshSaveState()
    {
        loadedSave = SaveSystem.HasSave() ? SaveSystem.Load() : null;
        hasSave = loadedSave != null;

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(hasSave);
            continueButton.interactable = hasSave;
        }

        if (saveInfoPanel != null)
        {
            saveInfoPanel.SetActive(hasSave);
        }

        if (newGameSubLabel != null)
        {
            newGameSubLabel.gameObject.SetActive(hasSave);
            newGameSubLabel.text = hasSave ? "(borrara la partida actual)" : string.Empty;
        }

        if (saveInfoText != null)
        {
            saveInfoText.text = hasSave
                ? "<color=#FFFFFF>[ PARTIDA GUARDADA ]</color>\n"
                    + "Kaisen - Nivel <color=#80D8FF>" + loadedSave.systemLevel + "</color>\n"
                    + "<color=#666666>Rango Oficial: E</color>\n"
                    + "Runs completadas: <color=#80D8FF>" + loadedSave.totalRunsCompleted + "</color>\n"
                    + "Hospital de Lira: <color=#80D8FF>" + Mathf.RoundToInt(loadedSave.liraHealthPercent * 100f) + "%</color>\n"
                    + "Guardado: <color=#80D8FF>" + loadedSave.lastSaveDateTime + "</color>"
                : string.Empty;
        }

        ApplyMenuLayout();

        SetButtonPalette(continueButton, new Color(0.05f, 0.5f, 0.72f, 0.94f), Color.white);
        SetButtonPalette(newGameButton, hasSave ? new Color(0.33f, 0.34f, 0.38f, 0.94f) : new Color(1f, 0.72f, 0.22f, 0.96f), hasSave ? Color.white : new Color(0.08f, 0.045f, 0.018f, 1f));
        SetButtonPalette(optionsButton, new Color(0.05f, 0.07f, 0.105f, 0.94f), Color.white);
        SetButtonPalette(exitButton, new Color(0.11f, 0.055f, 0.06f, 0.94f), Color.white);

        Button defaultButton = hasSave && continueButton != null && continueButton.gameObject.activeInHierarchy ? continueButton : newGameButton;
        if (defaultButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(defaultButton.gameObject);
        }
    }

    private void ConfigureScenePresentation()
    {
        if (menuCamera == null)
        {
            menuCamera = Camera.main;
        }

        if (menuCamera != null)
        {
            menuCamera.orthographic = true;
            menuCamera.orthographicSize = 5f;
            menuCamera.transform.position = new Vector3(0f, 0f, -10f);
            menuCamera.backgroundColor = new Color32(7, 8, 16, 255);
        }

        Sprite menuBackgroundSprite = LoadMenuBackgroundSprite();
        if (panelBackgroundImage != null)
        {
            panelBackgroundImage.sprite = menuBackgroundSprite != null ? menuBackgroundSprite : CreateMenuBackdropSprite();
            panelBackgroundImage.color = Color.white;
            panelBackgroundImage.type = Image.Type.Simple;
            panelBackgroundImage.preserveAspect = false;
            panelBackgroundImage.raycastTarget = false;
        }

        ConfigureBackgroundSprites();
        ConfigureTextStyling();
    }

    private void ConfigureBackgroundSprites()
    {
        crackRenderers.Clear();
        if (backgroundRoot != null)
        {
            SpriteRenderer[] renderers = backgroundRoot.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].name.Contains("Crack"))
                {
                    crackRenderers.Add(renderers[i]);
                }
            }
        }

        Sprite menuBackgroundSprite = LoadMenuBackgroundSprite();
        usingMenuBackgroundSprite = menuBackgroundSprite != null;

        if (skyRenderer != null && usingMenuBackgroundSprite)
        {
            skyRenderer.sprite = menuBackgroundSprite;
            skyRenderer.color = Color.white;
            skyRenderer.transform.localPosition = Vector3.zero;
            skyRenderer.transform.localScale = Vector3.one;
            skyRenderer.sortingLayerName = "Background";
            skyRenderer.sortingOrder = -10;
        }
        else if (skyRenderer != null && skyRenderer.sprite == null)
        {
            skyRenderer.sprite = CreateSkySprite();
            skyRenderer.color = Color.white;
        }

        if (skylineRenderer != null && usingMenuBackgroundSprite)
        {
            skylineRenderer.gameObject.SetActive(false);
        }
        else if (skylineRenderer != null && skylineRenderer.sprite == null)
        {
            skylineRenderer.sprite = CreateSkylineSprite();
            skylineRenderer.color = Color.white;
        }

        if (usingMenuBackgroundSprite)
        {
            for (int i = 0; i < crackRenderers.Count; i++)
            {
                if (crackRenderers[i] != null)
                {
                    crackRenderers[i].gameObject.SetActive(false);
                }
            }

            crackRenderers.Clear();
        }

        for (int i = 0; i < crackRenderers.Count; i++)
        {
            if (crackRenderers[i] != null && crackRenderers[i].sprite == null)
            {
                crackRenderers[i].sprite = CreateCrackSprite(i);
                crackRenderers[i].color = new Color32(10, 10, 42, 255);
            }
        }

        if (crackParticles != null)
        {
            ParticleSystem.MainModule main = crackParticles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.startColor = new Color(0f, 0.75f, 1f, 0.6f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = crackParticles.emission;
            emission.rateOverTime = 8f;

            ParticleSystem.ShapeModule shape = crackParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(12f, 4f, 0.1f);

            ParticleSystem.VelocityOverLifetimeModule velocity = crackParticles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystemRenderer renderer = crackParticles.GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerName = "Background";
            renderer.sortingOrder = 4;
            renderer.material = GetMenuParticleMaterial();

            if (!crackParticles.isPlaying)
            {
                crackParticles.Play();
            }
        }

        if (vignetteImage != null)
        {
            vignetteImage.sprite = CreateVignetteSprite();
            vignetteImage.color = new Color(0f, 0f, 0f, 0.7f);
            vignetteImage.raycastTarget = false;
        }
    }


    private void ConfigureTextStyling()
    {
        TMP_FontAsset fontAsset = TMP_Settings.defaultFontAsset;
        Image logoImage = EnsureTitleLogoImage();
        Sprite titleLogoSprite = LoadTitleLogoSprite();
        titleUsesLogoSprite = logoImage != null && titleLogoSprite != null;
        if (titleUsesLogoSprite)
        {
            logoImage.sprite = titleLogoSprite;
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;
            logoImage.enabled = true;
            titleLogoTargetPosition = logoImage.rectTransform.anchoredPosition;
            logoImage.rectTransform.anchoredPosition = titleLogoTargetPosition + Vector2.up * 30f;
            SetGraphicAlpha(logoImage, 0f);
        }
        else if (logoImage != null)
        {
            logoImage.enabled = false;
        }

        if (titleText != null)
        {
            titleText.gameObject.SetActive(!titleUsesLogoSprite);
            titleText.font = fontAsset;
            titleText.text = "LAST ASCENTION";
            titleText.fontSize = 76f;
            titleText.color = new Color(1f, 0.94f, 0.76f, 1f);
            titleTargetPosition = titleText.rectTransform.anchoredPosition;
            titleText.rectTransform.anchoredPosition = titleTargetPosition + Vector2.up * 30f;
            titleText.alignment = TextAlignmentOptions.Left;
            SetTextAlpha(titleText, titleUsesLogoSprite ? 1f : 0f);
            ApplyOutline(titleText, new Color32(255, 126, 30, 255), 0.23f);
        }

        if (subtitleText != null)
        {
            subtitleText.font = fontAsset;
            subtitleText.text = "SISTEMA DESPERTADO // ELIGE TU ASCENSO";
            subtitleText.fontSize = 17f;
            subtitleText.color = new Color32(128, 216, 255, 255);
            subtitleText.alignment = TextAlignmentOptions.Left;
            subtitleText.maxVisibleCharacters = 0;
        }
    }

    private void ApplyMenuLayout()
    {
        if (panelBackgroundImage != null)
        {
            SetRectTransform(panelBackgroundImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MenuCanvasWidth, MenuCanvasHeight));
        }

        if (vignetteImage != null)
        {
            SetRectTransform(vignetteImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MenuCanvasWidth, MenuCanvasHeight));
        }

        if (titlePanel != null)
        {
            SetRectTransform(titlePanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -22f), new Vector2(760f, 228f));
        }

        if (titleLogoImage != null)
        {
            SetRectTransform(titleLogoImage.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -2f), new Vector2(680f, 199f));
            titleLogoImage.preserveAspect = true;
            titleLogoTargetPosition = titleLogoImage.rectTransform.anchoredPosition;
        }

        if (titleText != null)
        {
            SetRectTransform(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -24f), new Vector2(720f, 96f));
            titleText.alignment = TextAlignmentOptions.Left;
            titleText.fontSize = 76f;
            titleTargetPosition = titleText.rectTransform.anchoredPosition;
        }

        if (subtitleText != null)
        {
            SetRectTransform(subtitleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -188f), new Vector2(560f, 26f));
            subtitleText.alignment = TextAlignmentOptions.Left;
            subtitleText.fontSize = 15f;
        }

        if (scanlineRect != null)
        {
            SetRectTransform(scanlineRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -214f), new Vector2(540f, 3f));
        }

        if (buttonsPanel != null)
        {
            if (usingMenuBackgroundSprite)
            {
                SetButtonsPanelLayoutDrivers(false);
                SetRectTransform(buttonsPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(MenuCanvasWidth, MenuCanvasHeight));
                ClearPanelChrome(buttonsPanel);
            }
            else
            {
                SetButtonsPanelLayoutDrivers(true);
                SetRectTransform(buttonsPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(74f, -300f), new Vector2(374f, hasSave ? 288f : 218f));
            }
        }

        float buttonWidth = usingMenuBackgroundSprite ? EmbeddedButtonSlotWidth : 340f;
        float buttonHeight = usingMenuBackgroundSprite ? EmbeddedButtonSlotHeight : 54f;
        float spacing = usingMenuBackgroundSprite ? EmbeddedButtonSlotSpacing : 66f;
        if (hasSave)
        {
            float startY = usingMenuBackgroundSprite ? 500f : 0f;
            float saveSpacing = usingMenuBackgroundSprite ? 46f : spacing;
            ConfigureButtonLayout(continueButton, GetButtonSlotPosition(0, startY, saveSpacing), buttonWidth, usingMenuBackgroundSprite ? 36f : buttonHeight, false, usingMenuBackgroundSprite);
            ConfigureButtonLayout(newGameButton, GetButtonSlotPosition(1, startY, saveSpacing), buttonWidth, usingMenuBackgroundSprite ? 36f : buttonHeight, true, usingMenuBackgroundSprite);
            ConfigureButtonLayout(optionsButton, GetButtonSlotPosition(2, startY, saveSpacing), buttonWidth, usingMenuBackgroundSprite ? 36f : buttonHeight, false, usingMenuBackgroundSprite);
            ConfigureButtonLayout(exitButton, GetButtonSlotPosition(3, startY, saveSpacing), buttonWidth, usingMenuBackgroundSprite ? 36f : buttonHeight, false, usingMenuBackgroundSprite);
        }
        else
        {
            ConfigureButtonLayout(newGameButton, GetButtonSlotPosition(0, EmbeddedButtonSlotY, spacing), buttonWidth, buttonHeight, false, usingMenuBackgroundSprite);
            ConfigureButtonLayout(optionsButton, GetButtonSlotPosition(1, EmbeddedButtonSlotY, spacing), buttonWidth, buttonHeight, false, usingMenuBackgroundSprite);
            ConfigureButtonLayout(exitButton, GetButtonSlotPosition(2, EmbeddedButtonSlotY, spacing), buttonWidth, buttonHeight, false, usingMenuBackgroundSprite);
        }

        if (saveInfoPanel != null)
        {
            RectTransform saveInfoRect = saveInfoPanel.GetComponent<RectTransform>();
            if (saveInfoRect != null)
            {
                SetRectTransform(saveInfoRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-42f, 42f), new Vector2(370f, 152f));
            }
        }

        if (saveInfoText != null)
        {
            SetRectTransform(saveInfoText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(330f, 126f));
            saveInfoText.alignment = TextAlignmentOptions.TopLeft;
            saveInfoText.fontSize = 14f;
        }

        ApplyMenuFrames();
    }

    private Vector2 GetButtonSlotPosition(int index, float startY, float spacing)
    {
        if (!usingMenuBackgroundSprite)
        {
            return new Vector2(0f, -(spacing * index));
        }

        return new Vector2(EmbeddedButtonSlotX, -(startY + spacing * index));
    }

    private void ConfigureButtonLayout(Button button, Vector2 anchoredPosition, float width, float height, bool showSubLabel)
    {
        ConfigureButtonLayout(button, anchoredPosition, width, height, showSubLabel, false);
    }

    private void ConfigureButtonLayout(Button button, Vector2 anchoredPosition, float width, float height, bool showSubLabel, bool useTopLeftAnchor)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            SetRectTransform(rect, useTopLeftAnchor ? new Vector2(0f, 1f) : new Vector2(0.5f, 1f), useTopLeftAnchor ? new Vector2(0f, 1f) : new Vector2(0.5f, 1f), anchoredPosition, new Vector2(width, height));
        }

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = useTopLeftAnchor;
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = height;
        }

        float labelFontSize = useTopLeftAnchor ? (showSubLabel ? 12.5f : 14.5f) : 21f;
        float labelX = useTopLeftAnchor ? 38f : 58f;
        float cursorX = useTopLeftAnchor ? 13f : 18f;
        Transform labelTransform = button.transform.Find("Label");
        if (labelTransform != null)
        {
            TextMeshProUGUI label = labelTransform.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                SetRectTransform(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), showSubLabel ? new Vector2(labelX, useTopLeftAnchor ? 5f : 8f) : new Vector2(labelX, 0f), new Vector2(useTopLeftAnchor ? 170f : 260f, showSubLabel ? 20f : 28f));
                label.alignment = TextAlignmentOptions.Left;
                label.fontSize = labelFontSize;
            }
        }

        Transform cursorTransform = button.transform.Find("Cursor");
        if (cursorTransform != null)
        {
            TextMeshProUGUI cursorText = cursorTransform.GetComponent<TextMeshProUGUI>();
            if (cursorText != null)
            {
                cursorText.text = ">";
                SetRectTransform(cursorText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), showSubLabel ? new Vector2(cursorX, useTopLeftAnchor ? 5f : 8f) : new Vector2(cursorX, 0f), new Vector2(24f, 24f));
                cursorText.alignment = TextAlignmentOptions.Center;
                cursorText.fontSize = useTopLeftAnchor ? 14f : 20f;
            }
        }

        if (button == newGameButton && newGameSubLabel != null)
        {
            SetRectTransform(newGameSubLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), useTopLeftAnchor ? new Vector2(38f, -8f) : new Vector2(58f, -13f), new Vector2(useTopLeftAnchor ? 170f : 250f, 14f));
            newGameSubLabel.alignment = TextAlignmentOptions.Left;
            newGameSubLabel.fontSize = useTopLeftAnchor ? 7.5f : 10f;
        }
    }

    private void SetRectTransform(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private void SetButtonsPanelLayoutDrivers(bool enabled)
    {
        if (buttonsPanel == null)
        {
            return;
        }

        LayoutGroup[] layoutGroups = buttonsPanel.GetComponents<LayoutGroup>();
        for (int i = 0; i < layoutGroups.Length; i++)
        {
            if (layoutGroups[i] != null)
            {
                layoutGroups[i].enabled = enabled;
            }
        }

        ContentSizeFitter fitter = buttonsPanel.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.enabled = enabled;
        }
    }

    private void ConfigureButtons()
    {
        buttonFeedbacks.Clear();
        BindButton(continueButton, OnContinuePressed);
        BindButton(newGameButton, OnNewGamePressed);
        BindButton(optionsButton, OnOptionsPressed);
        BindButton(exitButton, OnExitPressed);
        BindButton(confirmYesButton, OnConfirmNewGamePressed);
        BindButton(confirmNoButton, OnCancelNewGamePressed);
    }

    private void ConfigureOptionsPanel()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }

        if (closeOptionsButton != null)
        {
            closeOptionsButton.onClick.RemoveAllListeners();
            closeOptionsButton.onClick.AddListener(delegate { SetOptionsVisible(false); });
        }

        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveAllListeners();
            masterSlider.onValueChanged.AddListener(delegate(float value)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetMasterVolume(value);
                }
            });
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(delegate(float value)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetMusicVolume(value);
                }
            });
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(delegate(float value)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetSfxVolume(value);
                }
            });
        }

        SyncAudioSliders();
    }

    private void ConfigureConfirmationPanel()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
    }

    private void SetOptionsVisible(bool visible)
    {
        if (optionsPanel == null)
        {
            return;
        }

        optionsPanel.SetActive(visible);
        SyncAudioSliders();
        if (visible && masterSlider != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(masterSlider.gameObject);
        }
        else if (!visible && newGameButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(newGameButton.gameObject);
        }
    }

    private void SetConfirmationVisible(bool visible)
    {
        if (confirmationPanel == null)
        {
            return;
        }

        confirmationPanel.SetActive(visible);
        if (confirmationCard != null)
        {
            confirmationCard.localScale = visible ? Vector3.zero : Vector3.one;
        }

        if (visible)
        {
            StartCoroutine(ConfirmationScaleRoutine());
            if (confirmNoButton != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(confirmNoButton.gameObject);
            }
        }
        else if (newGameButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(newGameButton.gameObject);
        }
    }

    private IEnumerator ConfirmationScaleRoutine()
    {
        if (confirmationCard == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.2f);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            confirmationCard.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, eased);
            yield return null;
        }

        confirmationCard.localScale = Vector3.one;
    }

    private IEnumerator PlayTitleIntro()
    {
        Graphic titleGraphic = titleUsesLogoSprite ? titleLogoImage : titleText;
        RectTransform titleRect = titleGraphic != null ? titleGraphic.rectTransform : null;
        Vector2 targetPosition = titleUsesLogoSprite ? titleLogoTargetPosition : titleTargetPosition;
        if (titleGraphic == null || titleRect == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < 1.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 1.2f);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            titleRect.anchoredPosition = Vector2.Lerp(targetPosition + Vector2.up * 30f, targetPosition, eased);
            SetGraphicAlpha(titleGraphic, eased);
            yield return null;
        }

        titleRect.anchoredPosition = targetPosition;
        SetGraphicAlpha(titleGraphic, 1f);
    }

    private IEnumerator SubtitleTypewriterRoutine()
    {
        if (titleUsesLogoSprite)
        {
            while (titleLogoImage != null && titleLogoImage.color.a < 0.99f)
            {
                yield return null;
            }
        }
        else
        {
            while (titleText != null && titleText.color.a < 0.99f)
            {
                yield return null;
            }
        }

        if (subtitleText == null)
        {
            yield break;
        }

        int totalCharacters = subtitleText.text.Length;
        for (int i = 0; i <= totalCharacters; i++)
        {
            subtitleText.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(0.04f);
        }
    }

    private IEnumerator CrackFlashLoop()
    {
        for (;;)
        {
            yield return new WaitForSecondsRealtime(Random.Range(4f, 7f));
            if (crackRenderers.Count == 0)
            {
                continue;
            }

            SpriteRenderer target = crackRenderers[Random.Range(0, crackRenderers.Count)];
            if (target == null)
            {
                continue;
            }

            Color baseColor = new Color32(10, 10, 42, 255);
            Color flashColor = new Color32(68, 136, 255, 255);
            float elapsed = 0f;
            while (elapsed < 0.1f)
            {
                elapsed += Time.unscaledDeltaTime;
                target.color = Color.Lerp(baseColor, flashColor, Mathf.Clamp01(elapsed / 0.1f));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                target.color = Color.Lerp(flashColor, baseColor, Mathf.Clamp01(elapsed / 0.5f));
                yield return null;
            }

            target.color = baseColor;
        }
    }

    private void AnimateParallax()
    {
        if (skyRenderer != null && !usingMenuBackgroundSprite)
        {
            skyOffset += Time.unscaledDeltaTime * 0.02f;
            skyRenderer.transform.localPosition = new Vector3(Mathf.Repeat(skyOffset + 0.75f, 1.5f) - 0.75f, skyRenderer.transform.localPosition.y, skyRenderer.transform.localPosition.z);
        }

        if (skylineRenderer != null && skylineRenderer.gameObject.activeInHierarchy)
        {
            skylineOffset += Time.unscaledDeltaTime * 0.05f;
            skylineRenderer.transform.localPosition = new Vector3(Mathf.Repeat(skylineOffset + 0.75f, 1.5f) - 0.75f, skylineRenderer.transform.localPosition.y, skylineRenderer.transform.localPosition.z);
        }
    }

    private void AnimateScanline()
    {
        if (scanlineRect == null || titlePanel == null)
        {
            return;
        }

        float progress = Mathf.Repeat(Time.unscaledTime / 3f, 1f);
        float top = titlePanel.rect.height * 0.5f;
        float bottom = -titlePanel.rect.height * 0.5f;
        scanlineRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(top, bottom, progress));
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.Automatic;
        button.navigation = navigation;

        MainMenuButtonFeedback feedback = button.GetComponent<MainMenuButtonFeedback>();
        if (feedback != null)
        {
            feedback.Initialize(this);
            feedback.ConfigureSizing(21f, 25f);
            button.onClick.AddListener(feedback.PlayClickFeedback);
            buttonFeedbacks.Add(feedback);
        }
    }

    private void SetButtonPalette(Button button, Color backgroundColor, Color textColor)
    {
        if (button == null)
        {
            return;
        }

        Color resolvedBackground = usingMenuBackgroundSprite ? new Color(0f, 0f, 0f, 0.04f) : backgroundColor;
        Color resolvedText = usingMenuBackgroundSprite ? new Color(0.72f, 0.96f, 1f, 1f) : textColor;
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = resolvedBackground;
        }

        MainMenuButtonFeedback feedback = button.GetComponent<MainMenuButtonFeedback>();
        if (feedback != null)
        {
            feedback.SetVisualColors(resolvedBackground, resolvedText);
        }

        ApplyButtonChrome(button, usingMenuBackgroundSprite ? new Color(0.02f, 0.58f, 0.78f, 0.72f) : backgroundColor);
    }

    private void ApplyMenuFrames()
    {
        if (usingMenuBackgroundSprite)
        {
            ClearPanelChrome(titlePanel);
            ClearPanelChrome(buttonsPanel);
        }
        else
        {
            ApplyPanelChrome(titlePanel, new Color(0.012f, 0.018f, 0.028f, 0.66f), new Color(1f, 0.52f, 0.16f, 0.76f));
            ApplyPanelChrome(buttonsPanel, new Color(0.015f, 0.021f, 0.032f, 0.72f), new Color(0.2f, 0.8f, 1f, 0.56f));
        }

        if (saveInfoPanel != null)
        {
            ApplyPanelChrome(saveInfoPanel.GetComponent<RectTransform>(), new Color(0.016f, 0.024f, 0.036f, 0.82f), new Color(0.55f, 0.84f, 1f, 0.5f));
        }

        if (optionsPanel != null)
        {
            RectTransform optionsRect = optionsPanel.GetComponent<RectTransform>();
            ApplyPanelChrome(optionsRect, new Color(0f, 0f, 0f, 0.62f), new Color(0.38f, 0.78f, 1f, 0.35f));
        }

        if (confirmationCard != null)
        {
            ApplyPanelChrome(confirmationCard, new Color(0.035f, 0.025f, 0.028f, 0.94f), new Color(1f, 0.52f, 0.2f, 0.8f));
        }
    }

    private void ClearPanelChrome(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        Image image = rect.GetComponent<Image>();
        if (image != null)
        {
            image.color = Color.clear;
            image.raycastTarget = false;
        }

        string[] lineNames = { "BorderTop", "BorderBottom", "BorderLeft" };
        for (int i = 0; i < lineNames.Length; i++)
        {
            Transform line = rect.Find(lineNames[i]);
            if (line != null)
            {
                line.gameObject.SetActive(false);
            }
        }
    }

    private void ApplyPanelChrome(RectTransform rect, Color backgroundColor, Color outlineColor)
    {
        if (rect == null)
        {
            return;
        }

        Image image = rect.GetComponent<Image>();
        if (image == null)
        {
            image = rect.gameObject.AddComponent<Image>();
        }

        image.sprite = GetWhiteSprite();
        image.color = backgroundColor;
        image.raycastTarget = false;

        Outline outline = rect.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }

        SetPanelLine(rect, "BorderTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 2f), outlineColor);
        SetPanelLine(rect, "BorderBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 2f), new Color(outlineColor.r, outlineColor.g, outlineColor.b, outlineColor.a * 0.42f));
        SetPanelLine(rect, "BorderLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(2f, 0f), outlineColor);
    }

    private void SetPanelLine(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        RectTransform line = EnsureButtonChildImage(parent, name, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
        line.GetComponent<Image>().color = color;
    }

    private void ApplyButtonChrome(Button button, Color accentColor)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = GetWhiteSprite();
            if (usingMenuBackgroundSprite)
            {
                image.color = new Color(0f, 0f, 0f, 0.04f);
            }
        }

        Outline outline = button.GetComponent<Outline>();
        if (outline == null)
        {
            outline = button.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = usingMenuBackgroundSprite ? new Color(0f, 0.85f, 1f, 0.2f) : new Color(0f, 0f, 0f, 0.86f);
        outline.effectDistance = usingMenuBackgroundSprite ? new Vector2(1f, -1f) : new Vector2(2f, -2f);

        RectTransform accent = EnsureButtonChildImage(button.transform, "Accent", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(5f, 0f));
        Image accentImage = accent.GetComponent<Image>();
        accentImage.color = usingMenuBackgroundSprite
            ? new Color(0f, 0.8f, 1f, 0.55f)
            : new Color(Mathf.Clamp01(accentColor.r + 0.15f), Mathf.Clamp01(accentColor.g + 0.15f), Mathf.Clamp01(accentColor.b + 0.15f), 1f);

        RectTransform topLine = EnsureButtonChildImage(button.transform, "TopLine", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -1f), new Vector2(0f, 2f));
        Image topLineImage = topLine.GetComponent<Image>();
        topLineImage.color = usingMenuBackgroundSprite ? new Color(0.74f, 0.95f, 1f, 0.12f) : new Color(1f, 1f, 1f, 0.18f);
    }

    private RectTransform EnsureButtonChildImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Transform target = parent.Find(name);
        if (target == null)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(Image));
            target = child.transform;
            target.SetParent(parent, false);
        }

        target.SetAsFirstSibling();

        Image image = target.GetComponent<Image>();
        image.sprite = GetWhiteSprite();
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private void SyncAudioSliders()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
        }
    }

    private void PlayHoverSfx()
    {
        if (uiAudioSource != null && hoverClip != null)
        {
            uiAudioSource.PlayOneShot(hoverClip);
        }
    }

    private void EnsureMenuAudioSource()
    {
        if (uiAudioSource == null)
        {
            GameObject audioObject = GameObject.Find("AudioManager_Menu");
            if (audioObject != null)
            {
                uiAudioSource = audioObject.GetComponent<AudioSource>();
                if (uiAudioSource == null)
                {
                    uiAudioSource = audioObject.AddComponent<AudioSource>();
                }
            }
        }

        if (uiAudioSource == null)
        {
            GameObject audioObject = new GameObject("AudioManager_Menu");
            uiAudioSource = audioObject.AddComponent<AudioSource>();
        }

        uiAudioSource.playOnAwake = false;
        uiAudioSource.loop = false;
        uiAudioSource.spatialBlend = 0f;
        uiAudioSource.volume = 0.85f;
    }

    private void CreateMenuClips()
    {
        hoverClip = CreateToneClip("menu_hover", 0.05f, 880f, 0.08f, false);
        clickClip = CreateToneClip("menu_click", 0.1f, 1240f, 0.12f, true);
    }

    private AudioClip CreateToneClip(string clipName, float duration, float frequency, float amplitude, bool descending)
    {
        int sampleRate = 22050;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] data = new float[sampleCount];
        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)Mathf.Max(1, sampleCount - 1);
            float currentFrequency = descending ? Mathf.Lerp(frequency, frequency * 0.65f, t) : frequency;
            phase += (Mathf.PI * 2f * currentFrequency) / sampleRate;
            float envelope = Mathf.Sin(t * Mathf.PI);
            data[i] = Mathf.Sin(phase) * amplitude * envelope;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void AutoWireSceneReferences()
    {
        if (menuCamera == null)
        {
            menuCamera = Camera.main;
        }

        if (backgroundRoot == null)
        {
            GameObject backgroundObject = GameObject.Find("Background");
            if (backgroundObject != null)
            {
                backgroundRoot = backgroundObject.transform;
            }
        }

        if (skyRenderer == null)
        {
            skyRenderer = FindSceneComponent<SpriteRenderer>("Background/SkyCracksLayer");
        }

        if (skylineRenderer == null)
        {
            skylineRenderer = FindSceneComponent<SpriteRenderer>("Background/CitySkylineLayer");
        }

        if (crackParticles == null)
        {
            crackParticles = FindSceneComponent<ParticleSystem>("Background/CrackParticles");
        }

        if (panelBackgroundImage == null)
        {
            panelBackgroundImage = FindSceneComponent<Image>("Canvas_MainMenu/Panel_Background");
        }

        if (titlePanel == null)
        {
            titlePanel = FindSceneComponent<RectTransform>("Canvas_MainMenu/Panel_Title");
        }

        if (titleText == null)
        {
            titleText = FindSceneComponent<TextMeshProUGUI>("Canvas_MainMenu/Panel_Title/TitleText");
        }

        if (titleLogoImage == null)
        {
            titleLogoImage = FindSceneComponent<Image>("Canvas_MainMenu/Panel_Title/TitleLogo");
        }

        if (subtitleText == null)
        {
            subtitleText = FindSceneComponent<TextMeshProUGUI>("Canvas_MainMenu/Panel_Title/SubtitleText");
        }

        if (scanlineRect == null)
        {
            scanlineRect = FindSceneComponent<RectTransform>("Canvas_MainMenu/Panel_Title/Scanline");
        }

        if (buttonsPanel == null)
        {
            buttonsPanel = FindSceneComponent<RectTransform>("Canvas_MainMenu/Panel_Buttons");
        }

        if (continueButton == null)
        {
            continueButton = FindSceneComponent<Button>("Canvas_MainMenu/Panel_Buttons/Button_Continue");
        }

        if (newGameButton == null)
        {
            newGameButton = FindSceneComponent<Button>("Canvas_MainMenu/Panel_Buttons/Button_NewGame");
        }

        if (optionsButton == null)
        {
            optionsButton = FindSceneComponent<Button>("Canvas_MainMenu/Panel_Buttons/Button_Options");
        }

        if (exitButton == null)
        {
            exitButton = FindSceneComponent<Button>("Canvas_MainMenu/Panel_Buttons/Button_Exit");
        }

        if (newGameSubLabel == null)
        {
            newGameSubLabel = FindSceneComponent<TextMeshProUGUI>("Canvas_MainMenu/Panel_Buttons/Button_NewGame/SubLabel");
        }

        if (saveInfoPanel == null)
        {
            GameObject saveInfoObject = GameObject.Find("Canvas_MainMenu/Panel_SaveInfo");
            if (saveInfoObject != null)
            {
                saveInfoPanel = saveInfoObject;
            }
        }

        if (saveInfoText == null)
        {
            saveInfoText = FindSceneComponent<TextMeshProUGUI>("Canvas_MainMenu/Panel_SaveInfo/SaveInfoText");
        }

        if (vignetteImage == null)
        {
            vignetteImage = FindSceneComponent<Image>("Canvas_MainMenu/Panel_Background/Vignette");
        }

        if (optionsPanel == null)
        {
            GameObject optionsObject = GameObject.Find("Canvas_MainMenu/Panel_Options");
            if (optionsObject != null)
            {
                optionsPanel = optionsObject;
            }
        }

        if (masterSlider == null)
        {
            masterSlider = FindSceneComponent<Slider>("Canvas_MainMenu/Panel_Options/OptionsCard/Slider_Master");
        }

        if (musicSlider == null)
        {
            musicSlider = FindSceneComponent<Slider>("Canvas_MainMenu/Panel_Options/OptionsCard/Slider_Music");
        }

        if (sfxSlider == null)
        {
            sfxSlider = FindSceneComponent<Slider>("Canvas_MainMenu/Panel_Options/OptionsCard/Slider_Sfx");
        }

        if (closeOptionsButton == null)
        {
            closeOptionsButton = FindSceneComponent<Button>("Canvas_MainMenu/Panel_Options/OptionsCard/Button_CloseOptions");
        }

        if (confirmationPanel == null)
        {
            GameObject confirmationObject = GameObject.Find("Canvas_MainMenu/Panel_Confirmation");
            if (confirmationObject != null)
            {
                confirmationPanel = confirmationObject;
            }
        }

        if (confirmationCard == null)
        {
            confirmationCard = FindSceneComponent<RectTransform>("Canvas_MainMenu/Panel_Confirmation/ConfirmationCard");
        }

        if (confirmYesButton == null)
        {
            confirmYesButton = FindSceneComponent<Button>("Canvas_MainMenu/Panel_Confirmation/ConfirmationCard/Button_Yes");
        }

        if (confirmNoButton == null)
        {
            confirmNoButton = FindSceneComponent<Button>("Canvas_MainMenu/Panel_Confirmation/ConfirmationCard/Button_No");
        }
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
    }

    private T FindSceneComponent<T>(string path) where T : Component
    {
        GameObject target = GameObject.Find(path);
        return target != null ? target.GetComponent<T>() : null;
    }

    private Image EnsureTitleLogoImage()
    {
        if (titleLogoImage != null || titlePanel == null)
        {
            return titleLogoImage;
        }

        Transform existing = titlePanel.Find("TitleLogo");
        if (existing != null)
        {
            titleLogoImage = existing.GetComponent<Image>();
        }

        if (titleLogoImage == null)
        {
            GameObject titleLogoObject = new GameObject("TitleLogo", typeof(RectTransform), typeof(Image));
            titleLogoObject.transform.SetParent(titlePanel, false);
            titleLogoImage = titleLogoObject.GetComponent<Image>();
        }

        titleLogoImage.transform.SetAsFirstSibling();
        titleLogoImage.raycastTarget = false;
        return titleLogoImage;
    }

    private Sprite LoadMenuBackgroundSprite()
    {
        return LoadResourceSprite(MenuBackgroundResourcePath, MenuBackgroundPixelsPerUnit, ref cachedMenuBackgroundSprite);
    }

    private Sprite LoadTitleLogoSprite()
    {
        return LoadResourceSprite(TitleLogoResourcePath, 100f, ref cachedTitleLogoSprite);
    }

    private Sprite LoadResourceSprite(string resourcePath, float pixelsPerUnit, ref Sprite cache)
    {
        if (cache != null)
        {
            return cache;
        }

        Sprite importedSprite = Resources.Load<Sprite>(resourcePath);
        if (importedSprite != null)
        {
            cache = importedSprite;
            return cache;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            return null;
        }

        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        cache = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), Mathf.Max(1f, pixelsPerUnit));
        cache.name = texture.name;
        return cache;
    }

    private Sprite GetWhiteSprite()
    {
        if (cachedWhiteSprite == null)
        {
            cachedWhiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        }

        return cachedWhiteSprite;
    }

    private Sprite CreateMenuBackdropSprite()
    {
        if (cachedMenuBackdropSprite != null)
        {
            return cachedMenuBackdropSprite;
        }

        Texture2D texture = new Texture2D(512, 288, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Vector2 glowCenter = new Vector2(texture.width * 0.66f, texture.height * 0.56f);
        float maxGlowDistance = texture.width * 0.78f;

        for (int y = 0; y < texture.height; y++)
        {
            float vertical = y / (float)(texture.height - 1);
            Color baseColor = Color.Lerp(new Color32(6, 7, 15, 255), new Color32(18, 23, 34, 255), vertical);
            baseColor = Color.Lerp(baseColor, new Color32(7, 8, 13, 255), Mathf.Clamp01((vertical - 0.58f) * 2.4f));

            for (int x = 0; x < texture.width; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), glowCenter) / maxGlowDistance;
                float glow = Mathf.Clamp01(1f - distance);
                Color cyanGlow = new Color(0.02f, 0.48f, 0.62f, 1f) * (glow * 0.34f);
                Color emberGlow = new Color(0.75f, 0.26f, 0.05f, 1f) * (Mathf.Pow(glow, 3f) * 0.38f);
                texture.SetPixel(x, y, baseColor + cyanGlow + emberGlow);
            }
        }

        for (int i = 0; i < 34; i++)
        {
            int x = Mathf.RoundToInt(Mathf.Lerp(0f, texture.width - 1f, i / 33f));
            int roof = 26 + Mathf.RoundToInt(Mathf.PerlinNoise(i * 0.31f, 0.41f) * 42f);
            int width = 9 + Mathf.RoundToInt(Mathf.PerlinNoise(i * 0.2f, 0.7f) * 15f);
            Color towerColor = i % 4 == 0 ? new Color32(11, 18, 26, 235) : new Color32(6, 10, 17, 245);

            for (int tx = Mathf.Max(0, x - width); tx < Mathf.Min(texture.width, x + width); tx++)
            {
                for (int ty = 0; ty < roof; ty++)
                {
                    texture.SetPixel(tx, ty, towerColor);
                }
            }

            if (i % 3 == 0)
            {
                for (int ty = 8; ty < roof - 4; ty += 12)
                {
                    int windowX = Mathf.Clamp(x, 0, texture.width - 1);
                    texture.SetPixel(windowX, ty, new Color32(49, 175, 202, 180));
                    if (windowX + 1 < texture.width)
                    {
                        texture.SetPixel(windowX + 1, ty, new Color32(49, 175, 202, 130));
                    }
                }
            }
        }

        for (int i = 0; i < 7; i++)
        {
            int startX = 42 + (i * 67);
            int startY = 205 - (i % 3) * 16;
            Color crackColor = i % 2 == 0 ? new Color32(255, 136, 38, 190) : new Color32(75, 226, 255, 165);
            for (int step = 0; step < 94; step++)
            {
                int x = startX + step;
                int y = startY - Mathf.RoundToInt(step * (0.42f + (i % 2) * 0.12f)) + Mathf.RoundToInt(Mathf.Sin(step * 0.23f + i) * 4f);
                if (x < 1 || x >= texture.width - 1 || y < 1 || y >= texture.height - 1)
                {
                    continue;
                }

                texture.SetPixel(x, y, crackColor);
                texture.SetPixel(x, y - 1, new Color(crackColor.r, crackColor.g, crackColor.b, 0.42f));
                if (step % 13 == 0)
                {
                    texture.SetPixel(x + 1, y + 1, crackColor);
                }
            }
        }

        texture.Apply();
        cachedMenuBackdropSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        return cachedMenuBackdropSprite;
    }

    private static Material GetMenuParticleMaterial()
    {
        if (sharedMenuParticleMaterial != null)
        {
            return sharedMenuParticleMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            return null;
        }

        sharedMenuParticleMaterial = new Material(shader)
        {
            name = "RuntimeMenuParticleUnlit",
            hideFlags = HideFlags.DontSave
        };
        return sharedMenuParticleMaterial;
    }

    private Sprite CreateSkySprite()
    {
        Texture2D texture = new Texture2D(256, 144, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color32 baseColor = new Color32(5, 5, 16, 255);
        Color32 crackTint = new Color32(10, 10, 42, 255);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, baseColor);
            }
        }

        for (int x = 24; x < 232; x++)
        {
            int y = 110 + Mathf.RoundToInt(Mathf.Sin(x * 0.12f) * 7f + Mathf.Sin(x * 0.05f) * 4f);
            for (int thickness = -1; thickness <= 1; thickness++)
            {
                int py = Mathf.Clamp(y + thickness, 0, texture.height - 1);
                texture.SetPixel(x, py, crackTint);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 24f);
    }

    private Sprite CreateSkylineSprite()
    {
        Texture2D texture = new Texture2D(320, 96, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        for (int x = 0; x < texture.width; x++)
        {
            float roof = 16f + Mathf.PerlinNoise(x * 0.035f, 0.21f) * 42f;
            for (int y = 0; y < texture.height; y++)
            {
                texture.SetPixel(x, y, y <= roof ? Color.black : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0f), 24f);
    }

    private Sprite CreateCrackSprite(int index)
    {
        Texture2D texture = new Texture2D(96, 22, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        float offset = index * 0.9f;
        for (int x = 2; x < texture.width - 2; x++)
        {
            int y = Mathf.RoundToInt(11f + Mathf.Sin((x * 0.18f) + offset) * 4f + Mathf.Sin((x * 0.07f) + offset) * 2f);
            for (int thickness = -1; thickness <= 1; thickness++)
            {
                int py = Mathf.Clamp(y + thickness, 0, texture.height - 1);
                texture.SetPixel(x, py, Color.white);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 24f);
    }

    private Sprite CreateVignetteSprite()
    {
        Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(texture.width * 0.5f, texture.height * 0.5f);
        float maxDistance = center.magnitude;
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                float alpha = Mathf.Clamp01(Mathf.Pow(distance, 1.6f));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null)
        {
            return;
        }

        Color color = text.color;
        color.a = Mathf.Clamp01(alpha);
        text.color = color;
    }

    private void SetGraphicAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
        {
            return;
        }

        Color color = graphic.color;
        color.a = Mathf.Clamp01(alpha);
        graphic.color = color;
    }

    private void ApplyOutline(TMP_Text text, Color outlineColor, float width)
    {
        if (text == null)
        {
            return;
        }

        text.fontMaterial = new Material(text.fontSharedMaterial);
        text.fontMaterial.SetColor(TMPro.ShaderUtilities.ID_OutlineColor, outlineColor);
        text.fontMaterial.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, width);
    }
}
