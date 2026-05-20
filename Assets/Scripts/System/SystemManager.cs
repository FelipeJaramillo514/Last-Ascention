using UnityEngine;

public class SystemManager : MonoBehaviour
{
    public static SystemManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private KaisenController playerController;
    [SerializeField] private SystemPanelUI systemPanelUI;

    [Header("Progression")]
    [SerializeField] private float currentEXP;
    [SerializeField] private float expToNextLevel = 100f;

    private KaisenStats stats;
    private StatChanges lastStatChanges;

    public KaisenStats Stats { get { return stats; } }
    public float CurrentEXP { get { return currentEXP; } }
    public float ExpToNextLevel { get { return expToNextLevel; } }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<KaisenController>();
        }

        if (systemPanelUI == null)
        {
            systemPanelUI = FindFirstObjectByType<SystemPanelUI>();
        }

        stats = playerController != null ? playerController.Stats : null;
        ConfigureForCurrentStats(true);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    public float GetPerceptionMultiplier()
    {
        return stats == null ? 1f : 1f + (stats.EffectivePerception * 0.005f);
    }

    public void RebindPlayer(KaisenController controller, bool resetExp)
    {
        playerController = controller != null ? controller : FindFirstObjectByType<KaisenController>();
        stats = playerController != null ? playerController.Stats : null;
        ConfigureForCurrentStats(resetExp);
    }

    public StatChanges GetStatChanges()
    {
        return lastStatChanges;
    }

    public void AddExperience(float amount)
    {
        if (stats == null || amount <= 0f)
        {
            return;
        }

        currentEXP += amount;
        while (currentEXP >= expToNextLevel && expToNextLevel > 0f)
        {
            LevelUp();
        }
    }

    public void AddGold(float amount)
    {
        if (stats == null || amount <= 0f)
        {
            return;
        }

        amount *= Mathf.Max(0.1f, GameBalanceData.Instance.goldRewardMultiplier);
        stats.currentGold += amount;
        EventBus.Publish(new GoldChangedEvent(stats.currentGold, amount));
    }

    public void ApplyFloorDamageBonus(float percentageBonus)
    {
        if (playerController == null || percentageBonus <= 0f)
        {
            return;
        }

        playerController.AddAttackDamageBonusMultiplier(percentageBonus);
    }

    public string BoostLowestStat(float amount)
    {
        if (stats == null || amount <= 0f)
        {
            return string.Empty;
        }

        float minValue = Mathf.Min(stats.strength, stats.agility, stats.resistance, stats.perception);
        if (Mathf.Approximately(stats.strength, minValue))
        {
            stats.strength += amount;
            return "Fuerza";
        }

        if (Mathf.Approximately(stats.agility, minValue))
        {
            stats.agility += amount;
            return "Agilidad";
        }

        if (Mathf.Approximately(stats.resistance, minValue))
        {
            stats.resistance += amount;
            return "Resistencia";
        }

        stats.perception += amount;
        return "Percepcion";
    }

    private void OnEnemyDied(EnemyDiedEvent enemyDiedEvent)
    {
        if (stats == null || enemyDiedEvent == null)
        {
            return;
        }

        AddExperience(enemyDiedEvent.expValue * GetPerceptionMultiplier());
    }

    private void LevelUp()
    {
        if (stats == null)
        {
            return;
        }

        int previousLevel = stats.systemLevel;
        stats.systemLevel++;

        StatChanges changes = new StatChanges();
        changes.previousLevel = previousLevel;
        changes.newLevel = stats.systemLevel;

        if (stats.systemLevel % 2 == 0)
        {
            stats.strength += 2f;
            stats.agility += 1f;
            changes.strengthDelta = 2f;
            changes.agilityDelta = 1f;
        }
        else
        {
            stats.resistance += 1f;
            stats.perception += 2f;
            changes.resistanceDelta = 1f;
            changes.perceptionDelta = 2f;
        }

        if (stats.systemLevel % 5 == 0)
        {
            stats.skillPoints += 1;
            changes.skillPointsDelta = 1;
        }

        changes.currentStrength = stats.strength;
        changes.currentAgility = stats.agility;
        changes.currentResistance = stats.resistance;
        changes.currentPerception = stats.perception;
        changes.currentSkillPoints = stats.skillPoints;

        expToNextLevel *= Mathf.Max(1.01f, GameBalanceData.Instance.expScalePerLevel);
        currentEXP = 0f;
        lastStatChanges = changes;
        if (PersistentData.Instance != null)
        {
            PersistentData.Instance.ApplyLevelUp(changes);
        }

        EventBus.Publish(new PlayerLevelUpEvent(previousLevel, stats.systemLevel, changes));


        if (NotificationSystem.Instance != null)
        {
            NotificationSystem.Instance.ShowNotification("NIVEL " + stats.systemLevel + " ALCANZADO", new Color(0f, 0.75f, 1f, 1f), 1.8f);
            if (changes.skillPointsDelta > 0)
            {
                NotificationSystem.Instance.ShowNotification("PUNTO DE HABILIDAD +1", new Color(1f, 0.84f, 0f, 1f), 2f);
            }
        }
    }

    public void DebugForceLevelUp()
    {
        AddExperience(expToNextLevel);
    }

    private void ConfigureForCurrentStats(bool resetExp)
    {
        if (stats == null)
        {
            return;
        }

        expToNextLevel = CalculateExpToNextLevel(stats.systemLevel);
        if (resetExp)
        {
            currentEXP = 0f;
        }
    }

    public static float CalculateExpToNextLevel(int level)
    {
        GameBalanceData balance = GameBalanceData.Instance;
        float required = Mathf.Max(1f, balance.baseEXPToLevel);
        for (int i = 1; i < Mathf.Max(1, level); i++)
        {
            required *= Mathf.Max(1.01f, balance.expScalePerLevel);
        }

        return required;
    }
}
