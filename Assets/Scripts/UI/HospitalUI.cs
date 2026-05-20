using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HospitalUI : MonoBehaviour
{
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text stateText;
    [SerializeField] private Image progressFill;
    [SerializeField] private Text progressText;
    [SerializeField] private Text dialogueText;
    [SerializeField] private Button payButton;
    [SerializeField] private Text payButtonText;
    [SerializeField] private RectTransform liraVisual;

    private Font uiFont;
    private bool visible;
    private Coroutine typeRoutine;
    private string[] stableLines =
    {
        "Lira: Gracias... esta vez senti menos frio.",
        "Lira: Sigue luchando, Kaisen. Aun puedo esperar.",
        "Lira: Hoy pude escuchar tu voz con claridad."
    };
    private string[] criticalLines =
    {
        "Lira: El pitido de las maquinas no me deja dormir.",
        "Lira: No te culpes por todo, aun sigo aqui.",
        "Lira: Estoy cansada... pero todavia resisto."
    };
    private string[] worseningLines =
    {
        "Lira: Kaisen... la habitacion se siente mas lejana.",
        "Lira: No quiero que te rompas por mi culpa.",
        "Lira: Si vuelves, prometeme que seguiremos intentandolo."
    };

    public bool IsVisible => visible;

    private void Awake()
    {
        EnsureUi();
    }

    private void Update()
    {
        if (!visible)
        {
            return;
        }

        if (liraVisual != null)
        {
            Vector2 basePosition = new Vector2(-150f, -24f);
            liraVisual.anchoredPosition = basePosition + Vector2.up * Mathf.Sin(Time.unscaledTime * 1.6f) * 4f;
        }
    }

    public void Show()
    {
        EnsureUi();
        visible = true;
        panelRoot.gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
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
        PersistentData persistent = PersistentData.Instance;
        float current = persistent.GoldForHospital;
        float target = persistent.HospitalMonthlyTarget;
        HospitalSupportState state = persistent.GetHospitalState();
        progressFill.fillAmount = target <= 0f ? 0f : Mathf.Clamp01(current / target);
        progressText.text = Mathf.RoundToInt(current) + " / " + Mathf.RoundToInt(target) + " cristales";
        stateText.text = "Estado de Lira: " + GetStateLabel(state) + " | Mes reinicia en " + persistent.RemainingRunsThisMonth + " runs";

        float suggested = persistent.GetSuggestedHospitalPayment();
        payButton.interactable = suggested > 0f;
        payButtonText.text = "Pagar [" + Mathf.RoundToInt(suggested) + "] cristales";
        payButton.onClick.RemoveAllListeners();
        payButton.onClick.AddListener(() =>
        {
            if (PersistentData.Instance.PayHospital(PersistentData.Instance.GetSuggestedHospitalPayment()))
            {
                if (GameStateManager.Instance != null)
                {
                    GameStateManager.Instance.SaveCurrentState();
                }
                Refresh();
            }
        });

        string[] sourceLines = state == HospitalSupportState.Stable ? stableLines : (state == HospitalSupportState.Critical ? criticalLines : worseningLines);
        string chosenLine = sourceLines[Random.Range(0, sourceLines.Length)];
        if (typeRoutine != null)
        {
            StopCoroutine(typeRoutine);
        }
        typeRoutine = StartCoroutine(TypeRoutine(chosenLine));
    }

    private IEnumerator TypeRoutine(string line)
    {
        dialogueText.text = string.Empty;
        for (int i = 0; i < line.Length; i++)
        {
            dialogueText.text += line[i];
            yield return new WaitForSecondsRealtime(0.02f);
        }
    }

    private string GetStateLabel(HospitalSupportState state)
    {
        if (state == HospitalSupportState.Stable)
        {
            return "Estable";
        }

        if (state == HospitalSupportState.Critical)
        {
            return "Critico";
        }

        return "Empeorado";
    }

    private void EnsureUi()
    {
        if (overlayCanvas != null && panelRoot != null && canvasGroup != null && stateText != null && progressFill != null && progressText != null && dialogueText != null && payButton != null && payButtonText != null && liraVisual != null)
        {
            return;
        }

        uiFont = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Canvas canvas = FindOverlayCanvas();
        EnsureEventSystem();

        Transform overlay = canvas.transform.Find("HospitalOverlay");
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject("HospitalOverlay", typeof(RectTransform));
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


        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        panelRoot = EnsureRect(overlay, "Panel", Vector2.zero, new Vector2(760f, 420f), new Vector2(0.5f, 0.5f));
        Image background = panelRoot.GetComponent<Image>();
        if (background == null)
        {
            background = panelRoot.gameObject.AddComponent<Image>();
        }
        background.sprite = HUDSpriteFactory.WhiteSprite;
        background.color = new Color(0.06f, 0.08f, 0.12f, 0.96f);
        canvasGroup = panelRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();
        }

        liraVisual = EnsureRect(panelRoot, "LiraVisual", new Vector2(-150f, -24f), new Vector2(180f, 180f), new Vector2(0.5f, 0.5f));
        Image liraImage = liraVisual.GetComponent<Image>();
        if (liraImage == null)
        {
            liraImage = liraVisual.gameObject.AddComponent<Image>();
        }
        liraImage.sprite = HUDSpriteFactory.WhiteSprite;
        liraImage.color = new Color(0.78f, 0.85f, 1f, 0.8f);

        stateText = EnsureText(panelRoot, "State", 22, TextAnchor.MiddleLeft, Color.white, new Vector2(-40f, 150f), new Vector2(520f, 28f), new Vector2(0f, 0.5f));

        RectTransform barRoot = EnsureRect(panelRoot, "BarRoot", new Vector2(-40f, 94f), new Vector2(520f, 18f), new Vector2(0f, 0.5f));
        Image barBackground = barRoot.GetComponent<Image>();
        if (barBackground == null)
        {
            barBackground = barRoot.gameObject.AddComponent<Image>();
        }
        barBackground.sprite = HUDSpriteFactory.WhiteSprite;
        barBackground.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        progressFill = EnsureImage(barRoot, "Fill", Vector2.zero, Vector2.one, Vector2.zero);
        progressFill.sprite = HUDSpriteFactory.WhiteSprite;
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.color = new Color(0f, 0.75f, 1f, 1f);
        progressText = EnsureText(panelRoot, "ProgressText", 16, TextAnchor.MiddleLeft, Color.white, new Vector2(-40f, 64f), new Vector2(300f, 20f), new Vector2(0f, 0.5f));
        dialogueText = EnsureText(panelRoot, "Dialogue", 18, TextAnchor.UpperLeft, new Color(0.88f, 0.9f, 0.95f, 1f), new Vector2(-40f, 22f), new Vector2(520f, 132f), new Vector2(0f, 1f));

        RectTransform buttonRect = EnsureRect(panelRoot, "PayButton", new Vector2(-40f, -146f), new Vector2(240f, 42f), new Vector2(0f, 0.5f));
        Image buttonImage = buttonRect.GetComponent<Image>();
        if (buttonImage == null)
        {
            buttonImage = buttonRect.gameObject.AddComponent<Image>();
        }
        buttonImage.sprite = HUDSpriteFactory.WhiteSprite;
        buttonImage.color = new Color(0f, 0.45f, 0.7f, 0.95f);
        payButton = buttonRect.GetComponent<Button>();
        if (payButton == null)
        {
            payButton = buttonRect.gameObject.AddComponent<Button>();
        }
        payButtonText = EnsureText(buttonRect, "Text", 18, TextAnchor.MiddleCenter, Color.white, Vector2.zero, buttonRect.sizeDelta, new Vector2(0.5f, 0.5f));

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

    private Image EnsureImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
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
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.sizeDelta = sizeDelta;
        return image;
    }
}
