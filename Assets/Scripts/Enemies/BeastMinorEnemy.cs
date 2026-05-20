using UnityEngine;

public class BeastMinorEnemy : EnemyBase
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private string projectilePoolKey = "EnemyProjectile";
    [SerializeField] private float optimalDistance = 4f;
    [SerializeField] private float projectileSpawnOffset = 0.6f;

    protected override Vector2 GetChaseDirection(Vector2 toPlayer, float distanceToPlayer)
    {
        if (distanceToPlayer < optimalDistance)
        {
            return -toPlayer.normalized;
        }

        return base.GetChaseDirection(toPlayer, distanceToPlayer);
    }

    protected override void PerformAttack()
    {
        if (PoolManager.Instance == null || projectilePrefab == null)
        {
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCue(AudioCueId.EnemyProjectile, transform.position, 0.75f, 1f, true, 0.75f);
        }

        Vector2 direction = GetDirectionToPlayer();
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = movementDirection.sqrMagnitude > 0.001f ? movementDirection.normalized : Vector2.right;
        }

        GameObject projectile = PoolManager.Instance.GetFromPool(projectilePoolKey, projectilePrefab);
        if (projectile == null)
        {
            return;
        }

        IPoolable poolable = projectile.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnSpawn((Vector2)transform.position + (direction * projectileSpawnOffset), direction, Data != null ? Data.damage : 0f);
        }
        else
        {
            projectile.transform.position = (Vector2)transform.position + (direction * projectileSpawnOffset);
            projectile.transform.right = direction;
        }
    }
}
