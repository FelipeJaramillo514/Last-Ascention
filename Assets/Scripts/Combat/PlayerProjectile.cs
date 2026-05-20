using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerProjectile : MonoBehaviour, IPoolable
{
    [SerializeField] private string poolKey = "PlayerProjectile";
    [SerializeField] private float damage = 10f;
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float range = 8f;
    [SerializeField] private bool isPiercing;
    [SerializeField] private bool isExplosive;
    [SerializeField] private float explosionRadius = 1.5f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private float remainingLifetime;
    private float distanceTravelled;

    private void OnEnable()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        remainingLifetime = lifetime;
        distanceTravelled = 0f;
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += transform.right * step;
        distanceTravelled += step;
        remainingLifetime -= Time.deltaTime;

        if (remainingLifetime <= 0f || distanceTravelled >= Mathf.Max(0.1f, range))
        {
            ReturnToPool();
        }
    }

    public void Configure(WeaponData weaponData, float damageOverride)
    {
        if (weaponData == null)
        {
            return;
        }

        damage = damageOverride > 0f ? damageOverride : weaponData.damage;
        speed = weaponData.projectileSpeed > 0f ? weaponData.projectileSpeed : speed;
        range = weaponData.projectileRange > 0f ? weaponData.projectileRange : range;
        isPiercing = weaponData.isPiercing;
        isExplosive = weaponData.isExplosive;

        if (spriteRenderer != null)
        {
            Sprite projectileSprite = null;
            if (weaponData.projectilePrefab != null)
            {
                SpriteRenderer projectileRenderer = weaponData.projectilePrefab.GetComponentInChildren<SpriteRenderer>();
                if (projectileRenderer != null)
                {
                    projectileSprite = projectileRenderer.sprite;
                }
            }

            if (projectileSprite == null && weaponData.animationFrames != null && weaponData.animationFrames.Length > 0)
            {
                projectileSprite = weaponData.animationFrames[0];
            }

            if (projectileSprite == null)
            {
                projectileSprite = weaponData.weaponIcon;
            }

            spriteRenderer.sprite = projectileSprite;
        }
    }

    public void OnSpawn(Vector2 position, Vector2 direction, float damageValue)
    {
        transform.position = position;
        Vector2 finalDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        transform.right = finalDirection;
        if (damageValue > 0f)
        {
            damage = damageValue;
        }
        remainingLifetime = lifetime;
        distanceTravelled = 0f;
    }

    public void OnDespawn()
    {
        remainingLifetime = lifetime;
        distanceTravelled = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(AudioCueId.WallImpact, transform.position, 0.7f, 1f, true, 0.75f);
            }
            if (isExplosive)
            {
                Explode();
            }
            ReturnToPool();
            return;
        }

        if (other.CompareTag("Cover"))
        {
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

        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage, transform.right);
            }

            if (isExplosive)
            {
                Explode();
            }

            if (!isPiercing)
            {
                ReturnToPool();
            }
        }
    }

    private void Explode()
    {
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayExplosion(transform.position);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            EnemyBase enemy = hits[i] != null ? hits[i].GetComponentInParent<EnemyBase>() : null;
            if (enemy != null)
            {
                enemy.TakeDamage(damage, ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized);
            }
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
