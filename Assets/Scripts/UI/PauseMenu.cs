using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : UIModalPanel
{
    public static PauseMenu Instance { get; private set; }

    [Header("Pause Menu")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private RectTransform selectionRect;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button abandonRunButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private RectTransform optionsRoot;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Text hintText;

    private readonly List<Button> buttons = new List<Button>();
    private bool optionsVisible;
    private int selectedIndex;

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PauseMenu] Solo debe existir una instancia en escena.", this);
        }

        Instance = this;
        WireButtons();
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        WireButtons();
        HideImmediate();
    }

    private new void Update()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            return;
        }

        if (UIModalGate.IsBlockingInteraction && !IsOpen)
        {
            return;
        }

        bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (escapePressed)
        {
            if (IsOpen && optionsVisible)
            {
                ToggleOptions(false);
                return;
            }

            TogglePause();
            return;
        }

        if (!IsOpen)
        {
            return;
        }

        HandleNavigation();
    }

    public void TogglePause()
    {
        if (IsOpen)
        {
            Resume();
        }
        else if (!UIModalGate.IsBlockingInteraction)
        {
            Open();
        }
    }

    public void Resume()
    {
        Close();
    }

    public void OpenOptionsOnly()
    {
        Open();
        ToggleOptions(true);
    }

    protected override void OnOpened()
    {
        optionsVisible = false;
        ToggleOptions(false);
        selectedIndex = 0;
        UpdateSelectionVisual();
        RefreshSliders();
    }

    protected override void OnEscapePressed()
    {
        if (optionsVisible)
        {
            ToggleOptions(false);
            return;
        }

        base.OnEscapePressed();
    }

    protected override void ValidateEditorReferences()
    {
        base.ValidateEditorReferences();
        if (panelRoot == null || continueButton == null)
        {
            Debug.LogError("[PauseMenu] Asigna panel y botones en el prefab (Last Ascention / Setup Modal UI Prefabs).", this);
        }
    }

    private void WireButtons()
    {
        buttons.Clear();
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(Resume);
            buttons.Add(continueButton);
        }

        if (optionsButton != null)
        {
            optionsButton.onClick.RemoveAllListeners();
            optionsButton.onClick.AddListener(() => ToggleOptions(true));
            buttons.Add(optionsButton);
        }

        if (abandonRunButton != null)
        {
            abandonRunButton.onClick.RemoveAllListeners();
            abandonRunButton.onClick.AddListener(() =>
            {
                Resume();
                if (RunManager.Instance != null)
                {
                    RunManager.Instance.ReturnToHub();
                }
            });
            buttons.Add(abandonRunButton);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
            buttons.Add(quitButton);
        }

        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveAllListeners();
            masterSlider.onValueChanged.AddListener(value =>
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
            musicSlider.onValueChanged.AddListener(value =>
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
            sfxSlider.onValueChanged.AddListener(value =>
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetSfxVolume(value);
                }
            });
        }

        if (hintText != null)
        {
            hintText.text = "ESC — Cerrar";
        }
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

        selectedIndex = visible ? 0 : 0;
        if (visible)
        {
            RefreshSliders();
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

    private void UpdateSelectionVisual()
    {
        if (selectionRect == null)
        {
            return;
        }

        if (optionsVisible)
        {
            RectTransform target = selectedIndex == 0 ? masterSlider.GetComponent<RectTransform>() : selectedIndex == 1 ? musicSlider.GetComponent<RectTransform>() : sfxSlider.GetComponent<RectTransform>();
            selectionRect.sizeDelta = new Vector2(300f, 40f);
            selectionRect.anchoredPosition = target.anchoredPosition + new Vector2(48f, 0f);
            return;
        }

        if (selectedIndex < 0 || selectedIndex >= buttons.Count || buttons[selectedIndex] == null)
        {
            return;
        }

        RectTransform targetButton = buttons[selectedIndex].GetComponent<RectTransform>();
        selectionRect.sizeDelta = targetButton.sizeDelta + new Vector2(24f, 12f);
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
