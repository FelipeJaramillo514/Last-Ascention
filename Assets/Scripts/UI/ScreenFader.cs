using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<ScreenFader>() != null)
        {
            return;
        }

        new GameObject("ScreenFader"
        ).AddComponent<ScreenFader>();
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
    }

    public Coroutine FadeIn(float duration)
    {
        return SceneTransitionManager.Instance != null ? SceneTransitionManager.Instance.FadeIn(duration) : null;
    }

    public Coroutine FadeOut(float duration)
    {
        return SceneTransitionManager.Instance != null ? SceneTransitionManager.Instance.FadeOut(duration) : null;
    }

    public void FadeToScene(string sceneName, float duration)
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.FadeToScene(sceneName, duration);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void FadeToScene(string sceneName)
    {
        FadeToScene(sceneName, 0.5f);
    }

    public void FadeToScene(string sceneName, float duration, Action onSceneLoaded)
    {
        StartCoroutine(FadeToSceneProxy(sceneName, duration, onSceneLoaded));
    }

    private IEnumerator FadeToSceneProxy(string sceneName, float duration, Action onSceneLoaded)
    {
        if (SceneTransitionManager.Instance != null)
        {
            yield return SceneTransitionManager.Instance.FadeToSceneRoutine(sceneName, duration);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }

        if (onSceneLoaded != null)
        {
            onSceneLoaded();
        }
    }
}
