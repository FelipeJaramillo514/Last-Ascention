using System;
using UnityEngine;

[DisallowMultipleComponent]
public class KnifeAuraAttack : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private float cooldown = 0.32f;
    [SerializeField] private float damageMultiplier = 1.15f;
    [SerializeField] private float damageBonus = 3f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float projectileRange = 7.5f;
    [SerializeField] private int maxPierceTargets = 2;
    [SerializeField] private float projectileColliderRadius = 0.22f;

    [Header("Spawn")]
    [SerializeField] private float auraVisualScale = 0.85f;
    [SerializeField] private float spawnDistance = 0.65f;
    [SerializeField] private float spawnHeightOffset = 0.12f;
    [SerializeField] private float trailDistance = 0.28f;
    [SerializeField] private float trailScale = 1.05f;

    [Header("Animation")]
    [SerializeField] private float attackAnimationDuration = 0.24f;
    [SerializeField] private float projectileFrameRate = 14f;
    [SerializeField] private float effectFrameRate = 18f;
    [SerializeField] private Sprite[] projectileFrames;
    [SerializeField] private Sprite[] impactFrames;
    [SerializeField] private Sprite[] trailFrames;

    private KaisenController controller;
    private float nextFireTime;

    private void Awake()
    {
        controller = GetComponent<KaisenController>();
        LoadSpritesIfNeeded();
    }

    public bool TryFire(Vector2 direction)
    {
        LoadSpritesIfNeeded();
        if (Time.time < nextFireTime || projectileFrames == null || projectileFrames.Length == 0)
        {
            return false;
        }

        Vector2 finalDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        nextFireTime = Time.time + Mathf.Max(0.01f, cooldown);

        float resolvedDamage = controller != null
            ? (controller.CurrentAttackDamage * damageMultiplier) + damageBonus
            : 12f;

        if (controller != null)
        {
            controller.PlayAttackAnimation(attackAnimationDuration);
        }

        Vector2 origin = (Vector2)transform.position + Vector2.up * spawnHeightOffset;
        SpawnTrail(origin, finalDirection);
        SpawnProjectile(origin + (finalDirection * spawnDistance), finalDirection, resolvedDamage);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCue(AudioCueId.SwordSwing, origin, 0.65f, 1.25f, true, 0.65f);
            AudioManager.Instance.PlayCue(AudioCueId.CrystalFire, origin, 0.45f, 1.15f, true, 0.55f);
        }

        return true;
    }

    private void SpawnProjectile(Vector2 spawnPosition, Vector2 direction, float damage)
    {
        GameObject projectileObject = new GameObject("KnifeAuraProjectile");
        projectileObject.transform.position = spawnPosition;
        projectileObject.AddComponent<SpriteRenderer>();
        projectileObject.AddComponent<CircleCollider2D>();
        projectileObject.AddComponent<Rigidbody2D>();
        KnifeAuraProjectile projectile = projectileObject.AddComponent<KnifeAuraProjectile>();
        projectile.Configure(
            projectileFrames,
            impactFrames,
            projectileSpeed,
            projectileRange,
            projectileFrameRate,
            effectFrameRate,
            auraVisualScale,
            projectileColliderRadius,
            maxPierceTargets);
        projectile.Fire(spawnPosition, direction, damage);
    }

    private void SpawnTrail(Vector2 origin, Vector2 direction)
    {
        KnifeAuraSpriteEffect.Spawn(
            "KnifeAuraTrail",
            trailFrames,
            origin + (direction * trailDistance),
            direction,
            effectFrameRate,
            trailScale * auraVisualScale,
            "VFX",
            18);
    }

    private void LoadSpritesIfNeeded()
    {
        if (projectileFrames == null || projectileFrames.Length == 0)
        {
            projectileFrames = LoadOrderedSprites("KnifeAura/Projectile");
        }

        if (impactFrames == null || impactFrames.Length == 0)
        {
            impactFrames = LoadOrderedSprites("KnifeAura/Impact");
        }

        if (trailFrames == null || trailFrames.Length == 0)
        {
            trailFrames = LoadOrderedSprites("KnifeAura/Trail");
        }
    }

    private static Sprite[] LoadOrderedSprites(string resourcePath)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
        Array.Sort(sprites, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
        return sprites;
    }
}
