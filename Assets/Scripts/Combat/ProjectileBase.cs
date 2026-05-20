using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ProjectileBase : MonoBehaviour, IPoolable
{
    [SerializeField] private string poolKey = "EnemyProjectile";
    [SerializeField] private float damage = 5f;
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private bool isPiercing;
    [SerializeField] private bool isEnemyProjectile = true;
    [SerializeField] private float projectileRange = 8f;

    private float distanceTravelled;
    private Collider2D triggerCollider;
    private float remainingLifetime;

    private void OnEnable()
    {
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider2D>();
        }

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        remainingLifetime = lifetime;
        distanceTravelled = 0f;
    }

    private void Update()
    {
        Vector3 delta = transform.right * speed * Time.deltaTime;
        transform.position += delta;
        distanceTravelled += delta.magnitude;

        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f || distanceTravelled >= projectileRange)
        {
            ReturnToPool();
        }
    }

    public void OnSpawn(Vector2 position, Vector2 direction, float damageValue)
    {
        transform.position = position;
        Vector2 finalDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        transform.right = finalDirection;
        damage = damageValue;
        remainingLifetime = lifetime;
        distanceTravelled = 0f;

        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider2D>();
        }

        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }

        if (isEnemyProjectile && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCue(AudioCueId.EnemyProjectile, position, 0.5f, 1f, true, 0.8f);
        }
    }

    public void OnDespawn()
    {
        remainingLifetime = lifetime;
        distanceTravelled = 0f;
    }

    public void AssignPoolKey(string key)
    {
        poolKey = key;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActiveAndEnabled || other == null)
        {
            return;
        }

        if (other.CompareTag("Cover"))
        {
            if (isEnemyProjectile)
            {
                damage *= 0.3f;
            }

            CoverObject cover = other.GetComponentInParent<CoverObject>();
            if (cover != null)
            {
                cover.TakeDamage(damage, transform.right);
            }

            if (!isPiercing)
            {
                ReturnToPool();
            }
            return;
        }

        if (isEnemyProjectile && other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }

            if (!isPiercing)
            {
                ReturnToPool();
            }
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(AudioCueId.WallImpact, transform.position, 0.7f, 1f, true, 0.8f);
            }
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (PoolManager.Instance == null)
        {
            Destroy(gameObject);
            return;
        }

        PoolManager.Instance.ReturnToPool(poolKey, gameObject);
    }
}
