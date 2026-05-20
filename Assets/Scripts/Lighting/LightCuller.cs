using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class LightCuller : MonoBehaviour
{
    public static LightCuller Instance { get; private set; }

    [SerializeField] private float cullDistance = 15f;
    [SerializeField] private int maxActiveLocalLights = 6;

    private readonly List<Light2D> trackedLights = new List<Light2D>();
    private readonly List<LightDistanceEntry> distances = new List<LightDistanceEntry>();

    private Transform playerTarget;

    private struct LightDistanceEntry
    {
        public Light2D light;
        public float sqrDistance;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<LightCuller>() != null)
        {
            return;
        }

        new GameObject("LightCuller").AddComponent<LightCuller>();
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
        RefreshLights();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (playerTarget == null)
        {
            KaisenController player = FindFirstObjectByType<KaisenController>();
            playerTarget = player != null ? player.transform : null;
        }

        if (playerTarget == null)
        {
            return;
        }

        CullLights();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        playerTarget = null;
        RefreshLights();
    }

    private void RefreshLights()
    {
        trackedLights.Clear();
        Light2D[] sceneLights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneLights.Length; i++)
        {
            if (sceneLights[i] != null)
            {
                trackedLights.Add(sceneLights[i]);
            }
        }
    }

    private void CullLights()
    {
        distances.Clear();
        float maxDistanceSqr = cullDistance * cullDistance;

        for (int i = 0; i < trackedLights.Count; i++)
        {
            Light2D light = trackedLights[i];
            if (light == null)
            {
                continue;
            }

            if (light.lightType == Light2D.LightType.Global)
            {
                light.enabled = true;
                continue;
            }

            if (light.transform.IsChildOf(playerTarget))
            {
                light.enabled = true;
                continue;
            }

            float sqrDistance = (light.transform.position - playerTarget.position).sqrMagnitude;
            if (sqrDistance > maxDistanceSqr)
            {
                light.enabled = false;
                continue;
            }

            distances.Add(new LightDistanceEntry { light = light, sqrDistance = sqrDistance });
        }

        distances.Sort((a, b) => a.sqrDistance.CompareTo(b.sqrDistance));
        for (int i = 0; i < distances.Count; i++)
        {
            if (distances[i].light != null)
            {
                distances[i].light.enabled = i < maxActiveLocalLights;
            }
        }
    }
}
