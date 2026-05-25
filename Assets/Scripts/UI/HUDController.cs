using UnityEngine;
using UnityEngine.SceneManagement;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [SerializeField] private HealthPanelUI healthPanel;
    [SerializeField] private WeaponHudUI weaponPanel;
    [SerializeField] private MissionPanelUI missionPanel;
    [SerializeField] private ResourcePanelUI resourcePanel;
    [SerializeField] private ShadowArmyHUD shadowPanel;
    [SerializeField] private MinimapSystem minimapSystem;
    [SerializeField] private BossHPBar bossHpBar;

    private KaisenController playerController;
    private PlayerHealth playerHealth;
    private WeaponManager weaponManager;
    private SystemManager systemManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneBootstrap()
    {
        SceneManager.sceneLoaded -= OnRuntimeSceneLoaded;
        SceneManager.sceneLoaded += OnRuntimeSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureHudForScene(SceneManager.GetActiveScene());
    }

    private static void OnRuntimeSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureHudForScene(scene);
    }

    private static void EnsureHudForScene(Scene scene)
    {
        if (scene.name == "MainMenu")
        {
            return;
        }

        if (FindFirstObjectByType<HUDController>() != null)
        {
            return;
        }

        GameObject hudObject = new GameObject("HUDController");
        hudObject.AddComponent<HUDController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsurePanels();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
        EventBus.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Subscribe<WeaponFiredEvent>(OnWeaponFired);
        EventBus.Subscribe<ShadowDiedEvent>(OnShadowDied);
        EventBus.Subscribe<PenaltyActivatedEvent>(OnPenaltyActivated);
        EventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
        EventBus.Subscribe<WeaponEmptyEvent>(OnWeaponEmpty);
        EventBus.Subscribe<WeaponSwappedEvent>(OnWeaponSwapped);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
        EventBus.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Unsubscribe<WeaponFiredEvent>(OnWeaponFired);
        EventBus.Unsubscribe<ShadowDiedEvent>(OnShadowDied);
        EventBus.Unsubscribe<PenaltyActivatedEvent>(OnPenaltyActivated);
        EventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
        EventBus.Unsubscribe<WeaponEmptyEvent>(OnWeaponEmpty);
        EventBus.Unsubscribe<WeaponSwappedEvent>(OnWeaponSwapped);
    }

    private void Update()
    {
        ResolveReferences();
        if (healthPanel != null && playerHealth != null)
        {
            healthPanel.Refresh(playerHealth);
        }

        if (weaponPanel != null && weaponManager != null)
        {
            weaponPanel.Refresh(weaponManager, playerController);
        }

        if (resourcePanel != null && systemManager != null)
        {
            resourcePanel.Refresh(systemManager);
        }
    }

    private void OnPlayerDamaged(PlayerDamagedEvent damageEvent)
    {
        ResolveReferences();
        if (damageEvent == null)
        {
            return;
        }

        if (healthPanel != null)
        {
            healthPanel.TriggerDamageFeedback(damageEvent.currentHP);
            if (playerHealth != null)
            {
                healthPanel.Refresh(playerHealth);
            }
        }

        if (CameraShakeManager.Instance != null)
        {
            float magnitude = damageEvent.damage >= 20f ? CameraShakeManager.HitHeavy : CameraShakeManager.HitLight;
            CameraShakeManager.Instance.Shake(magnitude);
        }
    }

    private void OnPlayerLevelUp(PlayerLevelUpEvent levelUpEvent)
    {
        ResolveReferences();
        if (healthPanel != null && playerHealth != null)
        {
            healthPanel.Refresh(playerHealth);
        }
    }

    private void OnEnemyDied(EnemyDiedEvent enemyDiedEvent)
    {
        ResolveReferences();
        if (resourcePanel != null && systemManager != null)
        {
            resourcePanel.Refresh(systemManager);
        }
    }

    private void OnWeaponFired(WeaponFiredEvent weaponEvent)
    {
        if (weaponPanel != null)
        {
            weaponPanel.OnWeaponFired(weaponEvent);
        }
    }

    private void OnShadowDied(ShadowDiedEvent shadowEvent)
    {
    }

    private void OnPenaltyActivated(PenaltyActivatedEvent penaltyEvent)
    {
        if (healthPanel != null && penaltyEvent != null)
        {
            healthPanel.ActivatePenalty(penaltyEvent.duration);
        }
    }

    private void OnGoldChanged(GoldChangedEvent goldEvent)
    {
        if (resourcePanel != null && goldEvent != null)
        {
            resourcePanel.OnGoldChanged(goldEvent.currentGold);
        }
    }

    private void OnWeaponEmpty(WeaponEmptyEvent emptyEvent)
    {
        if (weaponPanel != null)
        {
            weaponPanel.OnWeaponEmpty();
        }
    }

    private void OnWeaponSwapped(WeaponSwappedEvent swappedEvent)
    {
        if (weaponPanel != null)
        {
            weaponPanel.OnWeaponSwapped();
        }
    }

    private void EnsurePanels()
    {
        healthPanel = healthPanel != null ? healthPanel : GetComponent<HealthPanelUI>();
        if (healthPanel == null)
        {
            healthPanel = gameObject.AddComponent<HealthPanelUI>();
        }

        weaponPanel = weaponPanel != null ? weaponPanel : FindFirstObjectByType<WeaponHudUI>();
        if (weaponPanel == null)
        {
            weaponPanel = gameObject.AddComponent<WeaponHudUI>();
        }

        missionPanel = missionPanel != null ? missionPanel : FindFirstObjectByType<MissionPanelUI>();
        if (missionPanel == null)
        {
            missionPanel = gameObject.AddComponent<MissionPanelUI>();
        }

        resourcePanel = resourcePanel != null ? resourcePanel : GetComponent<ResourcePanelUI>();
        if (resourcePanel == null)
        {
            resourcePanel = gameObject.AddComponent<ResourcePanelUI>();
        }

        shadowPanel = shadowPanel != null ? shadowPanel : FindFirstObjectByType<ShadowArmyHUD>();
        if (shadowPanel == null)
        {
            shadowPanel = gameObject.AddComponent<ShadowArmyHUD>();
        }

        minimapSystem = minimapSystem != null ? minimapSystem : FindFirstObjectByType<MinimapSystem>();
        if (minimapSystem == null)
        {
            minimapSystem = gameObject.AddComponent<MinimapSystem>();
        }

        bossHpBar = bossHpBar != null ? bossHpBar : FindFirstObjectByType<BossHPBar>();
        if (bossHpBar == null)
        {
            bossHpBar = gameObject.AddComponent<BossHPBar>();
        }
    }

    private void ResolveReferences()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<KaisenController>();
        }

        if (playerHealth == null && playerController != null)
        {
            playerHealth = playerController.GetComponent<PlayerHealth>();
        }

        if (weaponManager == null && playerController != null)
        {
            weaponManager = playerController.GetComponent<WeaponManager>();
        }

        if (systemManager == null)
        {
            systemManager = FindFirstObjectByType<SystemManager>();
        }
    }
}
