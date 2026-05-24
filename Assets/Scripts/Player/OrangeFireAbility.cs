using UnityEngine;

[DisallowMultipleComponent]
public class OrangeFireAbility : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private float cooldown = 1.15f;
    [SerializeField] private float damageMultiplier = 1.45f;
    [SerializeField] private float damageBonus = 8f;
    [SerializeField] private float projectileSpeed = 8.5f;
    [SerializeField] private float projectileRange = 8.5f;
    [SerializeField] private float explosionRadius = 1.85f;

    [Header("Spawn")]
    [SerializeField] private float spawnDistance = 0.75f;
    [SerializeField] private float spawnHeightOffset = 0.14f;
    [SerializeField] private float attackAnimationDuration = 0.28f;

    private KaisenController controller;
    private float nextFireTime;

    private void Awake()
    {
        controller = GetComponent<KaisenController>();
    }

    public bool TryFire(Vector2 direction)
    {
        if (Time.time < nextFireTime)
        {
            return false;
        }

        Vector2 finalDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        nextFireTime = Time.time + Mathf.Max(0.05f, cooldown);

        float resolvedDamage = controller != null
            ? (controller.CurrentAttackDamage * damageMultiplier) + damageBonus
            : 18f;

        if (controller != null)
        {
            controller.PlayAttackAnimation(attackAnimationDuration);
        }

        Vector2 origin = (Vector2)transform.position + Vector2.up * spawnHeightOffset;
        GameObject projectileObject = new GameObject("OrangeFireballProjectile");
        projectileObject.transform.position = origin + finalDirection * spawnDistance;
        projectileObject.AddComponent<SpriteRenderer>();
        projectileObject.AddComponent<CircleCollider2D>();
        projectileObject.AddComponent<Rigidbody2D>();

        OrangeFireballProjectile projectile = projectileObject.AddComponent<OrangeFireballProjectile>();
        projectile.Fire(projectileObject.transform.position, finalDirection, resolvedDamage, projectileSpeed, projectileRange, explosionRadius);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCue(AudioCueId.CrystalFire, origin, 0.65f, 0.82f, true, 0.55f);
        }

        return true;
    }
}
