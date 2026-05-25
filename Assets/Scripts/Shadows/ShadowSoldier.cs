using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(SpriteRenderer))]
public class ShadowSoldier : MonoBehaviour
{
    private static readonly Color NecromancyTint = new Color(0.05f, 0.42f, 0.72f, 0.9f);
    private static readonly Color AllyMarkerTint = new Color(0f, 0.95f, 1f, 0.68f);
    private static Sprite allyMarkerSprite;

    private enum ShadowState
    {
        Orbit,
        Chase,
        Attack
    }

    [SerializeField] private float maxHP = 150f;
    [SerializeField] private float orbitRadius = 2f;
    [SerializeField] private float orbitAngularSpeed = 90f;
    [SerializeField] private float chaseRange = 9.5f;
    [SerializeField] private float meleeRange = 1.25f;
    [SerializeField] private float rangedRange = 5f;
    [SerializeField] private float attackCooldown = 0.9f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private CapsuleCollider2D capsuleCollider;
    [SerializeField] private Light2D eyeLight;
    [SerializeField] private SpriteRenderer allyMarkerRenderer;

    private KaisenController owner;
    private EnemyData sourceData;
    private EnemyAttackType attackType = EnemyAttackType.Melee;
    private ShadowState currentState;
    private EnemyBase currentTarget;
    private float currentHP;
    private float damage = 12f;
    private float moveSpeed = 3f;
    private float attackTimer;
    private float orbitAngle;
    private Vector2 facingDirection = Vector2.right;

