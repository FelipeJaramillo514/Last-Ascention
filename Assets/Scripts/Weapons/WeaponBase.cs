using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class WeaponBase : MonoBehaviour
{
    [SerializeField] private WeaponData data;
    [SerializeField] private int currentAmmo = -1;
    [SerializeField] private float lastFireTime = -999f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private SpriteRenderer weaponRenderer;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private LayerMask enemyLayer;

    private KaisenController ownerController;
    private WeaponManager ownerManager;

    public WeaponData Data { get { return data; } }
    public int CurrentAmmo { get { return currentAmmo; } }
    public Transform FirePoint { get { return firePoint; } }

    private void Awake()
    {
        ownerController = GetComponentInParent<KaisenController>();
        ownerManager = GetComponentInParent<WeaponManager>();
        if (weaponRenderer == null)
        {
            weaponRenderer = GetComponent<SpriteRenderer>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (firePoint == null)
        {
            Transform existing = transform.Find("FirePoint");
            if (existing != null)
            {
                firePoint = existing;
            }
            else
            {
                GameObject firePointObject = new GameObject("FirePoint");
                firePointObject.transform.SetParent(transform, false);
                firePointObject.transform.localPosition = new Vector3(0.6f, 0f, 0f);
                firePoint = firePointObject.transform;
            }
        }

        if (enemyLayer.value == 0)
        {
            enemyLayer = LayerMask.GetMask("Enemy");
        }
    }

    public void SetWeaponData(WeaponData weaponData, int ammoOverride = int.MinValue)
    {
        data = weaponData;
        if (data == null)
        {
            currentAmmo = -1;
            if (weaponRenderer != null)
            {
                weaponRenderer.sprite = null;
                weaponRenderer.enabled = false;
            }
            return;
        }

        currentAmmo = ammoOverride != int.MinValue ? ammoOverride : GetInitialAmmo();
        if (weaponRenderer != null)
        {
            Sprite weaponSprite = WeaponVisualResolver.GetWeaponIcon(data);
            if (weaponSprite == null && data.animationFrames != null && data.animationFrames.Length > 0)
            {
                weaponSprite = data.animationFrames[0];
            }

            weaponRenderer.sprite = weaponSprite;
            weaponRenderer.enabled = weaponSprite != null;
        }
    }

    public void SetAimDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public bool TryFire(Vector2 direction)
    {
        if (data == null)
        {
            return false;
        }

        float fireInterval = data.fireRate > 0f ? 1f / data.fireRate : 0f;
        if (Time.time - lastFireTime < fireInterval)
        {
            return false;
        }

        if (currentAmmo == 0)
        {
            return false;
        }

        Vector2 finalDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        SetAimDirection(finalDirection);

        bool fired = false;
        if (data.weaponType == WeaponType.Melee)
        {
            fired = ExecuteMeleeAttack(finalDirection);
        }
        else if (data.weaponType == WeaponType.Ranged)
        {
            fired = ExecuteProjectileFire(finalDirection);
        }
        else if (data.weaponType == WeaponType.Hybrid)
        {
            bool meleeFired = ExecuteMeleeAttack(finalDirection);
            bool projectileFired = ExecuteProjectileFire(finalDirection);
            fired = meleeFired || projectileFired;
        }

        if (!fired)
        {
            return false;
        }

        lastFireTime = Time.time;
        if (data.maxAmmo >= 0)
        {
            currentAmmo = Mathf.Max(0, currentAmmo - 1);
        }

        if (audioSource != null && data.fireSound != null)
        {
            audioSource.PlayOneShot(data.fireSound);
        }

        EventBus.Publish(new WeaponFiredEvent(data, currentAmmo, ownerManager != null ? ownerManager.ActiveSlot : 0));
        return true;
    }

    private bool ExecuteMeleeAttack(Vector2 direction)
    {
        if (ownerController == null)
        {
            ownerController = GetComponentInParent<KaisenController>();
            if (ownerController == null)
            {
                return false;
            }
        }

        Vector2 origin = ownerController.transform.position;
        Vector2 attackCenter = origin + (direction.normalized * Mathf.Max(0.1f, data.meleeDistance));
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, Mathf.Max(0.1f, data.meleeRadius), enemyLayer);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        HashSet<EnemyBase> hitEnemies = new HashSet<EnemyBase>();
        bool hitSomething = false;
        float allowedHalfArc = Mathf.Max(0f, data.meleeArc * 0.5f);
        for (int i = 0; i < hits.Length; i++)
        {
            EnemyBase enemy = hits[i] != null ? hits[i].GetComponentInParent<EnemyBase>() : null;
            if (enemy == null || !hitEnemies.Add(enemy))
            {
                continue;
            }

            Vector2 toEnemy = (Vector2)enemy.transform.position - origin;
            if (toEnemy.sqrMagnitude <= 0.001f)
            {
                continue;
            }

            if (allowedHalfArc > 0f && allowedHalfArc < 179.9f && Vector2.Angle(direction, toEnemy.normalized) > allowedHalfArc)
            {
                continue;
            }

            enemy.TakeDamage(GetDamage(), direction);
            if (data.knockbackForce > 0f)
            {
                enemy.ApplyKnockback(direction, data.knockbackForce);
            }
            hitSomething = true;
        }

        return hitSomething;
    }

    private bool ExecuteProjectileFire(Vector2 direction)
    {
        if (PoolManager.Instance == null || data == null || data.projectilePrefab == null)
        {
            return false;
        }

        float[] spreadAngles = data.spreadAngles != null && data.spreadAngles.Length > 0 ? data.spreadAngles : null;
        if (spreadAngles == null)
        {
            return SpawnSingleProjectile(direction);
        }

        bool spawnedAny = false;
        for (int i = 0; i < spreadAngles.Length; i++)
        {
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, spreadAngles[i]) * direction;
            spawnedAny |= SpawnSingleProjectile(shotDirection);
        }

        return spawnedAny;
    }

    private bool SpawnSingleProjectile(Vector2 direction)
    {
        GameObject projectileObject = PoolManager.Instance.GetFromPool("PlayerProjectile", data.projectilePrefab);
        if (projectileObject == null)
        {
            return false;
        }

        PlayerProjectile projectile = projectileObject.GetComponent<PlayerProjectile>();
        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<PlayerProjectile>();
        }

        projectile.Configure(data, GetDamage());

        IPoolable poolable = projectileObject.GetComponent<IPoolable>();
        Vector2 spawnPosition = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        if (poolable != null)
        {
            poolable.OnSpawn(spawnPosition, direction, GetDamage());
        }
        else
        {
            projectileObject.transform.position = spawnPosition;
            projectileObject.transform.right = direction;
        }

        return true;
    }

    private int GetInitialAmmo()
    {
        return data != null && data.maxAmmo >= 0 ? data.maxAmmo : -1;
    }

    private float GetDamage()
    {
        if (data == null)
        {
            return 0f;
        }

        float resolvedDamage = data.damage > 0f ? data.damage : (ownerController != null ? ownerController.CurrentAttackDamage : 0f);
        if (ownerController != null
            && ownerController.Stats != null
            && data.weaponType == WeaponType.Melee
            && !string.IsNullOrWhiteSpace(data.weaponName)
            && data.weaponName.Contains("Espada"))
        {
            resolvedDamage *= 1f + Mathf.Max(0f, ownerController.Stats.swordDamageBonusPercent);
        }

        return resolvedDamage;
    }
}
