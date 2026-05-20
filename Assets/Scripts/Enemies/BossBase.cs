using System.Collections;
using UnityEngine;

public abstract class BossBase : EnemyBase
{
    [SerializeField, Range(0.1f, 0.95f)] private float phase2HPThreshold = 0.5f;
    [SerializeField] private float deathDespawnDelay = 3f;

    private bool isInPhase2;
    private bool isPhaseTransitioning;

    public bool IsInPhase2 => isInPhase2;
    public bool IsPhaseTransitioning => isPhaseTransitioning;
    public float HealthNormalized => MaxHP > 0f ? CurrentHP / MaxHP : 0f;
    public string BossName => Data != null && !string.IsNullOrWhiteSpace(Data.enemyName) ? Data.enemyName : name;

    protected override void Update()
    {
        base.Update();

        if (IsDead || isInPhase2 || isPhaseTransitioning || MaxHP <= 0f)
        {
            return;
        }

        if (CurrentHP <= MaxHP * phase2HPThreshold)
        {
            StartCoroutine(TriggerPhase2Routine());
        }
    }

    private IEnumerator TriggerPhase2Routine()
    {
        if (isInPhase2 || isPhaseTransitioning)
        {
            yield break;
        }

        isPhaseTransitioning = true;
        isInPhase2 = true;
        PauseBehaviour(1.5f);
        Time.timeScale = 0.3f;
        PlayAnimation("Roar");
        yield return new WaitForSecondsRealtime(1.5f);
        isPhaseTransitioning = false;
        Time.timeScale = 1f;
        EventBus.Publish(new BossPhase2Event(this));
        OnPhase2Triggered();
    }

    protected virtual void OnPhase2Triggered()
    {
    }

    protected override float GetDeathDespawnDelay()
    {
        return deathDespawnDelay;
    }

    protected override void SpawnDrops()
    {
        if (Data == null || Data.dropTable == null)
        {
            return;
        }

        for (int i = 0; i < Data.dropTable.Count; i++)
        {
            DropEntry drop = Data.dropTable[i];
            if (drop == null || drop.prefab == null)
            {
                continue;
            }

            if (LootSpawner.Instance != null)
            {
                LootSpawner.Instance.SpawnDrop(drop.prefab, transform.position);
            }
            else
            {
                Instantiate(drop.prefab, transform.position, Quaternion.identity);
            }
        }
    }
}
