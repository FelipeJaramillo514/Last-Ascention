using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum AssociationMissionMetric
{
    KillEnemies,
    ClearRooms,
    ExtractShadows,
    ReachLevel,
    DefeatBoss
}

[Serializable]
public class AssociationMissionOffer
{
    public string id;
    public string title;
    public string description;
    public AssociationMissionMetric metric;
    public int targetValue;
    public int rewardGold;

    public bool IsCompleted(RunStats stats)
    {
        if (stats == null)
        {
            return false;
        }

        if (metric == AssociationMissionMetric.KillEnemies)
        {
            return stats.enemiesKilled >= targetValue;
        }

        if (metric == AssociationMissionMetric.ClearRooms)
        {
            return stats.roomsCleared >= targetValue;
        }

        if (metric == AssociationMissionMetric.ExtractShadows)
        {
            return stats.shadowsExtracted >= targetValue;
        }

        if (metric == AssociationMissionMetric.ReachLevel)
        {
            return stats.levelReached >= targetValue;
        }

        return stats.bossDefeated;
    }
}

[Serializable]
public class RunStats
{
    public bool completed;
    public bool playerDied;
    public bool bossDefeated;
    public int goldEarned;
    public int enemiesKilled;
    public int roomsCleared;
    public float timeElapsed;
    public int shadowsExtracted;
    public int levelGained;
    public int levelReached;
    public int associationBonusGold;
}

