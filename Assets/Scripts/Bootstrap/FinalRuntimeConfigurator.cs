using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class FinalRuntimeConfigurator : MonoBehaviour
{
    public static FinalRuntimeConfigurator Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<FinalRuntimeConfigurator>() != null)
        {
            return;
        }

        new GameObject("FinalRuntimeConfigurator").AddComponent<FinalRuntimeConfigurator>();
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
        ApplyGlobalRuntimeSettings();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyGlobalRuntimeSettings();
        ConfigureSceneObjects();
    }

    private void ApplyGlobalRuntimeSettings()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
        QualitySettings.antiAliasing = 0;
        Physics2D.queriesHitTriggers = false;
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        if (Screen.width < 1280 || Screen.height < 720)
        {
            Screen.SetResolution(1280, 720, FullScreenMode.FullScreenWindow);
        }
    }

    private void ConfigureSceneObjects()
    {
        TilemapRenderer[] tilemapRenderers = FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
        for (int i = 0; i < tilemapRenderers.Length; i++)
        {
            if (tilemapRenderers[i] != null)
            {
                tilemapRenderers[i].mode = TilemapRenderer.Mode.Chunk;
            }
        }

        Rigidbody2D[] bodies = FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None);
        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody2D body = bodies[i];
            if (body == null)
            {
                continue;
            }

            if (body.GetComponent<KaisenController>() != null
                || body.GetComponentInParent<EnemyBase>() != null
                || body.GetComponent<ProjectileBase>() != null
                || body.GetComponentInParent<ShadowSoldier>() != null)
            {
                continue;
            }

            body.sleepMode = RigidbodySleepMode2D.StartAsleep;
        }
    }
}
