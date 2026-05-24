using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private Image fadeImage;

    private Coroutine transitionRoutine;
    private Sprite cachedWhiteSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<SceneTransitionManager>() != null)
        {
            return;
        }

        new GameObject("SceneTransitionManager").AddComponent<SceneTransitionManager>();
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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureUi();
        DisableDuplicateTransitionCanvases();
        FadeOut(0.5f);
    }

    public Coroutine FadeIn(float duration)
    {
        EnsureUi();
        return StartManagedRoutine(FadeRoutine(1f, duration));
    }

    public Coroutine FadeOut(float duration)
    {
        EnsureUi();
        return StartManagedRoutine(FadeRoutine(0f, duration));
    }

    public Coroutine FadeToScene(string sceneName, float duration)
    {
        EnsureUi();
        return StartManagedRoutine(FadeToSceneRoutine(sceneName, duration));
    }

    public Coroutine FadeToScene(string sceneName)
    {
        return FadeToScene(sceneName, 0.5f);
    }

    public IEnumerator FadeToSceneRoutine(string sceneName, float duration)
    {
        yield return FadeRoutine(1f, duration);
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        while (operation != null && !operation.isDone)
        {
            yield return null;
        }

        EnsureUi();
        yield return FadeRoutine(0f, duration);
    }

    private Coroutine StartManagedRoutine(IEnumerator routine)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(ManagedRoutine(routine));
        return transitionRoutine;
    }

    private IEnumerator ManagedRoutine(IEnumerator routine)
    {
        yield return StartCoroutine(routine);
        transitionRoutine = null;
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        EnsureUi();
        float startAlpha = GetCurrentAlpha();
        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, eased));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private void EnsureUi()
    {
        if (overlayCanvas != null && fadeImage != null)
        {
            return;
        }

        if (!AdoptSceneFadeCanvas())
        {
            CreateFallbackCanvas();
        }

        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 999;

        CanvasScaler scaler = overlayCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = overlayCanvas.gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        if (overlayCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            overlayCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        fadeImage.raycastTarget = false;
        if (fadeImage.sprite == null)
        {
            fadeImage.sprite = GetWhiteSprite();
        }

        DontDestroyOnLoad(overlayCanvas.gameObject);
    }

    private void DisableDuplicateTransitionCanvases()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas == overlayCanvas || canvas.name != "Canvas_Transition")
            {
                continue;
            }

            Transform fadeTransform = canvas.transform.Find("Image_Fade");
            Image duplicateFade = fadeTransform != null ? fadeTransform.GetComponent<Image>() : null;
            if (duplicateFade != null)
            {
                Color color = duplicateFade.color;
                color.a = 0f;
                duplicateFade.color = color;
                duplicateFade.raycastTarget = false;
            }

            canvas.gameObject.SetActive(false);
        }
    }

    private bool AdoptSceneFadeCanvas()
    {
        GameObject canvasObject = GameObject.Find("Canvas_Transition");
        if (canvasObject == null)
        {
            return false;
        }

        overlayCanvas = canvasObject.GetComponent<Canvas>();
        if (overlayCanvas == null)
        {
            overlayCanvas = canvasObject.AddComponent<Canvas>();
        }

        Transform fadeTransform = canvasObject.transform.Find("Image_Fade");
        if (fadeTransform == null)
        {
            GameObject fadeObject = new GameObject("Image_Fade", typeof(RectTransform), typeof(Image));
            fadeTransform = fadeObject.transform;
            fadeTransform.SetParent(canvasObject.transform, false);
        }

        fadeImage = fadeTransform.GetComponent<Image>();
        if (fadeImage == null)
        {
            fadeImage = fadeTransform.gameObject.AddComponent<Image>();
        }

        return true;
    }

    private void CreateFallbackCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas_Transition", typeof(RectTransform), typeof(Canvas));
        overlayCanvas = canvasObject.GetComponent<Canvas>();
        GameObject fadeObject = new GameObject("Image_Fade", typeof(RectTransform), typeof(Image));
        fadeObject.transform.SetParent(canvasObject.transform, false);
        fadeImage = fadeObject.GetComponent<Image>();
        fadeImage.sprite = GetWhiteSprite();
        fadeImage.color = Color.black;
        SetAlpha(1f);
    }

    private Sprite GetWhiteSprite()
    {
        if (cachedWhiteSprite == null)
        {
            cachedWhiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        }

        return cachedWhiteSprite;
    }

    private float GetCurrentAlpha()
    {
        return fadeImage != null ? fadeImage.color.a : 1f;
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage == null)
        {
            return;
        }

        Color color = fadeImage.color;
        color.r = 0f;
        color.g = 0f;
        color.b = 0f;
        color.a = Mathf.Clamp01(alpha);
        fadeImage.color = color;
    }
}
