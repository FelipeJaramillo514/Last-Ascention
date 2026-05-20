using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayFeedbackController : MonoBehaviour
{
    public static GameplayFeedbackController Instance { get; private set; }

    [SerializeField] private float footstepInterval = 0.3f;

    private KaisenController player;
    private float nextFootstepTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<GameplayFeedbackController>() != null)
        {
            return;
        }

        GameObject controllerObject = new GameObject("GameplayFeedbackController");
        controllerObject.AddComponent<GameplayFeedbackController>();
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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
        EventBus.Subscribe<PlayerDeathEvent>(OnPlayerDeath);
        EventBus.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        EventBus.Subscribe<WeaponFiredEvent>(OnWeaponFired);
        EventBus.Subscribe<PenaltyActivatedEvent>(OnPenaltyActivated);
        EventBus.Subscribe<BossPhase2Event>(OnBossPhase2);
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Subscribe<PlayerDodgedEvent>(OnPlayerDodged);
        EventBus.Subscribe<WeaponEmptyEvent>(OnWeaponEmpty);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
        EventBus.Unsubscribe<PlayerDeathEvent>(OnPlayerDeath);
        EventBus.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        EventBus.Unsubscribe<WeaponFiredEvent>(OnWeaponFired);
        EventBus.Unsubscribe<PenaltyActivatedEvent>(OnPenaltyActivated);
        EventBus.Unsubscribe<BossPhase2Event>(OnBossPhase2);
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Unsubscribe<PlayerDodgedEvent>(OnPlayerDodged);
        EventBus.Unsubscribe<WeaponEmptyEvent>(OnWeaponEmpty);
    }

    private void Update()
    {
        if (AudioManager.Instance == null || SceneManager.GetActiveScene().name == "MainMenu")
        {
            return;
        }

        if (player == null)
        {
            player = FindFirstObjectByType<KaisenController>();
        }

        if (player == null || !player.gameObject.activeInHierarchy || Time.timeScale <= 0.01f)
        {
            return;
        }

        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        Vector2 velocity = playerBody != null ? playerBody.linearVelocity : Vector2.zero;
        if (velocity.magnitude < 0.15f || Time.time < nextFootstepTime)
        {
            return;
        }

        nextFootstepTime = Time.time + footstepInterval;
        AudioCueId footstepCue = (AudioCueId)((int)AudioCueId.FootstepA + Random.Range(0, 4));
        AudioManager.Instance.PlayCue(footstepCue, player.transform.position, 0.45f, 1f, true, 0.7f);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        player = FindFirstObjectByType<KaisenController>();
        nextFootstepTime = Time.time;
    }

    private void OnPlayerDamaged(PlayerDamagedEvent playerDamagedEvent)
    {
        if (AudioManager.Instance == null || player == null)
        {
            return;
        }

        AudioManager.Instance.PlayCue(AudioCueId.PlayerHit, player.transform.position, 0.9f, 1f, true, 0.7f);
    }

    private void OnPlayerDeath(PlayerDeathEvent playerDeathEvent)
    {
        if (AudioManager.Instance == null || player == null)
        {
            return;
        }

        AudioManager.Instance.PlayCue(AudioCueId.PlayerDeath, player.transform.position, 1f, 1f, true, 0.75f);
    }

    private void OnPlayerLevelUp(PlayerLevelUpEvent playerLevelUpEvent)
    {
        if (AudioManager.Instance == null || player == null)
        {
            return;
        }

        AudioManager.Instance.PlayCue(AudioCueId.LevelUp, player.transform.position, 0.9f, 1f, false, 0f);
    }

    private void OnWeaponFired(WeaponFiredEvent weaponFiredEvent)
    {
        if (AudioManager.Instance == null || player == null || weaponFiredEvent == null || weaponFiredEvent.weaponData == null)
        {
            return;
        }

        AudioCueId cueId = weaponFiredEvent.weaponData.weaponType == WeaponType.Melee ? AudioCueId.SwordSwing : AudioCueId.CrystalFire;
        AudioManager.Instance.PlayCue(cueId, player.transform.position, 0.8f, 1f, true, 0.5f);
    }

    private void OnPenaltyActivated(PenaltyActivatedEvent penaltyActivatedEvent)
    {
        if (AudioManager.Instance == null || player == null)
        {
            return;
        }

        AudioManager.Instance.PlayCue(AudioCueId.Penalty, player.transform.position, 0.95f, 1f, false, 0f);
    }

    private void OnBossPhase2(BossPhase2Event bossPhase2Event)
    {
        if (AudioManager.Instance == null || bossPhase2Event == null || bossPhase2Event.boss == null)
        {
            return;
        }

        AudioManager.Instance.PlayCue(AudioCueId.BossRoar, bossPhase2Event.boss.transform.position, 1f, 1f, true, 0.8f);
    }

    private void OnPlayerDodged(PlayerDodgedEvent playerDodgedEvent)
    {
        if (AudioManager.Instance == null || playerDodgedEvent == null)
        {
            return;
        }

        AudioManager.Instance.PlayCue(AudioCueId.DodgeRoll, playerDodgedEvent.position, 0.75f, 1f, true, 0.7f);
    }

    private void OnEnemyDied(EnemyDiedEvent enemyDiedEvent)
    {
        if (AudioManager.Instance == null || enemyDiedEvent == null || enemyDiedEvent.enemy == null)
        {
            return;
        }

        EnemyBase enemy = enemyDiedEvent.enemy.GetComponent<EnemyBase>();
        if (enemy == null || enemy.Data == null)
        {
            return;
        }

        AudioCueId cueId = AudioCueId.GiantImpact;
        if (enemy.Data.enemyType == EnemyType.Goblin)
        {
            cueId = AudioCueId.GoblinDeath;
        }

        AudioManager.Instance.PlayCue(cueId, enemyDiedEvent.position, 0.9f, 1f, true, 0.75f);
    }

    private void OnWeaponEmpty(WeaponEmptyEvent weaponEmptyEvent)
    {
        if (AudioManager.Instance == null || player == null)
        {
            return;
        }

        AudioManager.Instance.PlayCue(AudioCueId.WallImpact, player.transform.position, 0.18f, 1.5f, false, 0f);
    }
}
