#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;

public class EditorDebugOverlay : MonoBehaviour
{
    public static EditorDebugOverlay Instance { get; private set; }

    private float smoothedDeltaTime;
    private bool godMode;
    private bool showOverlay;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<EditorDebugOverlay>() != null)
        {
            return;
        }

        new GameObject("EditorDebugOverlay").AddComponent<EditorDebugOverlay>();
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

    private void Update()
    {
        smoothedDeltaTime += (Time.unscaledDeltaTime - smoothedDeltaTime) * 0.1f;

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            ToggleGodMode();
        }

        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            showOverlay = !showOverlay;
        }

        if (Keyboard.current.f2Key.wasPressedThisFrame && SystemManager.Instance != null)
        {
            SystemManager.Instance.DebugForceLevelUp();
        }

        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            PersistentData.shadowExtractionUnlocked = true;
        }

        if (Keyboard.current.f4Key.wasPressedThisFrame)
        {
            PersistentData.Instance.AddWalletGold(9999f);
        }

        if (Keyboard.current.f5Key.wasPressedThisFrame && DungeonBuilder.Instance != null)
        {
            int randomSeed = Random.Range(1000, 999999);
            DungeonBuilder.Instance.BuildDungeon(randomSeed);
        }
    }

    private void OnGUI()
    {
        if (!showOverlay)
        {
            return;
        }

        GUI.color = Color.white;
        float fps = smoothedDeltaTime > 0.0001f ? 1f / smoothedDeltaTime : 0f;
        DungeonBuilder dungeon = DungeonBuilder.Instance;
        DungeonRoom currentRoom = dungeon != null ? dungeon.GetCurrentRoom() : null;
        KaisenController player = FindFirstObjectByType<KaisenController>();
        PlayerHealth health = player != null ? player.GetComponent<PlayerHealth>() : null;

        string text = string.Format(
            "FPS: {0:0}\nSeed: {1}\nSala: {2}\nHP: {3:0}/{4:0}\nNivel: {5}\nGod Mode: {6}",
            fps,
            dungeon != null ? dungeon.CurrentSeed : 0,
            currentRoom != null ? currentRoom.RuntimeRoomType + " #" + currentRoom.RoomIndex : "N/A",
            health != null ? health.CurrentHP : 0f,
            health != null ? health.MaxHP : 0f,
            player != null && player.Stats != null ? player.Stats.systemLevel : 0,
            godMode ? "ON" : "OFF");

        GUI.Box(new Rect(12f, 12f, 220f, 120f), text);
    }

    private void ToggleGodMode()
    {
        godMode = !godMode;
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.SetGodMode(godMode);
        }
    }
}
#endif
