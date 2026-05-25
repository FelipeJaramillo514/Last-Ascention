using UnityEngine;

public class ExperienceDropManager : MonoBehaviour
{
    public static ExperienceDropManager Instance { get; private set; }

    [SerializeField] private bool spawnExperienceOrbs = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<ExperienceDropManager>() != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("ExperienceDropManager");
        managerObject.AddComponent<ExperienceDropManager>();
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
        if (Instance != this)
        {
            return;
        }

        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    private void OnDisable()
    {
        if (Instance != this)
        {
            return;
        }

        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    private void OnEnemyDied(EnemyDiedEvent enemyDiedEvent)
    {
        if (!spawnExperienceOrbs || enemyDiedEvent == null || enemyDiedEvent.expValue <= 0)
        {
            return;
        }

        ExperienceOrbPickup.SpawnCluster(enemyDiedEvent.position, enemyDiedEvent.expValue);
    }
}
