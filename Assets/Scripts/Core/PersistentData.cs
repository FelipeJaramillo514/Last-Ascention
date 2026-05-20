using UnityEngine;

public enum HospitalSupportState
{
    Stable,
    Critical,
    Worsening
}

public class PersistentData : MonoBehaviour
{
    private const float MonthlyHospitalTarget = SaveSystem.DefaultHospitalTarget;

    private static PersistentData instance;

    [SerializeField] private SaveData saveData;

    public static PersistentData Instance
    {
        get
        {
            EnsureInstance();
            return instance;
        }
    }

    public static bool HasExistingSave
    {
        get { return SaveSystem.HasSave(); }
    }

    public static bool shadowExtractionUnlocked
    {
        get { return Instance.saveData.shadowExtractionUnlocked; }
        set
        {
            Instance.saveData.shadowExtractionUnlocked = value;
            Instance.Save();
        }
    }

    public static bool shadowUnlockAnnouncementShown
    {
        get { return Instance.saveData.shadowUnlockAnnouncementShown; }
        set
        {
            Instance.saveData.shadowUnlockAnnouncementShown = value;
            Instance.Save();
        }
    }

    public static int maxShadowSlots
    {
        get { return Mathf.Max(1, Instance.saveData.maxShadowSlots); }
        set
        {
            Instance.saveData.maxShadowSlots = Mathf.Max(1, value);
            Instance.Save();
        }
    }

    public SaveData SaveData
    {
        get { return saveData; }
    }

    public KaisenStats PermanentStats
    {
        get { return BuildHubStats(); }
    }

    public float SpendableGold
    {
        get { return saveData != null ? saveData.currentGold : 0f; }
    }

    public float GoldForHospital
    {
        get { return saveData != null ? saveData.goldForHospital : 0f; }
    }

    public float HospitalMonthlyTarget
    {
        get { return MonthlyHospitalTarget; }
    }

    public int RemainingRunsThisMonth
    {
        get { return saveData == null ? 3 : Mathf.Max(0, 3 - saveData.runsSinceHospitalReset); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        instance = FindFirstObjectByType<PersistentData>();
        if (instance == null)
        {
            GameObject persistentObject = new GameObject("PersistentData");
            instance = persistentObject.AddComponent<PersistentData>();
        }

        instance.Initialize();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Initialize();
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    private void Initialize()
    {
        DontDestroyOnLoad(gameObject);
        if (saveData == null)
        {
            SaveData liveSave = GameStateManager.Instance != null ? GameStateManager.Instance.currentSave : null;
            if (liveSave != null)
            {
                saveData = liveSave;
            }
            else
            {
                saveData = SaveSystem.Load();
                if (saveData == null)
                {
                    saveData = SaveSystem.CreateNewSave();
                }
            }
        }

        EnsureDefaults();
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.currentSave = saveData;
        }
    }

    public void Save()
    {
        EnsureDefaults();
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.currentSave = saveData;
        }

        SaveSystem.Save(saveData);
    }

