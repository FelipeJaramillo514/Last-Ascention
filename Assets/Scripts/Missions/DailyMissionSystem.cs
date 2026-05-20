using System;
using System.Collections.Generic;
using UnityEngine;

public enum DailyMissionType
{
    Resistance,
    Precision,
    Exploration,
    Dureza
}

[Serializable]
public class DailyMissionRuntime
{
    public DailyMissionType missionType;
    public string displayName;
    public string rewardText;
    public int targetValue;
    public int currentValue;
    public bool completed;

    public float ProgressNormalized
    {
        get
        {
            if (targetValue <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)currentValue / targetValue);
        }
    }
}

public class DailyMissionSystem : MonoBehaviour
{
    public static DailyMissionSystem Instance { get; private set; }

    [SerializeField] private SystemManager systemManager;
    [SerializeField] private DungeonBuilder dungeonBuilder;

    private readonly List<DailyMissionRuntime> activeMissions = new List<DailyMissionRuntime>();

    private DungeonLayout observedLayout;
    private DungeonRoom currentRoom;
    private bool runInitialized;
    private bool tookDamageInCurrentRoom;
    private bool usedItemSinceRoomEntry;
    private bool penaltyTriggered;
    private int killsWithoutDamageInRoom;
    private int consecutiveRoomsWithoutItems;
    private int completedMissionCount;

    public IReadOnlyList<DailyMissionRuntime> ActiveMissions { get { return activeMissions; } }
    public event Action MissionsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (systemManager == null)
        {
            systemManager = FindFirstObjectByType<SystemManager>();
        }

        if (dungeonBuilder == null)
        {
            dungeonBuilder = FindFirstObjectByType<DungeonBuilder>();
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Subscribe<RoomClearedEvent>(OnRoomCleared);
        EventBus.Subscribe<RoomVisitedEvent>(OnRoomVisited);
        EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Unsubscribe<RoomClearedEvent>(OnRoomCleared);
        EventBus.Unsubscribe<RoomVisitedEvent>(OnRoomVisited);
        EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
    }

    private void Update()
    {
        if (dungeonBuilder == null)
        {
            dungeonBuilder = FindFirstObjectByType<DungeonBuilder>();
        }

        if (systemManager == null)
        {
            systemManager = FindFirstObjectByType<SystemManager>();
        }

        if (dungeonBuilder == null || dungeonBuilder.CurrentLayout == null)
        {
            return;
        }

        if (!runInitialized || observedLayout != dungeonBuilder.CurrentLayout)
        {
            BeginRun(dungeonBuilder.CurrentLayout);
        }
    }

    public void NotifyItemUsed()
    {
        usedItemSinceRoomEntry = true;
    }

    private void BeginRun(DungeonLayout layout)
    {
        observedLayout = layout;
        runInitialized = true;
        activeMissions.Clear();
        completedMissionCount = 0;
        penaltyTriggered = false;
        tookDamageInCurrentRoom = false;
        usedItemSinceRoomEntry = false;
        killsWithoutDamageInRoom = 0;
        consecutiveRoomsWithoutItems = 0;

        List<DailyMissionRuntime> pool = new List<DailyMissionRuntime>();
        pool.Add(new DailyMissionRuntime { missionType = DailyMissionType.Resistance, displayName = "Resistencia", rewardText = "+10 gold", targetValue = 20, currentValue = 0 });
        pool.Add(new DailyMissionRuntime { missionType = DailyMissionType.Precision, displayName = "Precision", rewardText = "+sala secreta", targetValue = 10, currentValue = 0 });
        pool.Add(new DailyMissionRuntime { missionType = DailyMissionType.Exploration, displayName = "Exploracion", rewardText = "+5% daño por 1 piso", targetValue = dungeonBuilder != null ? dungeonBuilder.GetTotalRoomCount() : 0, currentValue = 0 });
        pool.Add(new DailyMissionRuntime { missionType = DailyMissionType.Dureza, displayName = "Dureza", rewardText = "+2 al stat mas bajo", targetValue = 3, currentValue = 0 });

        while (pool.Count > 0 && activeMissions.Count < 3)
        {
            int pick = UnityEngine.Random.Range(0, pool.Count);
            activeMissions.Add(pool[pick]);
            pool.RemoveAt(pick);
        }

        currentRoom = dungeonBuilder != null ? dungeonBuilder.GetCurrentRoom() : null;
        UpdateExplorationMission();
        NotifyMissionStateChanged();
    }

    private void OnRoomVisited(RoomVisitedEvent roomVisitedEvent)
    {
        currentRoom = roomVisitedEvent != null ? roomVisitedEvent.room : null;
        tookDamageInCurrentRoom = false;
        usedItemSinceRoomEntry = false;
        killsWithoutDamageInRoom = 0;

        DailyMissionRuntime precisionMission = FindMission(DailyMissionType.Precision);
        if (precisionMission != null && !precisionMission.completed)
        {
            precisionMission.currentValue = 0;
        }

        UpdateExplorationMission();
        NotifyMissionStateChanged();
    }

    private void OnPlayerDamaged(PlayerDamagedEvent playerDamagedEvent)
    {
        tookDamageInCurrentRoom = true;
        killsWithoutDamageInRoom = 0;

        DailyMissionRuntime precisionMission = FindMission(DailyMissionType.Precision);
        if (precisionMission != null && !precisionMission.completed)
        {
            precisionMission.currentValue = 0;
            NotifyMissionStateChanged();
        }
    }

