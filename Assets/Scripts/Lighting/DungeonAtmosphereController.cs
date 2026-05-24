using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class DungeonAtmosphereController : MonoBehaviour
{
    public static DungeonAtmosphereController Instance { get; private set; }

    [SerializeField] private Light2D globalLight;
    [SerializeField] private Volume dungeonVolume;

    private VolumeProfile runtimeProfile;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;
    private Bloom bloom;
    private LiftGammaGain liftGammaGain;
    private bool bossRoomActive;
    private string lastDecoratedScene = string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<DungeonAtmosphereController>() != null)
        {
            return;
        }

        GameObject controllerObject = new GameObject("DungeonAtmosphereController");
        controllerObject.AddComponent<DungeonAtmosphereController>();
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

    private void Start()
    {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EventBus.Subscribe<RoomVisitedEvent>(OnRoomVisited);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        EventBus.Unsubscribe<RoomVisitedEvent>(OnRoomVisited);
    }

    private void Update()
    {
        if (!IsDungeonScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        UpdateVolumeState();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllCoroutines();
        bossRoomActive = false;
        lastDecoratedScene = string.Empty;

        if (IsDungeonScene(scene.name))
        {
            if (globalLight != null)
            {
                globalLight.enabled = true;
            }

            if (dungeonVolume != null)
            {
                dungeonVolume.enabled = true;
            }

            StartCoroutine(SetupDungeonRoutine(scene.name));
            return;
        }

        if (globalLight != null)
        {
            globalLight.enabled = false;
        }

        if (dungeonVolume != null)
        {
            dungeonVolume.enabled = false;
        }
    }

    private void OnRoomVisited(RoomVisitedEvent roomVisitedEvent)
    {
        if (roomVisitedEvent == null || roomVisitedEvent.room == null)
        {
            return;
        }

        bossRoomActive = roomVisitedEvent.room.RuntimeRoomType == RoomType.Boss;
        UpdateVolumeState();
    }

    private IEnumerator SetupDungeonRoutine(string sceneName)
    {
        EnsureGlobalLight();
        EnsureVolume();
        EnsurePlayerLight();

        float timeout = 3f;
        while (timeout > 0f)
        {
            DungeonRoom[] rooms = FindObjectsByType<DungeonRoom>(FindObjectsSortMode.None);
            if (rooms != null && rooms.Length > 0)
            {
                DecorateRooms(sceneName, rooms);
                break;
            }

            timeout -= Time.deltaTime;
            yield return null;
        }

        UpdateVolumeState();
    }

    private void EnsureGlobalLight()
    {
        if (globalLight == null)
        {
            Transform existing = transform.Find("DungeonGlobalLight");
            if (existing != null)
            {
                globalLight = existing.GetComponent<Light2D>();
            }
        }

        if (globalLight == null)
        {
            GameObject lightObject = new GameObject("DungeonGlobalLight");
            lightObject.transform.SetParent(transform, false);
            globalLight = lightObject.AddComponent<Light2D>();
        }

        globalLight.lightType = Light2D.LightType.Global;
        globalLight.color = new Color32(62, 66, 86, 255);
        globalLight.intensity = 0.64f;
    }

    private void EnsurePlayerLight()
    {
        KaisenController player = FindFirstObjectByType<KaisenController>();
        if (player == null)
        {
            return;
        }

        Transform playerLightTransform = player.transform.Find("PlayerSubtleLight");
        Light2D playerLight = playerLightTransform != null ? playerLightTransform.GetComponent<Light2D>() : null;
        if (playerLight == null)
        {
            GameObject lightObject = new GameObject("PlayerSubtleLight");
            lightObject.transform.SetParent(player.transform, false);
            playerLight = lightObject.AddComponent<Light2D>();
        }

        playerLight.lightType = Light2D.LightType.Point;
        playerLight.color = new Color(0.82f, 0.9f, 1f, 1f);
        playerLight.intensity = 1.05f;
        playerLight.pointLightOuterRadius = 6.75f;
        playerLight.pointLightInnerRadius = 1.35f;
    }

    private void EnsureVolume()
    {
        if (dungeonVolume == null)
        {
            Transform existing = transform.Find("DungeonGlobalVolume");
            if (existing != null)
            {
                dungeonVolume = existing.GetComponent<Volume>();
            }
        }

        if (dungeonVolume == null)
        {
            GameObject volumeObject = new GameObject("DungeonGlobalVolume");
            volumeObject.transform.SetParent(transform, false);
            dungeonVolume = volumeObject.AddComponent<Volume>();
        }

        runtimeProfile = runtimeProfile != null ? runtimeProfile : ScriptableObject.CreateInstance<VolumeProfile>();
        dungeonVolume.isGlobal = true;
        dungeonVolume.priority = 100f;
        dungeonVolume.sharedProfile = runtimeProfile;

        colorAdjustments = runtimeProfile.TryGet(out colorAdjustments) ? colorAdjustments : runtimeProfile.Add<ColorAdjustments>(true);
        vignette = runtimeProfile.TryGet(out vignette) ? vignette : runtimeProfile.Add<Vignette>(true);
        bloom = runtimeProfile.TryGet(out bloom) ? bloom : runtimeProfile.Add<Bloom>(true);
        liftGammaGain = runtimeProfile.TryGet(out liftGammaGain) ? liftGammaGain : runtimeProfile.Add<LiftGammaGain>(true);

        colorAdjustments.postExposure.Override(0.55f);
        colorAdjustments.contrast.Override(6f);
        colorAdjustments.saturation.Override(0f);

        vignette.color.Override(new Color(0f, 0f, 0f, 1f));
        vignette.intensity.Override(0.1f);
        vignette.smoothness.Override(0.35f);
        vignette.rounded.Override(false);

        bloom.intensity.Override(0.12f);
        bloom.threshold.Override(0.8f);

        liftGammaGain.lift.Override(new Vector4(0.03f, 0.03f, 0.06f, 0f));
    }

    private void DecorateRooms(string sceneName, DungeonRoom[] rooms)
    {
        if (lastDecoratedScene == sceneName)
        {
            return;
        }

        lastDecoratedScene = sceneName;
        for (int i = 0; i < rooms.Length; i++)
        {
            DungeonRoom room = rooms[i];
            if (room == null)
            {
                continue;
            }

            BoxCollider2D bounds = room.GetComponent<BoxCollider2D>();
            if (bounds == null)
            {
                continue;
            }

            Bounds roomBounds = bounds.bounds;
            CreateTorch(room.transform, "TorchLeft", new Vector3(roomBounds.min.x + 1.4f, roomBounds.max.y - 1.2f, 0f));
            CreateTorch(room.transform, "TorchRight", new Vector3(roomBounds.max.x - 1.4f, roomBounds.max.y - 1.2f, 0f));
            CreateCrack(room.transform, "CrackTop", new Vector3(roomBounds.center.x, roomBounds.max.y - 0.9f, 0f));

            if (room.RuntimeRoomType == RoomType.Boss)
            {
                CreateCrack(room.transform, "CrackBossLeft", new Vector3(roomBounds.min.x + 2.2f, roomBounds.center.y + 1.6f, 0f));
                CreateCrack(room.transform, "CrackBossRight", new Vector3(roomBounds.max.x - 2.2f, roomBounds.center.y + 1.6f, 0f));
            }
        }
    }

    private void CreateTorch(Transform parent, string objectName, Vector3 worldPosition)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return;
        }

        GameObject torch = new GameObject(objectName);
        torch.transform.SetParent(parent, true);
        torch.transform.position = worldPosition;
        torch.AddComponent<TorchLight>();
    }

    private void CreateCrack(Transform parent, string objectName, Vector3 worldPosition)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return;
        }

        GameObject crack = new GameObject(objectName);
        crack.transform.SetParent(parent, true);
        crack.transform.position = worldPosition;
        crack.AddComponent<CrackLight>();
        crack.AddComponent<CrackAmbientSound>();
    }

    private void UpdateVolumeState()
    {
        if (colorAdjustments == null || vignette == null || bloom == null)
        {
            return;
        }

        float baseSaturation = 0f;
        float baseVignette = bossRoomActive ? 0.28f : 0.1f;
        float lowHpExtra = 0f;

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null && playerHealth.MaxHP > 0f)
        {
            float hpRatio = playerHealth.CurrentHP / playerHealth.MaxHP;
            if (hpRatio < 0.3f)
            {
                float pulse = Mathf.Lerp(0.4f, 0.6f, (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f) + 1f) * 0.5f);
                lowHpExtra = pulse - 0.3f;
                baseSaturation = Mathf.Lerp(-12f, 4f, (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f) + 1f) * 0.5f);
            }
        }

        colorAdjustments.saturation.Override(baseSaturation);
        vignette.intensity.Override(Mathf.Clamp01(baseVignette + lowHpExtra));
        bloom.intensity.Override(bossRoomActive ? 0.2f : 0.12f);
    }

    private bool IsDungeonScene(string sceneName)
    {
        return sceneName == "GameplayScene";
    }
}
