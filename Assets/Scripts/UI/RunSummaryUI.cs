using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RunSummaryUI : MonoBehaviour
{
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private Text titleText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Text goldText;
    [SerializeField] private Text footerText;
    [SerializeField] private RectTransform creditsViewport;
    [SerializeField] private Text creditsText;
    [SerializeField] private Button continueButton;
    [SerializeField] private float creditsRollDuration = 18f;

    private Font uiFont;
    private Coroutine showRoutine;

    private void Awake()
    {
        EnsureUi();
    }

    public void Show(RunStats stats, Action continueAction)
    {
        EnsureUi();
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
        }

        showRoutine = StartCoroutine(ShowRoutine(stats, continueAction));
    }

    private IEnumerator ShowRoutine(RunStats stats, Action continueAction)
    {
        panelRoot.gameObject.SetActive(true);
        panelCanvasGroup.alpha = 0f;
        titleText.text = string.Empty;
        bodyText.text = string.Empty;
        goldText.text = string.Empty;
        footerText.text = string.Empty;
        creditsText.text = string.Empty;
        creditsViewport.gameObject.SetActive(false);
        bodyText.gameObject.SetActive(true);
        goldText.gameObject.SetActive(true);

        bool playerDied = stats != null && stats.playerDied;
        bool runCompleted = stats != null && stats.completed;
        titleText.color = runCompleted ? new Color(0.3f, 1f, 0.45f, 1f) : new Color(1f, 0.25f, 0.25f, 1f);
        continueButton.interactable = false;
        continueButton.gameObject.SetActive(false);
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(() =>
        {
            panelRoot.gameObject.SetActive(false);
            continueAction?.Invoke();
        });

        float fadeDuration = playerDied ? 3f : 0.35f;
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            panelCanvasGroup.alpha = Mathf.Clamp01(fadeElapsed / fadeDuration);
            yield return null;
        }

        panelCanvasGroup.alpha = 1f;
        string titleValue = runCompleted ? "RUN COMPLETADA" : "KAISEN HA CAIDO";
        yield return Typewrite(titleText, titleValue, playerDied ? 0.05f : 0.015f);

        if (playerDied)
        {
            yield return new WaitForSecondsRealtime(0.45f);
            footerText.color = new Color(0.45f, 0.75f, 1f, 1f);
            yield return Typewrite(footerText, "El sistema continua. Lira te espera.", 0.035f);
            yield return new WaitForSecondsRealtime(0.4f);
        }
        else
        {
            footerText.text = string.Empty;
        }

        float countDuration = 1.4f;
        float countElapsed = 0f;
        int targetGold = stats != null ? stats.goldEarned : 0;
        while (countElapsed < countDuration)
        {
            countElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(countElapsed / countDuration);
            int displayedGold = Mathf.RoundToInt(Mathf.Lerp(0f, targetGold, t));
            bodyText.text = BuildStatsText(stats, t);
            goldText.text = "Cristales obtenidos: " + displayedGold;
            yield return null;
        }

        bodyText.text = BuildStatsText(stats, 1f);
        goldText.text = "Cristales obtenidos: " + targetGold;

        if (runCompleted)
        {
            yield return new WaitForSecondsRealtime(0.8f);
            yield return PlayCreditsRoll();
        }

        if (playerDied)
        {
            yield return new WaitForSecondsRealtime(5f);
        }

        continueButton.gameObject.SetActive(true);
        continueButton.interactable = true;
        showRoutine = null;
    }

    private IEnumerator Typewrite(Text target, string value, float interval)
    {
        target.text = string.Empty;
        if (string.IsNullOrEmpty(value))
        {
            yield break;
        }

        for (int i = 0; i < value.Length; i++)
        {
            target.text += value[i];
            yield return new WaitForSecondsRealtime(interval);
        }
    }

    private IEnumerator PlayCreditsRoll()
    {
        bodyText.gameObject.SetActive(false);
        goldText.gameObject.SetActive(false);
        footerText.color = new Color(0.55f, 0.88f, 1f, 1f);
        footerText.text = "Gracias por completar la caceria.";
        titleText.color = Color.white;
        titleText.text = "CREDITOS";

        creditsViewport.gameObject.SetActive(true);
        creditsText.text = BuildCreditsText();
        RectTransform creditsRect = creditsText.rectTransform;
        creditsRect.sizeDelta = new Vector2(creditsRect.sizeDelta.x, 880f);

        Canvas.ForceUpdateCanvases();
        float viewportHeight = creditsViewport.rect.height;
        float startY = -viewportHeight - 28f;
        float endY = creditsRect.sizeDelta.y + 28f;
        float elapsed = 0f;
        while (elapsed < creditsRollDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, creditsRollDuration));
            creditsRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(startY, endY, t));
            yield return null;
        }

        creditsRect.anchoredPosition = new Vector2(0f, endY);
        footerText.text = "FIN";
        yield return new WaitForSecondsRealtime(0.9f);
    }

    private string BuildStatsText(RunStats stats, float t)
    {
        int enemies = stats != null ? Mathf.RoundToInt(stats.enemiesKilled * t) : 0;
        int rooms = stats != null ? Mathf.RoundToInt(stats.roomsCleared * t) : 0;
        int shadows = stats != null ? Mathf.RoundToInt(stats.shadowsExtracted * t) : 0;
        int level = stats != null ? Mathf.RoundToInt(stats.levelReached * t) : 0;
        float timeValue = stats != null ? stats.timeElapsed * t : 0f;
        return string.Format(
            "Enemigos derrotados: {0}\nSalas limpiadas: {1}\nSombras extraidas: {2}\nNivel alcanzado: {3}\nTiempo: {4}",
            enemies,
            rooms,
            shadows,
            level,
            FormatTime(timeValue));
    }

    private string BuildCreditsText()
    {
        return
            "LAST ASCENTION\n\n" +
            "DIRECCION DEL PROYECTO\n" +
            "Bayron Felipe Jaramillo\n\n" +
            "PROGRAMACION\n" +
            "Luis Esteban Castillo\n" +
            "Matheu Ruales Galvis\n\n" +
            "DISENO DE UI\n" +
            "Bayron Felipe Jaramillo\n" +
            "Matheu Ruales Galvis\n\n" +
            "DISENO DE PERSONAJES\n" +
            "Luis Esteban Castillo\n" +
            "Bayron Felipe Jaramillo\n\n" +
            "DISENO DE ENEMIGOS\n" +
            "Matheu Ruales Galvis\n" +
            "Luis Esteban Castillo\n\n" +
            "DISENO DE MAZMORRAS\n" +
            "Bayron Felipe Jaramillo\n" +
            "Luis Esteban Castillo\n\n" +
            "ARTE PIXEL Y ANIMACION\n" +
            "Matheu Ruales Galvis\n" +
            "Bayron Felipe Jaramillo\n\n" +
            "SISTEMA DE SOMBRAS\n" +
            "Luis Esteban Castillo\n" +
            "Matheu Ruales Galvis\n\n" +
            "BALANCE Y GAMEPLAY\n" +
            "Bayron Felipe Jaramillo\n" +
            "Luis Esteban Castillo\n" +
            "Matheu Ruales Galvis\n\n" +
            "AUDIO Y AMBIENTACION\n" +
            "Matheu Ruales Galvis\n\n" +
            "PRUEBAS Y CONTROL DE CALIDAD\n" +
            "Bayron Felipe Jaramillo\n" +
            "Luis Esteban Castillo\n" +
            "Matheu Ruales Galvis\n\n" +
            "AGRADECIMIENTOS ESPECIALES\n" +
            "A todos los cazadores que llegaron hasta el final.\n\n\n" +
            "GRACIAS POR JUGAR";
    }

    private string FormatTime(float timeElapsed)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(Mathf.Max(0f, timeElapsed));
        return string.Format("{0:00}:{1:00}", timeSpan.Minutes, timeSpan.Seconds);
    }

    private void EnsureUi()
    {
        if (overlayCanvas != null && panelRoot != null && panelCanvasGroup != null && titleText != null && bodyText != null && goldText != null && footerText != null && creditsViewport != null && creditsText != null && continueButton != null)
        {
            return;
        }

        uiFont = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Canvas canvas = FindOverlayCanvas();
        EnsureEventSystem();

        Transform overlay = canvas.transform.Find("RunSummaryOverlay");
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject("RunSummaryOverlay", typeof(RectTransform));
            overlay = overlayObject.transform;
            overlay.SetParent(canvas.transform, false);
        }

        overlayCanvas = overlay.GetComponent<Canvas>();
        if (overlayCanvas == null)
        {
            overlayCanvas = overlay.gameObject.AddComponent<Canvas>();
        }
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 240;
        if (overlay.GetComponent<GraphicRaycaster>() == null)
        {
            overlay.gameObject.AddComponent<GraphicRaycaster>();
        }


        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        panelRoot = EnsureRect(overlay, "Panel", Vector2.zero, new Vector2(620f, 420f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        Image background = panelRoot.GetComponent<Image>();
        if (background == null)
        {
            background = panelRoot.gameObject.AddComponent<Image>();
        }
        background.sprite = HUDSpriteFactory.WhiteSprite;
        background.color = new Color(0f, 0f, 0f, 0.92f);
        panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();
        }

        titleText = EnsureText(panelRoot, "Title", 32, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, 164f), new Vector2(520f, 42f), new Vector2(0.5f, 0.5f));
        bodyText = EnsureText(panelRoot, "Body", 18, TextAnchor.UpperLeft, Color.white, new Vector2(-220f, 88f), new Vector2(440f, 170f), new Vector2(0f, 1f));
        goldText = EnsureText(panelRoot, "Gold", 22, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0f, 1f), new Vector2(0f, -40f), new Vector2(320f, 26f), new Vector2(0.5f, 0.5f));
        footerText = EnsureText(panelRoot, "Footer", 18, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, -112f), new Vector2(520f, 48f), new Vector2(0.5f, 0.5f));

        creditsViewport = EnsureRect(panelRoot, "CreditsViewport", new Vector2(0f, -6f), new Vector2(540f, 260f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        if (creditsViewport.GetComponent<RectMask2D>() == null)
        {
            creditsViewport.gameObject.AddComponent<RectMask2D>();
        }

        creditsText = EnsureText(creditsViewport, "CreditsText", 18, TextAnchor.UpperCenter, Color.white, new Vector2(0f, 0f), new Vector2(500f, 880f), new Vector2(0.5f, 1f));
        RectTransform creditsRect = creditsText.rectTransform;
        creditsRect.anchorMin = new Vector2(0.5f, 1f);
        creditsRect.anchorMax = new Vector2(0.5f, 1f);
        creditsRect.pivot = new Vector2(0.5f, 1f);
        creditsText.lineSpacing = 1.05f;
        creditsViewport.gameObject.SetActive(false);

        RectTransform buttonRect = EnsureRect(panelRoot, "ContinueButton", new Vector2(0f, -170f), new Vector2(180f, 40f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        Image buttonImage = buttonRect.GetComponent<Image>();
        if (buttonImage == null)
        {
            buttonImage = buttonRect.gameObject.AddComponent<Image>();
        }
        buttonImage.sprite = HUDSpriteFactory.WhiteSprite;
        buttonImage.color = new Color(0f, 0.75f, 1f, 0.85f);
        continueButton = buttonRect.GetComponent<Button>();
        if (continueButton == null)
        {
            continueButton = buttonRect.gameObject.AddComponent<Button>();
        }
        Text buttonText = EnsureText(buttonRect, "Text", 18, TextAnchor.MiddleCenter, Color.white, Vector2.zero, buttonRect.sizeDelta, new Vector2(0.5f, 0.5f));
        buttonText.text = "Continuar";

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

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        DontDestroyOnLoad(eventSystemObject);
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

    private Text EnsureText(Transform parent, string name, int fontSize, TextAnchor alignment, Color color, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
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