    private void OnEnemyDied(EnemyDiedEvent enemyDiedEvent)
    {
        DailyMissionRuntime resistanceMission = FindMission(DailyMissionType.Resistance);
        if (resistanceMission != null && !resistanceMission.completed)
        {
            resistanceMission.currentValue = Mathf.Min(resistanceMission.targetValue, resistanceMission.currentValue + 1);
            if (resistanceMission.currentValue >= resistanceMission.targetValue)
            {
                CompleteMission(resistanceMission);
            }
        }

        DailyMissionRuntime precisionMission = FindMission(DailyMissionType.Precision);
        if (precisionMission != null && !precisionMission.completed && !tookDamageInCurrentRoom)
        {
            killsWithoutDamageInRoom++;
            precisionMission.currentValue = Mathf.Clamp(killsWithoutDamageInRoom, 0, precisionMission.targetValue);
            if (precisionMission.currentValue >= precisionMission.targetValue)
            {
                CompleteMission(precisionMission);
            }
        }

        NotifyMissionStateChanged();
    }

    private void OnRoomCleared(RoomClearedEvent roomClearedEvent)
    {
        if (!usedItemSinceRoomEntry)
        {
            consecutiveRoomsWithoutItems++;
        }
        else
        {
            consecutiveRoomsWithoutItems = 0;
        }

        DailyMissionRuntime durezaMission = FindMission(DailyMissionType.Dureza);
        if (durezaMission != null && !durezaMission.completed)
        {
            durezaMission.currentValue = Mathf.Clamp(consecutiveRoomsWithoutItems, 0, durezaMission.targetValue);
            if (durezaMission.currentValue >= durezaMission.targetValue)
            {
                CompleteMission(durezaMission);
            }
        }

        UpdateExplorationMission();
        TryActivatePenaltyIfNeeded();
        NotifyMissionStateChanged();
    }

    private void UpdateExplorationMission()
    {
        DailyMissionRuntime explorationMission = FindMission(DailyMissionType.Exploration);
        if (explorationMission == null || explorationMission.completed || dungeonBuilder == null)
        {
            return;
        }

        explorationMission.targetValue = Mathf.Max(1, dungeonBuilder.GetTotalRoomCount());
        explorationMission.currentValue = Mathf.Clamp(dungeonBuilder.GetVisitedRoomCount(), 0, explorationMission.targetValue);
        if (explorationMission.currentValue >= explorationMission.targetValue)
        {
            CompleteMission(explorationMission);
        }
    }

    private void TryActivatePenaltyIfNeeded()
    {
        if (penaltyTriggered || completedMissionCount > 0 || dungeonBuilder == null)
        {
            return;
        }

        if (!dungeonBuilder.AreAllCombatRoomsCleared())
        {
            return;
        }

        penaltyTriggered = true;
        EventBus.Publish(new PenaltyActivatedEvent(30f, 0.5f, 0.8f));
        if (NotificationSystem.Instance != null)
        {
            NotificationSystem.Instance.ShowNotification("PENALIZACION POR INCUMPLIMIENTO", new Color(0.92f, 0.15f, 0.15f, 1f), 2.4f);
        }
    }

    private void CompleteMission(DailyMissionRuntime mission)
    {
        if (mission == null || mission.completed)
        {
            return;
        }

        mission.completed = true;
        mission.currentValue = mission.targetValue;
        completedMissionCount++;
        ApplyMissionReward(mission);

        if (NotificationSystem.Instance != null)
        {
            NotificationSystem.Instance.ShowNotification("MISION COMPLETADA: " + mission.displayName, new Color(0f, 0.75f, 1f, 1f), 2f);
        }

        NotifyMissionStateChanged();
    }

    private void ApplyMissionReward(DailyMissionRuntime mission)
    {
        if (systemManager == null)
        {
            return;
        }

        if (mission.missionType == DailyMissionType.Resistance)
        {
            systemManager.AddGold(10f);
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowNotification("ORO +10", new Color(0.98f, 0.84f, 0.15f, 1f), 1.6f);
            }
            return;
        }

        if (mission.missionType == DailyMissionType.Precision)
        {
            bool revealed = dungeonBuilder != null && dungeonBuilder.TryPromoteNormalRoomToSecret();
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowNotification(revealed ? "SALA SECRETA REVELADA" : "RECOMPENSA DE SECRETO PREPARADA", new Color(0.15f, 0.55f, 0.95f, 1f), 2f);
            }
            return;
        }

        if (mission.missionType == DailyMissionType.Exploration)
        {
            systemManager.ApplyFloorDamageBonus(0.05f);
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowNotification("DANO +5% POR ESTE PISO", new Color(0f, 0.75f, 1f, 1f), 2f);
            }
            return;
        }

        string boostedStat = systemManager.BoostLowestStat(2f);
        if (NotificationSystem.Instance != null)
        {
            NotificationSystem.Instance.ShowNotification(boostedStat + " +2", new Color(0.45f, 1f, 0.45f, 1f), 1.8f);
        }
    }

    private DailyMissionRuntime FindMission(DailyMissionType missionType)
    {
        for (int i = 0; i < activeMissions.Count; i++)
        {
            if (activeMissions[i].missionType == missionType)
            {
                return activeMissions[i];
            }
        }

        return null;
    }

    private void NotifyMissionStateChanged()
    {
        if (MissionsChanged != null)
        {
            MissionsChanged.Invoke();
        }
    }
}

