using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class KnifeAuraProjectile : MonoBehaviour
{
    private readonly HashSet<EnemyBase> hitEnemies = new HashSet<EnemyBase>();

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;
    private Rigidbody2D body;
    private Sprite[] projectileFrames;
    private Sprite[] impactFrames;
    private Vector2 direction = Vector2.right;
    private float damage = 10f;
    private float speed = 9f;
    private float range = 7f;
    private float frameRate = 14f;
    private float impactFrameRate = 18f;
    private float visualScale = 0.85f;
    private float distanceTravelled;
    private float animationTimer;
    private int maxPierceTargets = 2;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();
        body = GetComponent<Rigidbody2D>();

        circleCollider.isTrigger = true;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.simulated = true;
    }

    public void Configure(
        Sprite[] projectileAnimation,
        Sprite[] impactAnimation,
        float projectileSpeed,
        float projectileRange,
        float projectileFrameRate,
        float hitFrameRate,
        float scale,
        float colliderRadius,
        int pierceTargets)
    {
        projectileFrames = projectileAnimation;
        impactFrames = impactAnimation;
        speed = projectileSpeed;
        range = projectileRange;
        frameRate = Mathf.Max(1f, projectileFrameRate);
        impactFrameRate = Mathf.Max(1f, hitFrameRate);
        visualScale = Mathf.Max(0.1f, scale);
        maxPierceTargets = Mathf.Max(1, pierceTargets);
        if (circleCollider != null)
        {
            circleCollider.radius = Mathf.Max(0.05f, colliderRadius);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = "Projectiles";
            spriteRenderer.sortingOrder = 8;
            spriteRenderer.sprite = projectileFrames != null && projectileFrames.Length > 0 ? projectileFrames[0] : null;
        }

        transform.localScale = new Vector3(visualScale, visualScale, 1f);
    }

    public void Fire(Vector2 spawnPosition, Vector2 fireDirection, float damageValue)
    {
        transform.position = spawnPosition;
        direction = fireDirection.sqrMagnitude > 0.001f ? fireDirection.normalized : Vector2.right;
        transform.right = direction;
        damage = damageValue;
        distanceTravelled = 0f;
        animationTimer = 0f;
        hitEnemies.Clear();
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        distanceTravelled += step;
        animationTimer += Time.deltaTime;

        if (spriteRenderer != null && projectileFrames != null && projectileFrames.Length > 0)
        {
            int frameIndex = Mathf.FloorToInt(animationTimer * frameRate) % projectileFrames.Length;
            spriteRenderer.sprite = projectileFrames[frameIndex];
        }

        if (distanceTravelled >= Mathf.Max(0.1f, range))
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
        if (enemy != null && !enemy.IsDead)
        {
            if (!hitEnemies.Add(enemy))
            {
                return;
            }

            enemy.TakeDamage(damage, direction);
            SpawnImpact(enemy.transform.position);
            if (hitEnemies.Count >= maxPierceTargets)
            {
                Destroy(gameObject);
            }
            return;
        }

        if (other.CompareTag("Cover"))
        {
            CoverObject cover = other.GetComponentInParent<CoverObject>();
            if (cover != null)
            {
                cover.TakeDamage(damage, direction);
            }

            SpawnImpact(transform.position);
            Destroy(gameObject);
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(AudioCueId.WallImpact, transform.position, 0.65f, 1.25f, true, 0.75f);
            }

            SpawnImpact(transform.position);
            Destroy(gameObject);
        }
    }

    private void SpawnImpact(Vector3 position)
    {
        KnifeAuraSpriteEffect.Spawn(
            "KnifeAuraImpact",
            impactFrames,
            position,
            direction,
            impactFrameRate,
            visualScale,
            "VFX",
            20);
    }
}

internal class KnifeAuraSpriteEffect : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private float frameRate = 16f;
    private float elapsed;

    public static void Spawn(
        string objectName,
        Sprite[] animationFrames,
        Vector3 position,
        Vector2 direction,
        float animationFrameRate,
        float scale,
        string sortingLayerName,
        int sortingOrder)
    {
        if (animationFrames == null || animationFrames.Length == 0)
        {
            return;
        }

        GameObject effectObject = new GameObject(objectName);
        effectObject.transform.position = position;
        Vector2 finalDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        effectObject.transform.right = finalDirection;
        effectObject.transform.localScale = new Vector3(scale, scale, 1f);

        SpriteRenderer renderer = effectObject.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = sortingLayerName;
        renderer.sortingOrder = sortingOrder;
        renderer.sprite = animationFrames[0];

        KnifeAuraSpriteEffect effect = effectObject.AddComponent<KnifeAuraSpriteEffect>();
        effect.spriteRenderer = renderer;
        effect.frames = animationFrames;
        effect.frameRate = Mathf.Max(1f, animationFrameRate);
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.deltaTime;
        int frameIndex = Mathf.FloorToInt(elapsed * frameRate);
        if (frameIndex >= frames.Length)
        {
            Destroy(gameObject);
            return;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = frames[frameIndex];
        }
    }
}
