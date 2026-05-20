using UnityEngine;

public class GoblinEnemy : EnemyBase
{
    [SerializeField] private float meleeHitRadius = 0.8f;
    [SerializeField] private float dodgeConfusionDuration = 0.3f;

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerDodgedEvent>(OnPlayerDodged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerDodgedEvent>(OnPlayerDodged);
    }

    protected override void PerformAttack()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCue(AudioCueId.GoblinAttack, transform.position, 0.85f, 1f, true, 0.75f);
        }

        Vector2 attackOrigin = GetAttackOrigin(meleeHitRadius);
        Collider2D hit = Physics2D.OverlapCircle(attackOrigin, meleeHitRadius, PlayerLayerMask);
        if (hit == null)
        {
            return;
        }

        PlayerHealth target = hit.GetComponentInParent<PlayerHealth>();
        if (target == null)
        {
            return;
        }

        target.TakeDamage(Data != null ? Data.damage : 0f);
    }

    private void OnPlayerDodged(PlayerDodgedEvent dodgeEvent)
    {
        if (CurrentState == EnemyState.Dead || Data == null)
        {
            return;
        }

        if (Vector2.Distance(transform.position, dodgeEvent.position) <= Data.detectionRange)
        {
            PauseBehaviour(dodgeConfusionDuration);
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(GetAttackOrigin(meleeHitRadius), meleeHitRadius);
    }
}