public class RunManager : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string HubSceneName = "CityArken";
    private const string DungeonSceneName = "GameplayScene";

    private static RunManager instance;

    [SerializeField] private RunSummaryUI runSummaryUI;

    private readonly List<AssociationMissionOffer> currentAssociationMissions = new List<AssociationMissionOffer>();

    private RunStats currentRunStats = new RunStats();
    private bool runActive;
    private bool isTransitioning;
    private bool summaryVisible;
    private int pendingSeed;
    private float runStartRealtime;
    private int runStartLevel;

    public static RunManager Instance
    {
        get
        {
            EnsureInstance();
            return instance;
        }
    }

    public IReadOnlyList<AssociationMissionOffer> CurrentAssociationMissions => currentAssociationMissions;
    public RunStats CurrentRunStats => currentRunStats;
    public bool IsRunActive => runActive;
    public int CurrentFloorNumber => 1;

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

        instance = FindFirstObjectByType<RunManager>();
        if (instance == null)
        {
            GameObject managerObject = new GameObject("RunManager");
            instance = managerObject.AddComponent<RunManager>();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        GenerateAssociationMissions();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EventBus.Subscribe<PlayerDeathEvent>(OnPlayerDeath);
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Subscribe<RoomClearedEvent>(OnRoomCleared);
        EventBus.Subscribe<ShadowSummonedEvent>(OnShadowSummoned);
        EventBus.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        EventBus.Unsubscribe<PlayerDeathEvent>(OnPlayerDeath);
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Unsubscribe<RoomClearedEvent>(OnRoomCleared);
        EventBus.Unsubscribe<ShadowSummonedEvent>(OnShadowSummoned);
        EventBus.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
    }

    public bool ShouldManageDungeonBuild(string sceneName)
    {
        return runActive && sceneName == DungeonSceneName;
    }

    public void StartRun()
    {
        if (isTransitioning)
        {
            return;
        }

        Time.timeScale = 1f;
        summaryVisible = false;
        runActive = true;
        isTransitioning = true;
        pendingSeed = UnityEngine.Random.Range(1000, 999999);
        currentRunStats = new RunStats();
        currentRunStats.completed = false;
        currentRunStats.playerDied = false;
        currentRunStats.bossDefeated = false;
        PersistentData.Instance.ResetRun();
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.FadeToScene(DungeonSceneName, 0.5f);
        }
        else
        {
            SceneManager.LoadSceneAsync(DungeonSceneName);
        }
    }

    public void ReturnToHub()
    {
        if (isTransitioning)
        {
            return;
        }

        Time.timeScale = 1f;
        isTransitioning = true;
        runActive = false;
        summaryVisible = false;
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.FadeToScene(HubSceneName, 0.5f);
        }
        else
        {
            SceneManager.LoadSceneAsync(HubSceneName);
        }
    }

    public void EnsureAssociationMissions()
    {
        if (currentAssociationMissions.Count == 0)
        {
            GenerateAssociationMissions();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isTransitioning = false;
        KaisenController player = FindFirstObjectByType<KaisenController>();
        SystemManager systemManager = FindFirstObjectByType<SystemManager>();

        if (scene.name == HubSceneName)
        {
            PersistentData.Instance.ApplyStatsToPlayer(player, true);
            if (systemManager != null)
            {
                systemManager.RebindPlayer(player, true);
            }

            EnsureAssociationMissions();
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SaveCurrentState();
            }
            return;
        }

        if (scene.name == MainMenuSceneName)
        {
            return;
        }

        if (scene.name != DungeonSceneName)
        {
            return;
        }

        PersistentData.Instance.ApplyStatsToPlayer(player, false);
        if (systemManager != null)
        {
            systemManager.RebindPlayer(player, true);
        }

        if (!runActive)
        {
            return;
        }

        DungeonBuilder dungeonBuilder = FindFirstObjectByType<DungeonBuilder>();
        if (dungeonBuilder != null)
        {
            dungeonBuilder.BuildDungeon(pendingSeed);
        }

        runStartRealtime = Time.realtimeSinceStartup;
        runStartLevel = player != null && player.Stats != null ? player.Stats.systemLevel : PersistentData.Instance.SaveData.systemLevel;
        currentRunStats.levelReached = runStartLevel;
    }

    private void OnPlayerDeath(PlayerDeathEvent deathEvent)
    {
        if (!runActive || summaryVisible)
        {
            return;
        }

        currentRunStats.playerDied = true;
        FinishRun(false);
    }

    private void OnEnemyDied(EnemyDiedEvent enemyEvent)
    {
        if (!runActive || enemyEvent == null || enemyEvent.enemy == null)
        {
            return;
        }

        currentRunStats.enemiesKilled++;
        PersistentData.Instance.RegisterEnemyKill();

        if (enemyEvent.enemy.GetComponent<BossBase>() != null)
        {
            currentRunStats.bossDefeated = true;
            FinishRun(true);
        }
    }

    private void OnRoomCleared(RoomClearedEvent roomEvent)
    {
        if (!runActive)
        {
            return;
        }

        currentRunStats.roomsCleared++;
    }

    private void OnShadowSummoned(ShadowSummonedEvent shadowEvent)
    {
        if (!runActive)
        {
            return;
        }

        currentRunStats.shadowsExtracted++;
    }

    private void OnPlayerLevelUp(PlayerLevelUpEvent levelUpEvent)
    {
        if (!runActive || levelUpEvent == null)
        {
            return;
        }

        currentRunStats.levelReached = Mathf.Max(currentRunStats.levelReached, levelUpEvent.newLevel);
        currentRunStats.levelGained = Mathf.Max(0, currentRunStats.levelReached - runStartLevel);
    }

    private void FinishRun(bool completed)
    {
        if (summaryVisible)
        {
            return;
        }

        Time.timeScale = 1f;
        summaryVisible = true;
        runActive = false;
        currentRunStats.completed = completed;
        currentRunStats.timeElapsed = Mathf.Max(0f, Time.realtimeSinceStartup - runStartRealtime);

        SystemManager systemManager = FindFirstObjectByType<SystemManager>();
        KaisenController player = FindFirstObjectByType<KaisenController>();
        currentRunStats.goldEarned = Mathf.RoundToInt(systemManager != null && systemManager.Stats != null ? systemManager.Stats.currentGold : 0f);
        currentRunStats.levelReached = player != null && player.Stats != null ? player.Stats.systemLevel : currentRunStats.levelReached;
        currentRunStats.levelGained = Mathf.Max(0, currentRunStats.levelReached - runStartLevel);

        currentRunStats.associationBonusGold = EvaluateAssociationMissionRewards(currentRunStats);
        currentRunStats.goldEarned += currentRunStats.associationBonusGold;

        PersistentData.Instance.AddWalletGold(currentRunStats.goldEarned);
        PersistentData.Instance.RegisterRunEnd(completed);
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SaveCurrentState();
        }
        else
        {
            PersistentData.Instance.Save();
        }

        if (runSummaryUI == null)
        {
            runSummaryUI = FindFirstObjectByType<RunSummaryUI>();
        }
        if (runSummaryUI == null)
        {
            runSummaryUI = gameObject.AddComponent<RunSummaryUI>();
        }

        runSummaryUI.Show(currentRunStats, () =>
        {
            GenerateAssociationMissions();
            ReturnToHub();
        });
    }

    private int EvaluateAssociationMissionRewards(RunStats stats)
    {
        int totalReward = 0;
        for (int i = 0; i < currentAssociationMissions.Count; i++)
        {
            if (currentAssociationMissions[i].IsCompleted(stats))
            {
                totalReward += Mathf.RoundToInt(currentAssociationMissions[i].rewardGold * GameBalanceData.Instance.goldRewardMultiplier);
            }
        }

        return totalReward;
    }

    private void GenerateAssociationMissions()
    {
        currentAssociationMissions.Clear();
        currentAssociationMissions.Add(CreateMission("hunt_minor", "Caceria menor", "Derrota enemigos en la grieta.", AssociationMissionMetric.KillEnemies, UnityEngine.Random.Range(10, 17), 120));
        currentAssociationMissions.Add(CreateMission("secure_rooms", "Asegurar salas", "Limpia salas sin retroceder demasiado.", AssociationMissionMetric.ClearRooms, UnityEngine.Random.Range(4, 7), 150));
        currentAssociationMissions.Add(CreateMission("shadow_trial", "Prueba de sombras", "Extrae poder de los caidos.", AssociationMissionMetric.ExtractShadows, 1, 220));
        currentAssociationMissions.Add(CreateMission("system_growth", "Crecimiento del sistema", "Sube tu nivel durante la run.", AssociationMissionMetric.ReachLevel, Mathf.Max(2, PersistentData.Instance.SaveData.systemLevel + 1), 180));
        currentAssociationMissions.Add(CreateMission("bear_hunt", "Orden del oso de piedra", "Derrota al guardia del piso.", AssociationMissionMetric.DefeatBoss, 1, 300));
    }

    private AssociationMissionOffer CreateMission(string id, string title, string description, AssociationMissionMetric metric, int targetValue, int baseReward)
    {
        AssociationMissionOffer offer = new AssociationMissionOffer();
        offer.id = id;
        offer.title = title;
        offer.description = description;
        offer.metric = metric;
        offer.targetValue = targetValue;
        offer.rewardGold = baseReward + UnityEngine.Random.Range(0, 45);
        return offer;
    }
}
