using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoneBearBoss : BossBase
{
    [Header("Movement")]
    [SerializeField] private float phase1MoveSpeed = 2.5f;
    [SerializeField] private float phase2MoveSpeed = 3.5f;

    [Header("Charge")]
    [SerializeField] private float chargeWindup = 2f;
    [SerializeField] private float chargeSpeed = 10f;
    [SerializeField] private float chargeDuration = 1.5f;
    [SerializeField] private float chargePlayerDamage = 30f;
    [SerializeField] private float wallStaggerDuration = 1.5f;

    [Header("Swipe")]
    [SerializeField] private float swipeWindup = 0.8f;
    [SerializeField] private float swipeRadius = 2.5f;
    [SerializeField] private float swipeDamage = 50f;
    [SerializeField] private float swipeKnockback = 5f;
    [SerializeField] private float swipeOffset = 1.4f;

    [Header("Ground Slam")]
    [SerializeField] private float groundSlamWindup = 1.5f;
    [SerializeField] private float groundSlamRadius = 5f;
    [SerializeField] private float groundSlamDamage = 40f;
    [SerializeField] private float groundSlamMinCooldown = 30f;

    [Header("Projectiles")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private string projectilePoolKey = "EnemyProjectile";
    [SerializeField] private float projectileSpawnOffset = 1f;
    [SerializeField] private float fragmentDamage = 18f;
    [SerializeField] private float spinProjectileDamage = 14f;

    [Header("Phase 2")]
    [SerializeField] private float spinCooldown = 20f;
    [SerializeField] private float spinDuration = 2f;

    private Coroutine attackRoutine;
    private bool isCharging;
    private bool chargeHitWall;
    private bool chargeDamagedPlayer;
    private Vector2 chargeDirection = Vector2.right;
    private float nextGroundSlamTime;
    private float nextSpinTime;

    protected override void Start()
    {
        base.Start();
        nextGroundSlamTime = Time.time + 6f;
        nextSpinTime = Time.time + spinCooldown;
    }

    protected override void FixedUpdate()
    {
        if (isCharging && Body != null)
        {
            Body.MovePosition(Body.position + chargeDirection * chargeSpeed * Time.fixedDeltaTime);
            return;
        }

        base.FixedUpdate();
    }

    protected override float GetChaseSpeed()
    {
        return IsInPhase2 ? phase2MoveSpeed : phase1MoveSpeed;
    }

    protected override void PerformAttack()
    {
        if (attackRoutine != null || IsPhaseTransitioning)
        {
            return;
        }

        if (IsInPhase2 && Time.time >= nextSpinTime)
        {
            attackRoutine = StartCoroutine(SpinRoutine());
            return;
        }

        float slamWeight = Time.time >= nextGroundSlamTime ? 20f : 0f;
        float roll = Random.value * (80f + slamWeight);
        if (roll < 40f)
        {
            attackRoutine = StartCoroutine(ChargeRoutine());
        }
        else if (roll < 80f)
        {
            attackRoutine = StartCoroutine(SwipeRoutine());
        }
        else
        {
            attackRoutine = StartCoroutine(GroundSlamRoutine());
        }
    }

    protected override void OnPhase2Triggered()
    {
        DestroyRandomColumn();
    }

    protected override void OnEnteredDeadState()
    {
        PersistentData.shadowExtractionUnlocked = true;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCue(AudioCueId.AbilityUnlocked, transform.position, 1f, 1f, false, 0f);
        }

        if (NotificationSystem.Instance != null)
        {
            NotificationSystem.Instance.ShowNotification("NUEVA HABILIDAD DESBLOQUEADA: EXTRACCION DE SOMBRA", new Color(0f, 0.9f, 1f, 1f), 4f);
        }

        if (!PersistentData.shadowUnlockAnnouncementShown)
        {
            PersistentData.shadowUnlockAnnouncementShown = true;
            if (SystemUnlockPanelUI.Instance != null)
            {
                SystemUnlockPanelUI.Instance.ShowUnlockMessage("HABILIDAD DESBLOQUEADA\nExtraccion de Sombra\nRango: Sin clasificacion conocida");
            }
        }
    }

    private IEnumerator ChargeRoutine()
    {
        PauseBehaviour(chargeWindup + chargeDuration + wallStaggerDuration + 0.25f);
        PlayAnimation("Roar");
        chargeHitWall = false;
        chargeDamagedPlayer = false;

        float elapsed = 0f;
        while (elapsed < chargeWindup)
        {
            Vector2 toPlayer = GetDirectionToPlayer();
            if (toPlayer.sqrMagnitude > 0.001f)
            {
                chargeDirection = toPlayer;
                movementDirection = toPlayer;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        isCharging = true;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCue(AudioCueId.BossCharge, transform.position, 1f, 1f, true, 0.9f);
        }

        elapsed = 0f;
        while (elapsed < chargeDuration && !chargeHitWall)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        isCharging = false;
        SetBodyVelocity(Vector2.zero);

        if (chargeHitWall)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(AudioCueId.GiantImpact, transform.position, 1f, 0.9f, true, 0.95f);
            }

            if (CameraShakeManager.Instance != null)
            {
                CameraShakeManager.Instance.Shake(CameraShakeManager.BossCharge);
            }

            if (IsInPhase2)
            {
                SpawnProjectileRing(8, 0f, fragmentDamage);
            }

            yield return new WaitForSeconds(wallStaggerDuration);
        }
        else
        {
            yield return new WaitForSeconds(0.25f);
        }

        attackRoutine = null;
    }

    private IEnumerator SwipeRoutine()
    {
        PauseBehaviour(swipeWindup + 0.35f);
        PlayAnimation("Attack");
        yield return new WaitForSeconds(swipeWindup);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCue(AudioCueId.GiantImpact, transform.position, 0.95f, 1f, true, 0.85f);
        }

        Vector2 origin = (Vector2)transform.position + GetFacingDirection() * swipeOffset;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, swipeRadius);
        HashSet<GameObject> damagedObjects = new HashSet<GameObject>();
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            GameObject root = hit.transform.root.gameObject;
            if (!damagedObjects.Add(root))
            {
                continue;
            }

            Vector2 knockbackDirection = ((Vector2)root.transform.position - (Vector2)transform.position).normalized;
            PlayerHealth player = hit.GetComponentInParent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(GetScaledBossDamage(swipeDamage));
                Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
                if (playerBody != null)
                {
                    playerBody.AddForce(knockbackDirection * swipeKnockback, ForceMode2D.Impulse);
                }
                continue;
            }

            ShadowSoldier shadow = hit.GetComponentInParent<ShadowSoldier>();
            if (shadow != null)
            {
                shadow.TakeDamage(GetScaledBossDamage(swipeDamage));
                Rigidbody2D shadowBody = shadow.GetComponent<Rigidbody2D>();
                if (shadowBody != null)
                {
                    shadowBody.AddForce(knockbackDirection * swipeKnockback, ForceMode2D.Impulse);
                }
            }
        }

        yield return new WaitForSeconds(0.15f);
        attackRoutine = null;
    }

    private IEnumerator GroundSlamRoutine()
    {
        nextGroundSlamTime = Time.time + groundSlamMinCooldown;
        PauseBehaviour(groundSlamWindup + 0.5f);
        GameObject telegraph = CreateGroundTelegraph(groundSlamRadius);
        yield return new WaitForSeconds(groundSlamWindup);

        if (telegraph != null)
        {
            Destroy(telegraph);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, groundSlamRadius);
        HashSet<GameObject> damagedObjects = new HashSet<GameObject>();
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            GameObject root = hit.transform.root.gameObject;
            if (!damagedObjects.Add(root))
            {
                continue;
            }

            PlayerHealth player = hit.GetComponentInParent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(GetScaledBossDamage(groundSlamDamage));
                continue;
            }

            ShadowSoldier shadow = hit.GetComponentInParent<ShadowSoldier>();
            if (shadow != null)
            {
                shadow.TakeDamage(GetScaledBossDamage(groundSlamDamage));
                continue;
            }

            CoverObject cover = hit.GetComponentInParent<CoverObject>();
            if (cover != null)
            {
                Vector2 hitDirection = ((Vector2)cover.transform.position - (Vector2)transform.position).normalized;
                cover.TakeDamage(GetScaledBossDamage(groundSlamDamage), hitDirection);
            }
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCue(AudioCueId.GiantImpact, transform.position, 1f, 0.95f, true, 0.95f);
        }

        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.Shake(CameraShakeManager.BossSlam);
        }

        if (IsInPhase2)
        {
            DestroyRandomColumn();
        }

        yield return new WaitForSeconds(0.2f);
        attackRoutine = null;
    }

    private IEnumerator SpinRoutine()
    {
        nextSpinTime = Time.time + spinCooldown;
        PauseBehaviour(spinDuration + 0.35f);
        PlayAnimation("Attack");

        int projectileCount = 8;
        float interval = spinDuration / projectileCount;
        float elapsed = 0f;
        int shotsFired = 0;
        while (elapsed < spinDuration && shotsFired < projectileCount)
        {
            elapsed += Time.deltaTime;
            transform.Rotate(0f, 0f, (360f / spinDuration) * Time.deltaTime);
            if (elapsed >= interval * shotsFired)
            {
                float angle = shotsFired * 45f;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                SpawnProjectile(direction, GetScaledBossDamage(spinProjectileDamage));
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayCue(AudioCueId.EnemyProjectile, transform.position, 0.7f, 1f, true, 0.75f);
                }
                shotsFired++;
            }

            yield return null;
        }

        attackRoutine = null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isCharging || collision == null || collision.collider == null)
        {
            return;
        }

        int wallLayer = LayerMask.NameToLayer("Wall");
        int playerLayer = LayerMask.NameToLayer("Player");
        if (collision.collider.gameObject.layer == wallLayer)
        {
            chargeHitWall = true;
            isCharging = false;
            return;
        }

        if (!chargeDamagedPlayer && collision.collider.gameObject.layer == playerLayer)
        {
            PlayerHealth player = collision.collider.GetComponentInParent<PlayerHealth>();
            if (player != null)
            {
                chargeDamagedPlayer = true;
                player.TakeDamage(GetScaledBossDamage(chargePlayerDamage));
            }
        }
    }

    private Vector2 GetFacingDirection()
    {
        if (movementDirection.sqrMagnitude > 0.001f)
        {
            return movementDirection.normalized;
        }

        Vector2 toPlayer = GetDirectionToPlayer();
        return toPlayer.sqrMagnitude > 0.001f ? toPlayer : Vector2.right;
    }

    private void SpawnProjectileRing(int count, float startAngle, float damageAmount)
    {
        if (count <= 0)
        {
            return;
        }

        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + step * i;
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            SpawnProjectile(direction, GetScaledBossDamage(damageAmount));
        }
    }

    private void SpawnProjectile(Vector2 direction, float damageAmount)
    {
        if (PoolManager.Instance == null || projectilePrefab == null)
        {
            return;
        }

        GameObject projectile = PoolManager.Instance.GetFromPool(projectilePoolKey, projectilePrefab);
        if (projectile == null)
        {
            return;
        }

        IPoolable poolable = projectile.GetComponent<IPoolable>();
        Vector2 spawnPosition = (Vector2)transform.position + direction.normalized * projectileSpawnOffset;
        if (poolable != null)
        {
            poolable.OnSpawn(spawnPosition, direction, damageAmount);
        }
        else
        {
            projectile.transform.position = spawnPosition;
            projectile.transform.right = direction;
        }
    }

    private GameObject CreateGroundTelegraph(float radius)
    {
        GameObject telegraph = new GameObject("GroundSlamTelegraph");
        telegraph.transform.position = transform.position + Vector3.forward * -0.1f;
        LineRenderer line = telegraph.AddComponent<LineRenderer>();
        line.loop = true;
        line.positionCount = 40;
        line.startWidth = 0.08f;
        line.endWidth = 0.08f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(1f, 0.1f, 0.1f, 0.8f);
        line.endColor = new Color(1f, 0.1f, 0.1f, 0.8f);
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i / (float)line.positionCount * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }

        return telegraph;
    }

    private void DestroyRandomColumn()
    {
        DungeonRoom room = GetComponentInParent<DungeonRoom>();
        Transform root = room != null ? room.transform : transform.root;
        List<GameObject> candidates = new List<GameObject>();
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null || child == transform)
            {
                continue;
            }

            if (!child.name.ToLowerInvariant().Contains("column") || !child.gameObject.activeInHierarchy)
            {
                continue;
            }

            candidates.Add(child.gameObject);
        }

        if (candidates.Count == 0)
        {
            return;
        }

        GameObject target = candidates[Random.Range(0, candidates.Count)];
        CoverObject cover = target.GetComponent<CoverObject>();
        if (cover != null)
        {
            cover.TakeDamage(999f, (target.transform.position - transform.position).normalized);
        }
        else
        {
            Destroy(target);
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, groundSlamRadius);
        Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere((Vector2)transform.position + GetFacingDirection() * swipeOffset, swipeRadius);
    }

    private float GetScaledBossDamage(float baseDamage)
    {
        GameBalanceData balance = GameBalanceData.Instance;
        int floorNumber = DungeonBuilder.Instance != null ? DungeonBuilder.Instance.CurrentFloorNumber : 1;
        return balance.GetScaledEnemyDamage(baseDamage, floorNumber);
    }
}
