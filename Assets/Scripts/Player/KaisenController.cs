using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class KaisenController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private CapsuleCollider2D capsuleCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform visualPivot;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private WeaponManager weaponManager;

    [Header("Stats")]
    [SerializeField] private KaisenStats stats = new KaisenStats();

    [Header("Runtime Modifiers")]
    [SerializeField] private float moveSpeedMultiplier = 1f;
    [SerializeField] private float attackDamageMultiplier = 1f;
    [SerializeField] private bool combatInputEnabled = true;

    [Header("Dodge")]
    [SerializeField] private float dodgeDuration = 0.3f;
    [SerializeField] private float dodgeSpeed = 12f;
    [SerializeField] private float dodgeInvulnerabilityWindow = 0.2f;

    [Header("Melee")]
    [SerializeField] private float meleeRadius = 1.5f;
    [SerializeField] private float meleeOffset = 0.75f;
    [SerializeField] private float meleeActiveTime = 0.15f;
    [SerializeField] private float meleeCooldown = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Animation")]
    [SerializeField] private int attackAnimationVariantCount = 7;
    [SerializeField] private float hitAnimationDuration = 0.35f;
    [SerializeField] private float deathAnimationDuration = 0.55f;
    [SerializeField] private bool rotateVisualToAim;
    [SerializeField] private bool flipVisualToAim = true;

    private Vector2 moveInput;
    private Vector2 dodgeDirection = Vector2.down;
    private Vector2 aimDirection = Vector2.down;
    private float nextDodgeAvailableTime;
    private float nextAttackAvailableTime;
    private bool isDodging;
    private bool isAttacking;
    private bool painStatusActive;
    private int attackAnimationIndex = -1;
    private Coroutine attackAnimationRoutine;
    private Coroutine hitAnimationRoutine;
    private Coroutine painStatusRoutine;

    public KaisenStats Stats { get { return stats; } }
    public bool IsDodging { get { return isDodging; } }
    public float CurrentMoveSpeed { get { return stats.MoveSpeed * moveSpeedMultiplier; } }
    public float CurrentAttackDamage { get { return stats.AttackDamage * attackDamageMultiplier; } }
    public Vector2 AimDirection { get { return aimDirection; } }
    public bool HasPainStatus { get { return painStatusActive; } }
    public float DodgeCooldownDuration { get { return stats != null ? stats.DodgeCooldown : 0f; } }
    public float DodgeCooldownRemaining { get { return isDodging ? dodgeDuration : Mathf.Max(0f, nextDodgeAvailableTime - Time.time); } }
    public float DodgeCooldownNormalized
    {
        get
        {
            if (isDodging)
            {
                return 0f;
            }

            float duration = Mathf.Max(0.001f, DodgeCooldownDuration);
            return Mathf.Clamp01(1f - (DodgeCooldownRemaining / duration));
        }
    }
    public bool IsDodgeReady { get { return !isDodging && Time.time >= nextDodgeAvailableTime; } }
    public SpriteRenderer VisualSprite { get { return spriteRenderer; } }
    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (capsuleCollider == null)
        {
            capsuleCollider = GetComponent<CapsuleCollider2D>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (visualPivot == null && spriteRenderer != null)
        {
            visualPivot = spriteRenderer.transform;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (weaponManager == null)
        {
            weaponManager = GetComponent<WeaponManager>();
        }

        ConfigurePhysicsBody();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PenaltyActivatedEvent>(OnPenaltyActivated);
        if (animator != null)
        {
            animator.SetBool("IsDead", false);
        }
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PenaltyActivatedEvent>(OnPenaltyActivated);
        StopVisualAnimationRoutines();
    }

    private void Update()
    {
        ReadMovementInput();
        UpdateAimDirection();
        HandleActions();
        UpdateAnimatorParameters();
    }

    private void FixedUpdate()
    {
        if (isDodging)
        {
            rb.MovePosition(rb.position + (dodgeDirection * dodgeSpeed * Time.fixedDeltaTime));
            return;
        }

        Vector2 desiredMovement = moveInput.normalized * CurrentMoveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + desiredMovement);
    }

    public void AddAttackDamageBonusMultiplier(float additivePercentage)
    {
        attackDamageMultiplier *= 1f + Mathf.Max(0f, additivePercentage);
    }

    public void SetStats(KaisenStats sourceStats)
    {
        if (sourceStats == null)
        {
            return;
        }

        stats.CopyFrom(sourceStats);
        nextDodgeAvailableTime = 0f;
        nextAttackAvailableTime = 0f;
    }

    public void SetMoveSpeedMultiplier(float multiplier)
    {
        moveSpeedMultiplier = Mathf.Max(0.1f, multiplier);
    }

    public void SetCombatInputEnabled(bool enabled)
    {
        combatInputEnabled = enabled;
    }

    public void SetVisualTint(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    public void PlayAttackAnimation(float duration)
    {
        if (animator == null)
        {
            return;
        }

        if (attackAnimationRoutine != null)
        {
            StopCoroutine(attackAnimationRoutine);
        }

        int variantCount = Mathf.Max(1, attackAnimationVariantCount);
        attackAnimationIndex = (attackAnimationIndex + 1) % variantCount;
        animator.SetInteger("AttackIndex", attackAnimationIndex);
        attackAnimationRoutine = StartCoroutine(AttackAnimationRoutine(Mathf.Max(0.05f, duration)));
    }

    public void PlayHitAnimation()
    {
        PlayHitAnimation(hitAnimationDuration);
    }

    public void PlayHitAnimation(float duration)
    {
        if (animator == null)
        {
            return;
        }

        if (hitAnimationRoutine != null)
        {
            StopCoroutine(hitAnimationRoutine);
        }

        hitAnimationRoutine = StartCoroutine(HitAnimationRoutine(Mathf.Max(0.05f, duration)));
    }

    public float PlayDeathAnimation()
    {
        if (animator == null)
        {
            return deathAnimationDuration;
        }

        StopVisualAnimationRoutines();
        isDodging = false;
        isAttacking = false;
        animator.SetBool("IsDodging", false);
        animator.SetBool("IsAttacking", false);
        animator.SetBool("IsHit", false);
        animator.SetBool("IsDead", true);
        return deathAnimationDuration;
    }

    private void ReadMovementInput()
    {
        Vector2 keyboardInput = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) keyboardInput.x -= 1f;
            if (Keyboard.current.dKey.isPressed) keyboardInput.x += 1f;
            if (Keyboard.current.sKey.isPressed) keyboardInput.y -= 1f;
            if (Keyboard.current.wKey.isPressed) keyboardInput.y += 1f;
        }

        Vector2 gamepadInput = Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
        moveInput = gamepadInput.sqrMagnitude > keyboardInput.sqrMagnitude ? gamepadInput : keyboardInput;
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
    }

    private void UpdateAimDirection()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        if (Mouse.current != null)
        {
            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(mainCamera.transform.position.z - transform.position.z)));
            Vector3 pivotPosition = visualPivot != null ? visualPivot.position : transform.position;
            Vector2 direction = (Vector2)(worldPosition - pivotPosition);
            if (direction.sqrMagnitude > 0.0001f)
            {
                aimDirection = direction.normalized;
            }
        }

        if (visualPivot != null)
        {
            if (rotateVisualToAim)
            {
                float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
                visualPivot.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                visualPivot.localRotation = Quaternion.identity;
            }
        }

        if (flipVisualToAim && spriteRenderer != null && Mathf.Abs(aimDirection.x) > 0.08f)
        {
            spriteRenderer.flipX = aimDirection.x < 0f;
        }
    }

    private void HandleActions()
    {
        if (!combatInputEnabled)
        {
            return;
        }

        bool dodgePressed = (Keyboard.current != null && (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame))
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        bool attackPressed = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);

        if (dodgePressed)
        {
            TryStartDodge();
        }

        if (attackPressed)
        {
            weaponManager?.TryFireActiveWeapon(aimDirection);
        }
    }

    private void TryStartDodge()
    {
        if (isDodging || Time.time < nextDodgeAvailableTime)
        {
            return;
        }

        dodgeDirection = moveInput.sqrMagnitude > 0.001f ? moveInput.normalized : aimDirection.normalized;
        if (dodgeDirection.sqrMagnitude <= 0.001f)
        {
            dodgeDirection = Vector2.down;
        }

        StartCoroutine(DodgeRoutine());
    }

    private IEnumerator DodgeRoutine()
    {
        isDodging = true;
        EventBus.Publish(new PlayerDodgedEvent(gameObject, rb.position, dodgeDirection));

        if (animator != null)
        {
            animator.SetBool("IsDodging", true);
        }

        capsuleCollider.enabled = false;
        yield return new WaitForSeconds(dodgeInvulnerabilityWindow);
        capsuleCollider.enabled = true;

        float remainingTime = Mathf.Max(0f, dodgeDuration - dodgeInvulnerabilityWindow);
        if (remainingTime > 0f)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        isDodging = false;
        nextDodgeAvailableTime = Time.time + stats.DodgeCooldown;
        if (animator != null)
        {
            animator.SetBool("IsDodging", false);
        }
    }

    private void TryStartAttack()
    {
        if (isAttacking || isDodging || Time.time < nextAttackAvailableTime)
        {
            return;
        }

        StartCoroutine(MeleeAttackRoutine());
    }

    private IEnumerator MeleeAttackRoutine()
    {
        isAttacking = true;
        nextAttackAvailableTime = Time.time + meleeCooldown;
        if (animator != null)
        {
            animator.SetBool("IsAttacking", true);
        }

        HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();
        float timer = 0f;
        while (timer < meleeActiveTime)
        {
            Vector2 attackCenter = rb.position + (aimDirection.normalized * meleeOffset);
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, meleeRadius, enemyLayer);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null || !hitTargets.Add(hit))
                {
                    continue;
                }

                EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeDamage(CurrentAttackDamage, aimDirection);
                    continue;
                }

                hit.SendMessage("TakeDamage", CurrentAttackDamage, SendMessageOptions.DontRequireReceiver);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
        }

        isAttacking = false;
    }

    private IEnumerator AttackAnimationRoutine(float duration)
    {
        isAttacking = true;
        animator.SetBool("IsAttacking", true);
        yield return new WaitForSeconds(duration);
        animator.SetBool("IsAttacking", false);
        isAttacking = false;
        attackAnimationRoutine = null;
    }

    private IEnumerator HitAnimationRoutine(float duration)
    {
        animator.SetBool("IsHit", true);
        yield return new WaitForSeconds(duration);
        animator.SetBool("IsHit", false);
        hitAnimationRoutine = null;
    }

    private void StopVisualAnimationRoutines()
    {
        if (attackAnimationRoutine != null)
        {
            StopCoroutine(attackAnimationRoutine);
            attackAnimationRoutine = null;
        }

        if (hitAnimationRoutine != null)
        {
            StopCoroutine(hitAnimationRoutine);
            hitAnimationRoutine = null;
        }

        if (animator != null && animator.isInitialized && animator.runtimeAnimatorController != null)
        {
            animator.SetBool("IsAttacking", false);
            animator.SetBool("IsHit", false);
        }

        isAttacking = false;
    }

    private void UpdateAnimatorParameters()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat("Speed", moveInput.magnitude);
        animator.SetFloat("DirectionX", aimDirection.x);
        animator.SetFloat("DirectionY", aimDirection.y);
    }

    private void OnPenaltyActivated(PenaltyActivatedEvent penaltyEvent)
    {
        if (penaltyEvent == null)
        {
            return;
        }

        if (painStatusRoutine != null)
        {
            StopCoroutine(painStatusRoutine);
        }

        painStatusRoutine = StartCoroutine(PainStatusRoutine(penaltyEvent));
    }

    private IEnumerator PainStatusRoutine(PenaltyActivatedEvent penaltyEvent)
    {
        painStatusActive = true;
        moveSpeedMultiplier = Mathf.Max(0.1f, penaltyEvent.moveSpeedMultiplier);
        if (playerHealth != null)
        {
            playerHealth.SetMaxHealthMultiplier(penaltyEvent.maxHpMultiplier, true);
        }

        if (NotificationSystem.Instance != null)
        {
            NotificationSystem.Instance.ShowPainVignette(penaltyEvent.duration);
        }

        float elapsed = 0f;
        while (elapsed < penaltyEvent.duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        moveSpeedMultiplier = 1f;
        if (playerHealth != null)
        {
            playerHealth.SetMaxHealthMultiplier(1f, true);
        }
        painStatusActive = false;
        painStatusRoutine = null;
    }

    private void ConfigurePhysicsBody()
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        if (capsuleCollider != null)
        {
            capsuleCollider.size = new Vector2(0.7f, 0.5f);
            capsuleCollider.offset = new Vector2(0f, -0.2f);
        }

        if (enemyLayer.value == 0)
        {
            enemyLayer = LayerMask.GetMask("Enemy");
        }
    }

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        visualPivot = spriteRenderer != null ? spriteRenderer.transform : null;
        mainCamera = Camera.main;
        playerHealth = GetComponent<PlayerHealth>();
        weaponManager = GetComponent<WeaponManager>();
        enemyLayer = LayerMask.GetMask("Enemy");
        ConfigurePhysicsBody();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 center = Application.isPlaying && rb != null
            ? rb.position + (aimDirection.normalized * meleeOffset)
            : (Vector2)transform.position + (aimDirection.normalized * meleeOffset);
        Gizmos.DrawWireSphere(center, meleeRadius);
    }
}
