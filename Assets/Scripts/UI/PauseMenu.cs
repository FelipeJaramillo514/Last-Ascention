using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private RectTransform selectionRect;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button abandonRunButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private RectTransform optionsRoot;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private readonly List<Button> buttons = new List<Button>();

    private bool isOpen;
    private bool optionsVisible;
    private int selectedIndex;

    public bool IsOpen => isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<PauseMenu>() != null)
        {
            return;
        }

        new GameObject("PauseMenu").AddComponent<PauseMenu>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureUi();
        HideImmediate();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            return;
        }

        bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (escapePressed)
        {
            if (isOpen && optionsVisible)
            {
                ToggleOptions(false);
                return;
            }

            TogglePause();
        }

        if (!isOpen)
        {
            return;
        }

        HandleNavigation();
    }

    public void TogglePause()
    {
        if (isOpen)
        {
            Resume();
        }
        else
        {
            Open();
        }
    }

    public void Open()
    {
        EnsureUi();
        isOpen = true;
        optionsVisible = false;
        panelRoot.gameObject.SetActive(true);
        panelCanvasGroup.alpha = 1f;
        ToggleOptions(false);
        selectedIndex = 0;
        UpdateSelectionVisual();
        Time.timeScale = 0f;
        RefreshSliders();
    }

    public void Resume()
    {
        HideImmediate();
        Time.timeScale = 1f;
    }

    public void OpenOptionsOnly()
    {
        Open();
        ToggleOptions(true);
    }

    private void HandleNavigation()
    {
        Vector2 navigate = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame) navigate.y = 1f;
            if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame) navigate.y = -1f;
            if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame) navigate.x = -1f;
            if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame) navigate.x = 1f;
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.up.wasPressedThisFrame) navigate.y = 1f;
            if (Gamepad.current.dpad.down.wasPressedThisFrame) navigate.y = -1f;
            if (Gamepad.current.dpad.left.wasPressedThisFrame) navigate.x = -1f;
            if (Gamepad.current.dpad.right.wasPressedThisFrame) navigate.x = 1f;
        }

        if (optionsVisible)
        {
            Slider activeSlider = selectedIndex == 0 ? masterSlider : selectedIndex == 1 ? musicSlider : sfxSlider;
            if (navigate.y > 0.1f)
            {
                selectedIndex = Mathf.Max(0, selectedIndex - 1);
            }
            else if (navigate.y < -0.1f)
            {
                selectedIndex = Mathf.Min(2, selectedIndex + 1);
            }

            if (activeSlider != null && Mathf.Abs(navigate.x) > 0.1f)
            {
                activeSlider.value = Mathf.Clamp01(activeSlider.value + navigate.x * 0.05f);
            }

            UpdateSelectionVisual();
        }
        else
        {
            if (navigate.y > 0.1f)
            {
                selectedIndex = Mathf.Max(0, selectedIndex - 1);
                UpdateSelectionVisual();
            }
            else if (navigate.y < -0.1f)
            {
                selectedIndex = Mathf.Min(buttons.Count - 1, selectedIndex + 1);
                UpdateSelectionVisual();
            }
        }

        bool submitPressed = (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        if (!submitPressed)
        {
            return;
        }

        if (optionsVisible)
        {
            ToggleOptions(false);
            return;
        }

        if (selectedIndex >= 0 && selectedIndex < buttons.Count && buttons[selectedIndex] != null)
        {
            buttons[selectedIndex].onClick.Invoke();
        }
    }

    private void ToggleOptions(bool visible)
    {
        optionsVisible = visible;
        if (optionsRoot != null)
        {
            optionsRoot.gameObject.SetActive(visible);
        }

        if (visible)
        {
            selectedIndex = 0;
            RefreshSliders();
        }
        else
        {
            selectedIndex = 0;
        }

        UpdateSelectionVisual();
    }

    private void RefreshSliders()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        if (masterSlider != null) masterSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
    }

    private void HideImmediate()
    {
        isOpen = false;
        optionsVisible = false;
        if (panelRoot != null)
        {
            panelRoot.gameObject.SetActive(false);
        }

        if (optionsRoot != null)
        {
            optionsRoot.gameObject.SetActive(false);
        }
    }

    private void EnsureUi()
    {
        if (overlayCanvas != null && panelRoot != null && continueButton != null && optionsButton != null && abandonRunButton != null && quitButton != null)
        {
            return;
        }

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject canvasObject = overlayCanvas != null ? overlayCanvas.gameObject : new GameObject("PauseCanvas", typeof(RectTransform));
        overlayCanvas = canvasObject.GetComponent<Canvas>();
        if (overlayCanvas == null)
        {
            overlayCanvas = canvasObject.AddComponent<Canvas>();
        }

        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 320;
        if (canvasObject.GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
        }

        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        panelRoot = EnsureRect(canvasObject.transform, "PausePanel", Vector2.zero, new Vector2(560f, 420f), new Vector2(0.5f, 0.5f));
        Image panelImage = panelRoot.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = panelRoot.gameObject.AddComponent<Image>();
        }

        panelImage.sprite = HUDSpriteFactory.WhiteSprite;
        panelImage.color = new Color(0f, 0f, 0f, 0.82f);
        panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();
        }

        Text titleText = EnsureText(panelRoot, "Title", "PAUSA", 30, Color.white, new Vector2(0f, 150f), new Vector2(220f, 36f), new Vector2(0.5f, 0.5f));
        titleText.fontStyle = FontStyle.Bold;

        continueButton = EnsureButton(panelRoot, "Continue", "Continuar", new Vector2(0f, 72f), uiFont, Resume);
        optionsButton = EnsureButton(panelRoot, "Options", "Opciones", new Vector2(0f, 18f), uiFont, () => ToggleOptions(true));
        abandonRunButton = EnsureButton(panelRoot, "AbandonRun", "Abandonar Run", new Vector2(0f, -36f), uiFont, () =>
        {
            Resume();
            if (RunManager.Instance != null)
            {
                RunManager.Instance.ReturnToHub();
            }
        });
        quitButton = EnsureButton(panelRoot, "Quit", "Salir del Juego", new Vector2(0f, -90f), uiFont, QuitGame);

        buttons.Clear();
        buttons.Add(continueButton);
        buttons.Add(optionsButton);
        buttons.Add(abandonRunButton);
        buttons.Add(quitButton);

        selectionRect = EnsureRect(panelRoot, "Selection", continueButton.GetComponent<RectTransform>().anchoredPosition, new Vector2(240f, 36f), new Vector2(0.5f, 0.5f));
        Image selectionImage = selectionRect.GetComponent<Image>();
        if (selectionImage == null)
        {
            selectionImage = selectionRect.gameObject.AddComponent<Image>();
        }

        selectionImage.sprite = HUDSpriteFactory.WhiteSprite;
        selectionImage.color = new Color(0f, 0.75f, 1f, 0.2f);
        selectionRect.SetAsFirstSibling();

        optionsRoot = EnsureRect(panelRoot, "OptionsPanel", new Vector2(0f, -168f), new Vector2(420f, 160f), new Vector2(0.5f, 0f));
        Image optionsImage = optionsRoot.GetComponent<Image>();
        if (optionsImage == null)
        {
            optionsImage = optionsRoot.gameObject.AddComponent<Image>();
        }

        optionsImage.sprite = HUDSpriteFactory.WhiteSprite;
        optionsImage.color = new Color(0.05f, 0.08f, 0.14f, 0.95f);

        masterSlider = EnsureSlider(optionsRoot, "Master", "MASTER", new Vector2(0f, 48f), uiFont, value => { if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(value); });
        musicSlider = EnsureSlider(optionsRoot, "Music", "MUSICA", new Vector2(0f, 0f), uiFont, value => { if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(value); });
        sfxSlider = EnsureSlider(optionsRoot, "Sfx", "SFX", new Vector2(0f, -48f), uiFont, value => { if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(value); });
        optionsRoot.gameObject.SetActive(false);
        panelRoot.gameObject.SetActive(false);

        DontDestroyOnLoad(canvasObject);
    }

    private Button EnsureButton(Transform parent, string name, string text, Vector2 position, Font uiFont, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = EnsureRect(parent, name, position, new Vector2(220f, 34f), new Vector2(0.5f, 0.5f));
        Image image = rect.GetComponent<Image>();
        if (image == null)
        {
            image = rect.gameObject.AddComponent<Image>();
        }

        image.sprite = HUDSpriteFactory.WhiteSprite;
        image.color = new Color(0.08f, 0.12f, 0.2f, 0.92f);

        Button button = rect.GetComponent<Button>();
        if (button == null)
        {
            button = rect.gameObject.AddComponent<Button>();
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);
        EnsureText(rect, "Label", text, 18, Color.white, Vector2.zero, rect.sizeDelta, new Vector2(0.5f, 0.5f));
        return button;
    }

    private Slider EnsureSlider(Transform parent, string name, string labelText, Vector2 position, Font uiFont, UnityEngine.Events.UnityAction<float> onChanged)
    {
        RectTransform root = EnsureRect(parent, name, position, new Vector2(340f, 32f), new Vector2(0.5f, 0.5f));
        EnsureText(root, "Label", labelText, 16, Color.white, new Vector2(-134f, 0f), new Vector2(100f, 24f), new Vector2(0f, 0.5f));
        RectTransform sliderRect = EnsureRect(root, "Slider", new Vector2(48f, 0f), new Vector2(190f, 20f), new Vector2(0.5f, 0.5f));
        Slider slider = sliderRect.GetComponent<Slider>();
        if (slider == null)
        {
            slider = sliderRect.gameObject.AddComponent<Slider>();
        }

        Image background = sliderRect.GetComponent<Image>();
        if (background == null)
        {
            background = sliderRect.gameObject.AddComponent<Image>();
        }

        background.sprite = HUDSpriteFactory.WhiteSprite;
        background.color = new Color(0.18f, 0.18f, 0.24f, 1f);

        Transform fillArea = sliderRect.Find("Fill Area");
        if (fillArea == null)
        {
            GameObject fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
            fillArea = fillAreaObject.transform;
            fillArea.SetParent(sliderRect, false);
        }

        RectTransform fillAreaRect = fillArea as RectTransform;
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = new Vector2(4f, 4f);
        fillAreaRect.offsetMax = new Vector2(-4f, -4f);

        Transform fill = fillArea.Find("Fill");
        if (fill == null)
        {
            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill = fillObject.transform;
            fill.SetParent(fillArea, false);
        }

        Image fillImage = fill.GetComponent<Image>();
        fillImage.sprite = HUDSpriteFactory.WhiteSprite;
        fillImage.color = new Color(0f, 0.75f, 1f, 1f);
        slider.fillRect = fill as RectTransform;

        Transform handle = sliderRect.Find("Handle");
        if (handle == null)
        {
            GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle = handleObject.transform;
            handle.SetParent(sliderRect, false);
        }

        Image handleImage = handle.GetComponent<Image>();
        handleImage.sprite = HUDSpriteFactory.WhiteSprite;
        handleImage.color = Color.white;
        RectTransform handleRect = handle as RectTransform;
        handleRect.sizeDelta = new Vector2(10f, 20f);
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(onChanged);
        return slider;
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

    private Text EnsureText(Transform parent, string name, string text, int fontSize, Color color, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
    {
        Transform target = parent.Find(name);
        if (target == null)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(Text));
            target = child.transform;
            target.SetParent(parent, false);
        }

        Text label = target.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.raycastTarget = false;
        label.text = text;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        Outline outline = label.GetComponent<Outline>();
        if (outline == null)
        {
            outline = label.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);
        return label;
    }

    private void UpdateSelectionVisual()
    {
        if (selectionRect == null)
        {
            return;
        }

        if (optionsVisible)
        {
            RectTransform target = selectedIndex == 0 ? masterSlider.GetComponent<RectTransform>() : selectedIndex == 1 ? musicSlider.GetComponent<RectTransform>() : sfxSlider.GetComponent<RectTransform>();
            selectionRect.sizeDelta = new Vector2(228f, 24f);
            selectionRect.anchoredPosition = target.anchoredPosition + new Vector2(48f, 0f);
            return;
        }

        if (selectedIndex < 0 || selectedIndex >= buttons.Count || buttons[selectedIndex] == null)
        {
            return;
        }

        RectTransform targetButton = buttons[selectedIndex].GetComponent<RectTransform>();
        selectionRect.sizeDelta = targetButton.sizeDelta + new Vector2(20f, 8f);
        selectionRect.anchoredPosition = targetButton.anchoredPosition;
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
