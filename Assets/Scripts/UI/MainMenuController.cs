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

    [SerializeField] private Camera menuCamera;
    [SerializeField] private Transform backgroundRoot;
    [SerializeField] private SpriteRenderer skyRenderer;
    [SerializeField] private SpriteRenderer skylineRenderer;
    [SerializeField] private ParticleSystem crackParticles;
    [SerializeField] private Image vignetteImage;
    [SerializeField] private Image panelBackgroundImage;
    [SerializeField] private RectTransform titlePanel;
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
    private float skyOffset;
    private float skylineOffset;
    private Vector2 titleTargetPosition;
    private AudioClip hoverClip;
    private AudioClip clickClip;
    private MainMenuButtonFeedback lastHighlightedButton;
    private Sprite cachedWhiteSprite;

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
            AudioManager.Instance.PlayMusic(MusicTrackId.Hub, 1f);
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

        SetButtonPalette(continueButton, new Color(0f, 0.75f, 1f, 0.92f), Color.white);
        SetButtonPalette(newGameButton, hasSave ? new Color(0.53f, 0.53f, 0.53f, 0.92f) : Color.white, hasSave ? Color.white : Color.black);
        SetButtonPalette(optionsButton, new Color(0.16f, 0.16f, 0.18f, 0.92f), Color.white);
        SetButtonPalette(exitButton, new Color(0.16f, 0.16f, 0.18f, 0.92f), Color.white);

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
            menuCamera.backgroundColor = new Color32(5, 5, 16, 255);
        }

        if (panelBackgroundImage != null)
        {
            panelBackgroundImage.sprite = GetWhiteSprite();
            panelBackgroundImage.color = new Color(0f, 0f, 0f, 0.15f);
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

        if (skyRenderer != null && skyRenderer.sprite == null)
        {
            skyRenderer.sprite = CreateSkySprite();
            skyRenderer.color = Color.white;
        }

        if (skylineRenderer != null && skylineRenderer.sprite == null)
        {
            skylineRenderer.sprite = CreateSkylineSprite();
            skylineRenderer.color = Color.white;
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
        if (titleText != null)
        {
            titleText.font = fontAsset;
            titleText.text = "KAISEN";
            titleText.fontSize = 88f;
            titleText.color = Color.white;
            titleTargetPosition = titleText.rectTransform.anchoredPosition;
            titleText.rectTransform.anchoredPosition = titleTargetPosition + Vector2.up * 30f;
            titleText.alignment = TextAlignmentOptions.Center;
            SetTextAlpha(titleText, 0f);
            ApplyOutline(titleText, new Color32(0, 191, 255, 255), 0.3f);
        }

        if (subtitleText != null)
        {
            subtitleText.font = fontAsset;
            subtitleText.text = "EL ASCENSO DEL ULTIMO CAZADOR";
            subtitleText.fontSize = 16f;
            subtitleText.color = new Color32(128, 216, 255, 255);
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.maxVisibleCharacters = 0;
        }
    }

    private void ApplyMenuLayout()
    {
        if (panelBackgroundImage != null)
        {
            SetRectTransform(panelBackgroundImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1280f, 720f));
        }

        if (vignetteImage != null)
        {
            SetRectTransform(vignetteImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1280f, 720f));
        }

        if (titlePanel != null)
        {
            SetRectTransform(titlePanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(920f, 210f));
        }

        if (titleText != null)
        {
            SetRectTransform(titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(820f, 96f));
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 88f;
            titleTargetPosition = titleText.rectTransform.anchoredPosition;
        }

        if (subtitleText != null)
        {
            SetRectTransform(subtitleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -44f), new Vector2(860f, 30f));
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.fontSize = 16f;
        }

        if (scanlineRect != null)
        {
            SetRectTransform(scanlineRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 2f));
        }

        if (buttonsPanel != null)
        {
            SetRectTransform(buttonsPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -255f), new Vector2(320f, hasSave ? 250f : 180f));
        }

        float buttonWidth = 300f;
        float buttonHeight = 44f;
        float spacing = 58f;
        if (hasSave)
        {
            ConfigureButtonLayout(continueButton, new Vector2(0f, 0f), buttonWidth, buttonHeight, false);
            ConfigureButtonLayout(newGameButton, new Vector2(0f, -spacing), buttonWidth, buttonHeight, true);
            ConfigureButtonLayout(optionsButton, new Vector2(0f, -(spacing * 2f)), buttonWidth, buttonHeight, false);
            ConfigureButtonLayout(exitButton, new Vector2(0f, -(spacing * 3f)), buttonWidth, buttonHeight, false);
        }
        else
        {
            ConfigureButtonLayout(newGameButton, new Vector2(0f, 0f), buttonWidth, buttonHeight, false);
            ConfigureButtonLayout(optionsButton, new Vector2(0f, -spacing), buttonWidth, buttonHeight, false);
            ConfigureButtonLayout(exitButton, new Vector2(0f, -(spacing * 2f)), buttonWidth, buttonHeight, false);
        }

        if (saveInfoPanel != null)
        {
            RectTransform saveInfoRect = saveInfoPanel.GetComponent<RectTransform>();
            if (saveInfoRect != null)
            {
                SetRectTransform(saveInfoRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(36f, 36f), new Vector2(300f, 140f));
            }
        }

        if (saveInfoText != null)
        {
            SetRectTransform(saveInfoText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(270f, 120f));
            saveInfoText.alignment = TextAlignmentOptions.TopLeft;
            saveInfoText.fontSize = 14f;
        }
    }

    private void ConfigureButtonLayout(Button button, Vector2 anchoredPosition, float width, float height, bool showSubLabel)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            SetRectTransform(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), anchoredPosition, new Vector2(width, height));
        }

        Transform labelTransform = button.transform.Find("Label");
        if (labelTransform != null)
        {
            TextMeshProUGUI label = labelTransform.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                SetRectTransform(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), showSubLabel ? new Vector2(0f, 8f) : Vector2.zero, new Vector2(250f, showSubLabel ? 20f : 28f));
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 20f;
            }
        }

        Transform cursorTransform = button.transform.Find("Cursor");
        if (cursorTransform != null)
        {
            TextMeshProUGUI cursorText = cursorTransform.GetComponent<TextMeshProUGUI>();
            if (cursorText != null)
            {
                cursorText.text = ">";
                SetRectTransform(cursorText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), showSubLabel ? new Vector2(-122f, 8f) : new Vector2(-122f, 0f), new Vector2(24f, 24f));
                cursorText.alignment = TextAlignmentOptions.Center;
                cursorText.fontSize = 20f;
            }
        }

        if (button == newGameButton && newGameSubLabel != null)
        {
            SetRectTransform(newGameSubLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(250f, 14f));
            newGameSubLabel.alignment = TextAlignmentOptions.Center;
            newGameSubLabel.fontSize = 10f;
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
        if (titleText == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < 1.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 1.2f);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            titleText.rectTransform.anchoredPosition = Vector2.Lerp(titleTargetPosition + Vector2.up * 30f, titleTargetPosition, eased);
            SetTextAlpha(titleText, eased);
            yield return null;
        }

        titleText.rectTransform.anchoredPosition = titleTargetPosition;
        SetTextAlpha(titleText, 1f);
    }

    private IEnumerator SubtitleTypewriterRoutine()
    {
        while (titleText != null && titleText.color.a < 0.99f)
        {
            yield return null;
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
        if (skyRenderer != null)
        {
            skyOffset += Time.unscaledDeltaTime * 0.02f;
            skyRenderer.transform.localPosition = new Vector3(Mathf.Repeat(skyOffset + 0.75f, 1.5f) - 0.75f, skyRenderer.transform.localPosition.y, skyRenderer.transform.localPosition.z);
        }

        if (skylineRenderer != null)
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
            feedback.ConfigureSizing(20f, 24f);
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

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = backgroundColor;
        }

        MainMenuButtonFeedback feedback = button.GetComponent<MainMenuButtonFeedback>();
        if (feedback != null)
        {
            feedback.SetVisualColors(backgroundColor, textColor);
        }
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

    private Sprite GetWhiteSprite()
    {
        if (cachedWhiteSprite == null)
        {
            cachedWhiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        }

        return cachedWhiteSprite;
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
