using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Stagger,
    Dead
}

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public abstract class EnemyBase : MonoBehaviour
{
    private const float NecromancyRaiseWindow = 18f;

    [Header("Enemy Data")]
    [SerializeField] protected EnemyData data;

    [Header("Patrol")]
    [SerializeField] protected List<Vector2> patrolWaypoints = new List<Vector2>
    {
        new Vector2(-1.5f, 0f),
        new Vector2(1.5f, 0f)
    };

    [Header("Behaviour Timings")]
    [SerializeField] protected float idleDuration = 2f;
    [SerializeField] protected float detectionInterval = 0.3f;
    [SerializeField] protected float staggerDuration = 0.2f;
    [SerializeField] protected float staggerFlashDuration = 0.1f;
    [SerializeField] protected float staggerKnockbackForce = 6f;
    [SerializeField] protected LayerMask playerLayer;

    protected Rigidbody2D rb;
    protected CapsuleCollider2D capsuleCollider;
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;
    protected Transform playerTarget;
    protected PlayerHealth playerHealth;
    protected EnemyState currentState;
    protected Vector2 movementDirection = Vector2.right;
    protected Vector2 spawnPosition;

    private Color originalColor = Color.white;
    private float currentHP;
    private float stateTimer;
    private float detectionTimer;
    private int patrolIndex;
    private float deathTimestamp = -999f;
    private bool shadowExtracted;
    private float pauseUntilTime;

    public EnemyData Data => data;
    public EnemyState CurrentState => currentState;
    public float CurrentHP => currentHP;
    public float MaxHP => data != null ? data.maxHP : 0f;
    public float DeathTimestamp => deathTimestamp;
    public bool IsDead => currentState == EnemyState.Dead;
    public bool IsAvailableForShadowExtraction => IsDead && !shadowExtracted && data != null && data.isExtractable && Time.time - deathTimestamp <= NecromancyRaiseWindow;
    public Sprite CurrentSprite => spriteRenderer != null ? spriteRenderer.sprite : null;
    protected Rigidbody2D Body => rb;
    protected Transform TargetTransform => playerTarget;
    protected PlayerHealth TargetHealth => playerHealth;
    protected LayerMask PlayerLayerMask => playerLayer;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (playerLayer.value == 0)
        {
            playerLayer = LayerMask.GetMask("Player");
        }

        ConfigurePhysicsBody();
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
    }

    protected virtual void Start()
    {
        spawnPosition = rb != null ? rb.position : (Vector2)transform.position;
        currentHP = data != null ? data.maxHP : 1f;
        SetState(EnemyState.Idle, idleDuration);
    }

    protected virtual void Update()
    {
        if (currentState == EnemyState.Dead)
        {
            return;
        }

        detectionTimer -= Time.deltaTime;
        if (detectionTimer <= 0f)
        {
            detectionTimer = detectionInterval;
            RefreshPlayerTarget();
        }

        TickState(Time.deltaTime);
        UpdateSpriteFlip();
    }

    protected virtual void FixedUpdate()
    {
        if (currentState == EnemyState.Dead || rb == null)
        {
            return;
        }

        if (currentState == EnemyState.Stagger)
        {
            return;
        }

        Vector2 desiredVelocity = Vector2.zero;
        if (Time.time >= pauseUntilTime)
        {
            if (currentState == EnemyState.Patrol)
            {
                desiredVelocity = movementDirection.normalized * GetPatrolSpeed();
            }
            else if (currentState == EnemyState.Chase)
            {
                desiredVelocity = movementDirection.normalized * GetChaseSpeed();
            }
        }

        rb.MovePosition(rb.position + desiredVelocity * Time.fixedDeltaTime);
    }

    public void ApplyData(EnemyData runtimeData)
    {
        data = runtimeData;
        currentHP = data != null ? data.maxHP : currentHP;
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, Vector2.zero);
    }

    public virtual void TakeDamage(float amount, Vector2 hitDirection)
    {
        if (currentState == EnemyState.Dead || amount <= 0f)
        {
            return;
        }

        currentHP = Mathf.Max(0f, currentHP - amount);
        if (currentHP <= 0f)
        {
            EnterDeadState();
            return;
        }

        if (VFXManager.Instance != null)
        {
            Vector2 impactDirection = hitDirection.sqrMagnitude > 0.001f ? hitDirection.normalized : -GetDirectionToPlayer();
            VFXManager.Instance.PlayHitEffect(transform.position, impactDirection);
        }

        EnterStaggerState(hitDirection);
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (currentState == EnemyState.Dead)
        {
            return;
        }

        Vector2 finalDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.zero;
        if (finalDirection == Vector2.zero)
        {
            return;
        }

        SetBodyVelocity(finalDirection * force);
        PauseBehaviour(0.12f);
    }

    protected abstract void PerformAttack();

    protected virtual Vector2 GetChaseDirection(Vector2 toPlayer, float distanceToPlayer)
    {
        return toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector2.zero;
    }

    protected void PauseBehaviour(float duration)
    {
        pauseUntilTime = Mathf.Max(pauseUntilTime, Time.time + duration);
    }

    protected float GetDistanceToPlayer()
    {
        if (!HasValidPlayerTarget())
        {
            return float.MaxValue;
        }

        return Vector2.Distance(rb.position, playerTarget.position);
    }

    protected Vector2 GetDirectionToPlayer()
    {
        if (!HasValidPlayerTarget())
        {
            return Vector2.zero;
        }

        return ((Vector2)playerTarget.position - rb.position).normalized;
    }

    protected Vector2 GetAttackOrigin(float distanceFromCenter)
    {
        Vector2 direction = GetDirectionToPlayer();
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = movementDirection.sqrMagnitude > 0.001f ? movementDirection.normalized : Vector2.right;
        }

        return rb.position + direction * distanceFromCenter;
    }

    protected virtual float GetPatrolSpeed()
    {
        return data != null ? data.moveSpeed * 0.5f : 1f;
    }

    protected virtual float GetChaseSpeed()
    {
        return data != null ? data.moveSpeed : 2f;
    }

    public virtual void MarkShadowExtracted()
    {
        shadowExtracted = true;
        Destroy(gameObject);
    }

    protected virtual float GetDeathDespawnDelay()
    {
        return data != null && data.isExtractable ? NecromancyRaiseWindow : 2f;
    }

    protected virtual void OnEnteredDeadState()
    {
    }

    private void TickState(float deltaTime)
    {
        if (currentState == EnemyState.Stagger)
        {
            stateTimer -= deltaTime;
            if (stateTimer <= 0f)
            {
                SetBodyVelocity(Vector2.zero);
                ReturnToPassiveOrAggroState();
            }
            return;
        }

        if (Time.time < pauseUntilTime)
        {
            return;
        }

        switch (currentState)
        {
            case EnemyState.Idle:
                TickIdle(deltaTime);
                break;
            case EnemyState.Patrol:
                TickPatrol();
                break;
            case EnemyState.Chase:
                TickChase();
                break;
            case EnemyState.Attack:
                TickAttack(deltaTime);
                break;
        }
    }

    private void TickIdle(float deltaTime)
    {
        if (TryEnterChaseState())
        {
            return;
        }

        stateTimer -= deltaTime;
        if (stateTimer <= 0f)
        {
            SetState(patrolWaypoints.Count > 0 ? EnemyState.Patrol : EnemyState.Idle, patrolWaypoints.Count > 0 ? 0f : idleDuration);
        }
    }

    private void TickPatrol()
    {
        if (TryEnterChaseState())
        {
            return;
        }

        UpdatePatrolMovement();
    }

    private void TickChase()
    {
        if (!HasValidPlayerTarget())
        {
            ReturnToPassiveOrAggroState();
            return;
        }

        float distanceToPlayer = GetDistanceToPlayer();
        if (data != null && distanceToPlayer <= data.attackRange)
        {
            SetState(EnemyState.Attack, data.attackCooldown);
            PerformAttack();
            return;
        }

        Vector2 toPlayer = (Vector2)playerTarget.position - rb.position;
        movementDirection = GetChaseDirection(toPlayer, distanceToPlayer);
    }

    private void TickAttack(float deltaTime)
    {
        if (!HasValidPlayerTarget())
        {
            ReturnToPassiveOrAggroState();
            return;
        }

        float distanceToPlayer = GetDistanceToPlayer();
        if (data == null || distanceToPlayer > data.attackRange + 0.2f)
        {
            SetState(EnemyState.Chase);
            return;
        }

        stateTimer -= deltaTime;
        if (stateTimer <= 0f)
        {
            SetState(EnemyState.Attack, data.attackCooldown);
            PerformAttack();
        }
    }

    private bool TryEnterChaseState()
    {
        if (!HasValidPlayerTarget() || data == null)
        {
            return false;
        }

        if (Vector2.Distance(rb.position, playerTarget.position) > data.detectionRange)
        {
            return false;
        }

        SetState(EnemyState.Chase);
        return true;
    }

    private void UpdatePatrolMovement()
    {
        if (patrolWaypoints.Count == 0)
        {
            SetState(EnemyState.Idle, idleDuration);
            return;
        }

        Vector2 waypoint = spawnPosition + patrolWaypoints[patrolIndex];
        Vector2 toWaypoint = waypoint - rb.position;
        if (toWaypoint.magnitude <= 0.1f)
        {
            patrolIndex = (patrolIndex + 1) % patrolWaypoints.Count;
            waypoint = spawnPosition + patrolWaypoints[patrolIndex];
            toWaypoint = waypoint - rb.position;
        }

        movementDirection = toWaypoint.normalized;
    }

    private void RefreshPlayerTarget()
    {
        if (data == null)
        {
            playerTarget = null;
            playerHealth = null;
            return;
        }

        Vector2 origin = rb != null ? rb.position : (Vector2)transform.position;
        Collider2D detectedPlayer = Physics2D.OverlapCircle(origin, data.detectionRange, playerLayer);
        if (detectedPlayer == null)
        {
            playerTarget = null;
            playerHealth = null;
            return;
        }

        playerHealth = detectedPlayer.GetComponentInParent<PlayerHealth>();
        playerTarget = playerHealth != null ? playerHealth.transform : detectedPlayer.transform;
    }

    private bool HasValidPlayerTarget()
    {
        return playerTarget != null && playerHealth != null && playerHealth.gameObject.activeInHierarchy;
    }

    private void EnterStaggerState(Vector2 hitDirection)
    {
        SetState(EnemyState.Stagger, staggerDuration);
        Vector2 knockbackDirection = hitDirection.sqrMagnitude > 0.001f ? -hitDirection.normalized : -GetDirectionToPlayer();
        if (knockbackDirection.sqrMagnitude <= 0.001f)
        {
            knockbackDirection = Vector2.left;
        }

        SetBodyVelocity(knockbackDirection * staggerKnockbackForce);
        StopCoroutine(nameof(FlashRoutine));
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderer == null)
        {
            yield break;
        }

        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(staggerFlashDuration);
        spriteRenderer.color = originalColor;
    }

    private void EnterDeadState()
    {
        if (currentState == EnemyState.Dead)
        {
            return;
        }

        currentState = EnemyState.Dead;
        deathTimestamp = Time.time;
        shadowExtracted = false;

        if (capsuleCollider != null)
        {
            capsuleCollider.enabled = false;
        }

        if (rb != null)
        {
            SetBodyVelocity(Vector2.zero);
            rb.simulated = true;
        }

        PlayAnimation("Death");
        SpawnDrops();
        OnEnteredDeadState();
        EventBus.Publish(new EnemyDiedEvent(gameObject, rb != null ? rb.position : (Vector2)transform.position, data != null ? data.expValue : 0));
        Destroy(gameObject, GetDeathDespawnDelay());
    }

    protected virtual void SpawnDrops()
    {
        if (data == null || data.dropTable == null)
        {
            return;
        }

        for (int i = 0; i < data.dropTable.Count; i++)
        {
            DropEntry drop = data.dropTable[i];
            if (drop == null || drop.prefab == null)
            {
                continue;
            }

            if (Random.value > drop.chance)
            {
                continue;
            }

            if (LootSpawner.Instance != null)
            {
                LootSpawner.Instance.SpawnDrop(drop.prefab, transform.position);
            }
            else
            {
                Vector2 offset = Random.insideUnitCircle * 0.25f;
                Instantiate(drop.prefab, (Vector2)transform.position + offset, Quaternion.identity);
            }
        }
    }

    private void SetState(EnemyState newState, float timer = 0f)
    {
        currentState = newState;
        stateTimer = timer;

        if (newState != EnemyState.Stagger && rb != null)
        {
            SetBodyVelocity(Vector2.zero);
        }

        switch (newState)
        {
            case EnemyState.Idle:
                movementDirection = Vector2.zero;
                PlayAnimation("Idle");
                break;
            case EnemyState.Patrol:
            case EnemyState.Chase:
                PlayAnimation("Move");
                break;
            case EnemyState.Attack:
                movementDirection = Vector2.zero;
                PlayAnimation("Attack");
                break;
            case EnemyState.Stagger:
                PlayAnimation("Move");
                break;
        }
    }

    private void ReturnToPassiveOrAggroState()
    {
        if (HasValidPlayerTarget() && data != null && Vector2.Distance(rb.position, playerTarget.position) <= data.detectionRange)
        {
            SetState(EnemyState.Chase);
            return;
        }

        if (patrolWaypoints.Count > 0)
        {
            SetState(EnemyState.Patrol);
        }
        else
        {
            SetState(EnemyState.Idle, idleDuration);
        }
    }

    private void UpdateSpriteFlip()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (movementDirection.x > 0.05f)
        {
            spriteRenderer.flipX = false;
        }
        else if (movementDirection.x < -0.05f)
        {
            spriteRenderer.flipX = true;
        }
    }

    protected void PlayAnimation(string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(stateName))
        {
            return;
        }

        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, stateHash))
        {
            return;
        }

        animator.Play(stateHash, 0, 0f);
    }

    private void ConfigurePhysicsBody()
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        if (capsuleCollider != null)
        {
            capsuleCollider.direction = CapsuleDirection2D.Vertical;
            capsuleCollider.size = new Vector2(0.8f, 0.9f);
        }
    }

    protected void SetBodyVelocity(Vector2 velocity)
    {
        if (rb == null)
        {
            return;
        }

#if UNITY_6000_OR_NEWER
        rb.linearVelocity = velocity;
#else
        rb.linearVelocity = velocity;
#endif
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (data != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, data.detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.attackRange);
        }

        if (patrolWaypoints == null || patrolWaypoints.Count == 0)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Vector3 basePosition = Application.isPlaying ? (Vector3)spawnPosition : transform.position;
        for (int i = 0; i < patrolWaypoints.Count; i++)
        {
            Vector3 worldPoint = basePosition + (Vector3)patrolWaypoints[i];
            Gizmos.DrawSphere(worldPoint, 0.08f);
            Vector3 nextPoint = basePosition + (Vector3)patrolWaypoints[(i + 1) % patrolWaypoints.Count];
            Gizmos.DrawLine(worldPoint, nextPoint);
        }
    }
}
