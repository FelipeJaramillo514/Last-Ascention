using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    public bool isNewGame = true;
    public SaveData currentSave;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<GameStateManager>() != null)
        {
            return;
        }

        new GameObject("GameStateManager").AddComponent<GameStateManager>();
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
        if (currentSave == null)
        {
            currentSave = SaveSystem.CreateNewSave();
        }
    }

    public void InitFromSave(SaveData save)
    {
        currentSave = SaveSystem.Clone(save);
        currentSave.hasSave = true;
        SaveSystem.Normalize(currentSave);
        isNewGame = false;
    }

    public void StartNewGame()
    {
        isNewGame = true;
        currentSave = SaveSystem.CreateNewSave();
        currentSave.hasSave = true;
    }

    public void SaveCurrentState()
    {
        if (currentSave == null)
        {
            currentSave = SaveSystem.CreateNewSave();
        }

        PersistentData persistent = FindFirstObjectByType<PersistentData>();
        if (persistent != null && persistent.SaveData != null)
        {
            currentSave = persistent.SaveData;
        }

        KaisenController controller = FindFirstObjectByType<KaisenController>();
        if (controller != null && controller.Stats != null)
        {
            SaveSystem.ApplyStatsToSave(currentSave, controller.Stats);
        }

        SystemManager systemManager = FindFirstObjectByType<SystemManager>();
        if (systemManager != null && systemManager.Stats != null)
        {
            SaveSystem.ApplyStatsToSave(currentSave, systemManager.Stats);
        }

        // TODO: recoger stats de KaisenStats cuando esté implementado (Prompt 1)
        // TODO: recoger shadowExtractionUnlocked de ShadowExtractionSystem (Prompt 6)
        // TODO: recoger purchasedUpgrades de MetaUpgradeShop (Prompt 8)
        // TODO: recoger totalRunsCompleted de RunManager (Prompt 8)

        SaveSystem.Normalize(currentSave);
        SaveSystem.Save(currentSave);
    }

    private void OnApplicationQuit()
    {
        if (currentSave != null && currentSave.hasSave)
        {
            SaveCurrentState();
        }
    }
}