    public void Load()
    {
        SaveData loaded = GameStateManager.Instance != null && GameStateManager.Instance.currentSave != null
            ? GameStateManager.Instance.currentSave
            : SaveSystem.Load();

        saveData = loaded ?? SaveSystem.CreateNewSave();
        EnsureDefaults();
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.currentSave = saveData;
        }
    }

    public void ResetRun()
    {
        KaisenController controller = FindFirstObjectByType<KaisenController>();
        if (controller != null && controller.Stats != null)
        {
            controller.Stats.currentGold = 0f;
            controller.Stats.ResetRuntimeModifiers();
        }
    }

    public KaisenStats BuildHubStats()
    {
        EnsureDefaults();
        KaisenStats stats = SaveSystem.BuildStats(saveData);
        stats.currentGold = saveData.currentGold;
        stats.hospitalDebt = saveData.hospitalDebt;
        stats.ResetRuntimeModifiers();
        return stats;
    }

    public KaisenStats BuildRunStats()
    {
        EnsureDefaults();
        KaisenStats stats = SaveSystem.BuildStats(saveData);
        stats.currentGold = 0f;
        stats.hospitalDebt = saveData.hospitalDebt;
        stats.ResetRuntimeModifiers();

        HospitalSupportState state = GetHospitalState();
        if (state == HospitalSupportState.Stable)
        {
            stats.runAttackMultiplier = 1.15f;
        }
        else if (state == HospitalSupportState.Worsening)
        {
            stats.runAttackMultiplier = 0.9f;
            stats.runMoveSpeedMultiplier = 0.9f;
            stats.runMaxHpMultiplier = 0.9f;
            stats.runPerceptionMultiplier = 0.9f;
            stats.runDodgeCooldownMultiplier = 1.1f;
            stats.runVisualMuted = true;
        }

        return stats;
    }

    public void ApplyStatsToPlayer(KaisenController controller, bool forHub)
    {
        if (controller == null)
        {
            return;
        }

        KaisenStats runtimeStats = forHub ? BuildHubStats() : BuildRunStats();
        controller.SetStats(runtimeStats);
        controller.SetCombatInputEnabled(!forHub);
        controller.SetVisualTint(runtimeStats.runVisualMuted ? new Color(0.62f, 0.62f, 0.62f, 1f) : Color.white);

        PlayerHealth playerHealth = controller.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.RestoreToMaxHealth();
        }
    }

    public void AddWalletGold(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        EnsureDefaults();
        saveData.currentGold += amount;
        saveData.totalGoldEarned += amount;
        SyncLiveGold(saveData.currentGold, amount);
        Save();
    }

    public bool SpendWalletGold(float amount)
    {
        EnsureDefaults();
        if (amount <= 0f || saveData.currentGold < amount)
        {
            return false;
        }

        saveData.currentGold -= amount;
        SyncLiveGold(saveData.currentGold, -amount);
        Save();
        return true;
    }

    public bool PayHospital(float amount)
    {
        EnsureDefaults();
        float remaining = Mathf.Max(0f, MonthlyHospitalTarget - saveData.goldForHospital);
        float payment = Mathf.Clamp(amount, 0f, Mathf.Min(remaining, saveData.currentGold));
        if (payment <= 0f)
        {
            return false;
        }

        saveData.currentGold -= payment;
        saveData.goldForHospital += payment;
        saveData.hospitalDebt = Mathf.Max(0f, MonthlyHospitalTarget - saveData.goldForHospital);
        saveData.liraHealthPercent = Mathf.Clamp01(saveData.goldForHospital / MonthlyHospitalTarget);
        SyncLiveGold(saveData.currentGold, -payment);
        Save();
        return true;
    }

    public float GetSuggestedHospitalPayment()
    {
        EnsureDefaults();
        float remaining = Mathf.Max(0f, MonthlyHospitalTarget - saveData.goldForHospital);
        return Mathf.Min(5000f, Mathf.Min(remaining, saveData.currentGold));
    }

    public HospitalSupportState GetHospitalState()
    {
        float ratio = MonthlyHospitalTarget <= 0f ? 0f : Mathf.Clamp01(saveData.goldForHospital / MonthlyHospitalTarget);
        if (ratio >= 0.8f)
        {
            return HospitalSupportState.Stable;
        }

        if (ratio >= 0.3f)
        {
            return HospitalSupportState.Critical;
        }

        return HospitalSupportState.Worsening;
    }

    public void RegisterRunEnd(bool completed)
    {
        EnsureDefaults();
        if (completed)
        {
            saveData.totalRunsCompleted++;
        }

        saveData.runsSinceHospitalReset++;
        if (saveData.runsSinceHospitalReset >= 3)
        {
            saveData.runsSinceHospitalReset = 0;
            saveData.goldForHospital = 0f;
            saveData.hospitalDebt = MonthlyHospitalTarget;
            saveData.liraHealthPercent = 0f;
        }

        Save();
    }

    public void RegisterEnemyKill()
    {
        EnsureDefaults();
        saveData.totalEnemiesKilled++;
    }

    public void ApplyLevelUp(StatChanges changes)
    {
        EnsureDefaults();
        if (changes.newLevel > saveData.systemLevel)
        {
            saveData.systemLevel = changes.newLevel;
        }

        saveData.strength += changes.strengthDelta;
        saveData.agility += changes.agilityDelta;
        saveData.resistance += changes.resistanceDelta;
        saveData.perception += changes.perceptionDelta;
        saveData.skillPoints += changes.skillPointsDelta;
        Save();
    }

    public int GetUpgradePurchaseCount(string upgradeKey)
    {
        EnsureDefaults();
        int count = 0;
        for (int i = 0; i < saveData.purchasedUpgrades.Count; i++)
        {
            if (saveData.purchasedUpgrades[i] == upgradeKey)
            {
                count++;
            }
        }

        return count;
    }

    public bool PurchaseUpgrade(string upgradeKey, float cost)
    {
        EnsureDefaults();
        if (string.IsNullOrWhiteSpace(upgradeKey) || !SpendWalletGold(cost))
        {
            return false;
        }

        saveData.purchasedUpgrades.Add(upgradeKey);
        ApplyUpgradeEffects(upgradeKey);
        Save();
        return true;
    }

    private void ApplyUpgradeEffects(string upgradeKey)
    {
        if (upgradeKey == "vitalidad_extra")
        {
            saveData.bonusMaxHP += 5f;
            return;
        }

        if (upgradeKey == "slot_sombra")
        {
            saveData.maxShadowSlots += 1;
            return;
        }

        if (upgradeKey == "filo_eterno")
        {
            saveData.swordDamageBonusPercent += 0.05f;
            return;
        }

        if (upgradeKey == "reflejos")
        {
            saveData.bonusDodgeCooldownReduction += 0.1f;
            return;
        }

        if (upgradeKey == "percepcion_aguda")
        {
            saveData.perception += 3f;
        }
    }

    private void SyncLiveGold(float currentGold, float delta)
    {
        KaisenController controller = FindFirstObjectByType<KaisenController>();
        if (controller != null && controller.Stats != null)
        {
            controller.Stats.currentGold = currentGold;
        }

        SystemManager systemManager = FindFirstObjectByType<SystemManager>();
        if (systemManager != null && systemManager.Stats != null)
        {
            systemManager.Stats.currentGold = currentGold;
        }

        EventBus.Publish(new GoldChangedEvent(currentGold, delta));
    }

    private void EnsureDefaults()
    {
        if (saveData == null)
        {
            saveData = SaveSystem.CreateNewSave();
        }

        SaveSystem.Normalize(saveData);
        saveData.officialRank = "E";
    }
}