    public EnemyData SourceData { get { return sourceData; } }
    public EnemyAttackType AttackType { get { return attackType; } }
    public float CurrentHP { get { return currentHP; } }

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

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        gameObject.layer = LayerMask.NameToLayer("Shadow_Soldier");
        chaseRange = Mathf.Max(12.5f, chaseRange);
        currentHP = maxHP;
        EnsureEyeLight();
        EnsureAllyMarker();
    }

    private void Update()
    {
        UpdateAllyMarker();

        if (owner == null)
        {
            return;
        }

        attackTimer -= Time.deltaTime;
        currentTarget = FindNearestEnemy();
        if (currentTarget == null)
        {
            currentState = ShadowState.Orbit;
        }
        else if (TargetInAttackRange())
        {
            currentState = ShadowState.Attack;
            TryAttack();
        }
        else
        {
            currentState = ShadowState.Chase;
        }

        UpdateVisualFacing();
    }

    private void FixedUpdate()
    {
        if (owner == null || rb == null)
        {
            return;
        }

        if (currentState == ShadowState.Orbit)
        {
            orbitAngle += orbitAngularSpeed * Time.fixedDeltaTime;
            Vector2 orbitOffset = new Vector2(Mathf.Cos(orbitAngle * Mathf.Deg2Rad), Mathf.Sin(orbitAngle * Mathf.Deg2Rad)) * orbitRadius;
            Vector2 targetPosition = (Vector2)owner.transform.position + orbitOffset;
            MoveTowards(targetPosition);
            return;
        }

        if (currentState == ShadowState.Chase && currentTarget != null)
        {
            MoveTowards(currentTarget.transform.position);
            return;
        }

        SetVelocity(Vector2.zero);
    }

    public void InitializeFromEnemy(EnemyBase sourceEnemy, KaisenController ownerController, float orbitOffsetDegrees)
    {
        owner = ownerController;
        sourceData = sourceEnemy != null ? sourceEnemy.Data : null;
        orbitAngle = orbitOffsetDegrees;
        currentHP = maxHP;

        if (sourceData != null)
        {
            maxHP = Mathf.Max(maxHP, sourceData.maxHP * 1.15f);
            currentHP = maxHP;
            damage = Mathf.Max(8f, sourceData.damage * 0.85f);
            moveSpeed = Mathf.Max(2.4f, sourceData.moveSpeed * 0.95f);
            chaseRange = Mathf.Max(chaseRange, sourceData.detectionRange + 4f);
            rangedRange = Mathf.Max(rangedRange, sourceData.attackRange);
            attackCooldown = Mathf.Max(0.35f, sourceData.attackCooldown * 0.9f);
            attackType = sourceData.attackType;
            name = "Necromancy_" + sourceData.enemyName.Replace(" ", string.Empty);
        }

        Color sourceColor = Color.white;
        if (sourceEnemy != null)
        {
            transform.position = sourceEnemy.transform.position;
            facingDirection = sourceEnemy.transform.right;
            Sprite sprite = sourceEnemy.CurrentSprite;
            if (spriteRenderer != null && sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }

            SpriteRenderer sourceRenderer = sourceEnemy.GetComponent<SpriteRenderer>();
            if (sourceRenderer != null)
            {
                sourceColor = sourceRenderer.color;
            }

            CapsuleCollider2D sourceCollider = sourceEnemy.GetComponent<CapsuleCollider2D>();
            if (sourceCollider != null && capsuleCollider != null)
            {
                capsuleCollider.size = sourceCollider.size;
                capsuleCollider.offset = sourceCollider.offset;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(sourceColor, NecromancyTint, 0.84f);
            spriteRenderer.sortingLayerName = "Characters";
            spriteRenderer.sortingOrder = 3;
        }

        EnsureEyeLight();
        EnsureAllyMarker();
    }

    public void InitializeFromSoul(ShadowExtractionSystem.ExtractedSoul soul, KaisenController ownerController, float orbitOffsetDegrees, Vector3 spawnPosition)
    {
        owner = ownerController;
        sourceData = soul != null ? soul.sourceData : null;
        orbitAngle = orbitOffsetDegrees;
        currentHP = maxHP;
        transform.position = spawnPosition;

        if (sourceData != null)
        {
            maxHP = Mathf.Max(maxHP, sourceData.maxHP * 1.15f);
            currentHP = maxHP;
            damage = Mathf.Max(8f, sourceData.damage * 0.85f);
            moveSpeed = Mathf.Max(2.4f, sourceData.moveSpeed * 0.95f);
            chaseRange = Mathf.Max(chaseRange, sourceData.detectionRange + 4f);
            rangedRange = Mathf.Max(rangedRange, sourceData.attackRange);
            attackCooldown = Mathf.Max(0.35f, sourceData.attackCooldown * 0.9f);
            attackType = sourceData.attackType;
        }

        if (soul != null)
        {
            name = "ShadowSubdit_" + soul.displayName.Replace(" ", string.Empty);
            facingDirection = soul.facingDirection.sqrMagnitude > 0.001f ? soul.facingDirection.normalized : Vector2.right;

            if (spriteRenderer != null && soul.sprite != null)
            {
                spriteRenderer.sprite = soul.sprite;
            }

            if (capsuleCollider != null)
            {
                capsuleCollider.size = soul.colliderSize;
                capsuleCollider.offset = soul.colliderOffset;
            }
        }
        else if (sourceData != null)
        {
            name = "ShadowSubdit_" + sourceData.enemyName.Replace(" ", string.Empty);
        }

        Color sourceColor = soul != null ? soul.sourceColor : Color.white;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(sourceColor, NecromancyTint, 0.84f);
            spriteRenderer.sortingLayerName = "Characters";
            spriteRenderer.sortingOrder = 3;
        }

        EnsureEyeLight();
        EnsureAllyMarker();
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentHP = Mathf.Max(0f, currentHP - amount);
        if (currentHP <= 0f)
        {
            Die();
        }
    }

    private EnemyBase FindNearestEnemy()
    {
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        EnemyBase nearest = null;
        float nearestDistance = float.MaxValue;
        Vector2 ownerOrigin = owner != null ? (Vector2)owner.transform.position : (Vector2)transform.position;
        Vector2 shadowOrigin = transform.position;
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyBase enemy = enemies[i];
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            float distanceToOwner = Vector2.Distance(ownerOrigin, enemy.transform.position);
            float distanceToShadow = Vector2.Distance(shadowOrigin, enemy.transform.position);
            if (distanceToOwner > chaseRange && distanceToShadow > chaseRange)
            {
                continue;
            }

            if (distanceToShadow < nearestDistance)
            {
                nearestDistance = distanceToShadow;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private bool TargetInAttackRange()
    {
        if (currentTarget == null)
        {
            return false;
        }

        float distance = Vector2.Distance(transform.position, currentTarget.transform.position);
        if (attackType == EnemyAttackType.Ranged)
        {
            return distance <= rangedRange;
        }

        return distance <= meleeRange;
    }

    private void TryAttack()
    {
        if (currentTarget == null || attackTimer > 0f)
        {
            return;
        }

        Vector2 toTarget = ((Vector2)currentTarget.transform.position - (Vector2)transform.position).normalized;
        if (toTarget.sqrMagnitude > 0.001f)
        {
            facingDirection = toTarget;
        }

        float finalDamage = attackType == EnemyAttackType.Heavy ? damage * 1.25f : damage;
        currentTarget.TakeDamage(finalDamage, facingDirection);
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayHitEffect(currentTarget.transform.position, facingDirection);
        }

        if (attackType == EnemyAttackType.Heavy)
        {
            currentTarget.ApplyKnockback(facingDirection, 4f);
        }

        attackTimer = attackCooldown;
    }

    private void MoveTowards(Vector2 targetPosition)
    {
        Vector2 direction = (targetPosition - rb.position);
        if (direction.sqrMagnitude <= 0.001f)
        {
            SetVelocity(Vector2.zero);
            return;
        }

        facingDirection = direction.normalized;
        rb.MovePosition(rb.position + facingDirection * moveSpeed * Time.fixedDeltaTime);
    }

    private void UpdateVisualFacing()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (facingDirection.x > 0.05f)
        {
            spriteRenderer.flipX = false;
        }
        else if (facingDirection.x < -0.05f)
        {
            spriteRenderer.flipX = true;
        }
    }

    private void EnsureEyeLight()
    {
        if (eyeLight != null)
        {
            eyeLight.color = new Color(0f, 0.9f, 1f, 1f);
            eyeLight.intensity = 0.8f;
            eyeLight.pointLightOuterRadius = 0.5f;
            return;
        }

        Transform eyeTransform = transform.Find("EyeLight");
        if (eyeTransform == null)
        {
            GameObject eyeObject = new GameObject("EyeLight");
            eyeTransform = eyeObject.transform;
            eyeTransform.SetParent(transform, false);
            eyeTransform.localPosition = new Vector3(0f, 0.18f, 0f);
        }

        eyeLight = eyeTransform.GetComponent<Light2D>();
        if (eyeLight == null)
        {
            eyeLight = eyeTransform.gameObject.AddComponent<Light2D>();
        }

        eyeLight.lightType = Light2D.LightType.Point;
        eyeLight.color = new Color(0f, 0.9f, 1f, 1f);
        eyeLight.intensity = 0.8f;
        eyeLight.pointLightOuterRadius = 0.5f;
        eyeLight.pointLightInnerRadius = 0.12f;
    }

    private void EnsureAllyMarker()
    {
        Transform markerTransform = allyMarkerRenderer != null ? allyMarkerRenderer.transform : transform.Find("AllyShadowMarker");
        if (markerTransform == null)
        {
            GameObject markerObject = new GameObject("AllyShadowMarker");
            markerTransform = markerObject.transform;
            markerTransform.SetParent(transform, false);
        }

        markerTransform.localPosition = new Vector3(0f, -0.43f, 0f);
        allyMarkerRenderer = markerTransform.GetComponent<SpriteRenderer>();
        if (allyMarkerRenderer == null)
        {
            allyMarkerRenderer = markerTransform.gameObject.AddComponent<SpriteRenderer>();
        }

        allyMarkerRenderer.sprite = GetAllyMarkerSprite();
        allyMarkerRenderer.color = AllyMarkerTint;
        allyMarkerRenderer.sortingLayerName = spriteRenderer != null ? spriteRenderer.sortingLayerName : "Characters";
        allyMarkerRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder - 1 : 2;
        markerTransform.localScale = Vector3.one;
    }

    private void UpdateAllyMarker()
    {
        if (allyMarkerRenderer == null)
        {
            return;
        }

        float pulse = 0.92f + Mathf.Sin(Time.time * 5.5f) * 0.08f;
        allyMarkerRenderer.transform.localScale = new Vector3(pulse, pulse, 1f);
        Color color = AllyMarkerTint;
        color.a = 0.5f + Mathf.Sin(Time.time * 4.25f) * 0.12f;
        allyMarkerRenderer.color = color;
    }

    private static Sprite GetAllyMarkerSprite()
    {
        if (allyMarkerSprite != null)
        {
            return allyMarkerSprite;
        }

        const int size = 48;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 delta = new Vector2(x, y) - center;
                float distance = delta.magnitude;
                float outerRing = Mathf.Clamp01(1f - Mathf.Abs(distance - 17.5f) / 2.1f);
                float innerRing = Mathf.Clamp01(1f - Mathf.Abs(distance - 10.5f) / 1.2f) * 0.32f;
                float diamond = Mathf.Clamp01(1f - (Mathf.Abs(delta.x) + Mathf.Abs(delta.y + 8f)) / 6.5f) * 0.75f;
                float alpha = Mathf.Clamp01(Mathf.Max(outerRing, Mathf.Max(innerRing, diamond)));
                texture.SetPixel(x, y, alpha <= 0.03f ? Color.clear : new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        allyMarkerSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        allyMarkerSprite.name = "AllyShadowMarker";
        return allyMarkerSprite;
    }

    private void Die()
    {
        SpawnDeathParticles();
        EventBus.Publish(new ShadowDiedEvent(this));
        Destroy(gameObject);
    }

    private void SpawnDeathParticles()
    {
        GameObject particleObject = new GameObject("ShadowDeathParticles");
        particleObject.transform.position = transform.position;
        ParticleSystem particleSystem = particleObject.AddComponent<ParticleSystem>();
        var main = particleSystem.main;
        main.duration = 0.35f;
        main.startLifetime = 0.4f;
        main.startSpeed = 1.8f;
        main.startSize = 0.18f;
        main.startColor = new Color(0.6f, 0.2f, 1f, 0.95f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = particleSystem.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 18) });
        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.25f;
        particleSystem.Play();
        Destroy(particleObject, 1.25f);
    }

    private void SetVelocity(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
    }
}
