using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D))]
public class ExperienceOrbPickup : MonoBehaviour
{
    private const float MagnetDelay = 0.18f;
    private const float AttractionRange = 5.25f;
    private const float CollectDistance = 0.24f;
    private const float MaxLifetime = 14f;
    private const float FadeStartTime = 10.5f;
    private const float AnimationFps = 10f;
    private const float BaseScale = 0.36f;

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D triggerCollider;
    private Rigidbody2D body;
    private Sprite[] frames;
    private Transform target;
    private Vector3 velocity;
    private int experienceValue = 1;
    private float elapsed;
    private float animationTimer;
    private float bobSeed;
    private float individualMagnetDelay;
    private bool collected;

    public static void SpawnCluster(Vector3 position, int totalExperience)
    {
        if (totalExperience <= 0)
        {
            return;
        }

        ExperienceSpriteEffect.PlayDeathBurst(position);

        int orbCount = Mathf.Clamp(Mathf.CeilToInt(totalExperience / 2f), 1, 7);
        int baseValue = Mathf.Max(1, totalExperience / orbCount);
        int remainder = totalExperience % orbCount;

        for (int i = 0; i < orbCount; i++)
        {
            Vector2 scatterDirection = Random.insideUnitCircle;
            if (scatterDirection.sqrMagnitude <= 0.001f)
            {
                float angle = (i / (float)Mathf.Max(1, orbCount)) * Mathf.PI * 2f;
                scatterDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            scatterDirection.Normalize();
            Vector3 spawnPosition = position + (Vector3)(scatterDirection * Random.Range(0.05f, 0.16f));
            GameObject orbObject = new GameObject("ExperienceOrbPickup");
            orbObject.transform.position = spawnPosition;

            ExperienceOrbPickup pickup = orbObject.AddComponent<ExperienceOrbPickup>();
            int value = baseValue + (i < remainder ? 1 : 0);
            pickup.Initialize(value, scatterDirection * Random.Range(1.4f, 2.7f), Random.Range(0f, 0.16f));
        }
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        triggerCollider = GetComponent<CircleCollider2D>();
        body = GetComponent<Rigidbody2D>();

        triggerCollider.isTrigger = true;
        triggerCollider.radius = 0.18f;

        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;

        spriteRenderer.sortingLayerName = "VFX";
        spriteRenderer.sortingOrder = 25;
        frames = ExperienceSpriteCache.OrbFrames;
        spriteRenderer.sprite = frames != null && frames.Length > 0 ? frames[0] : null;

        bobSeed = Random.Range(0f, Mathf.PI * 2f);
        target = FindPlayerTransform();
        transform.localScale = Vector3.one * BaseScale;
    }

    public void Initialize(int value, Vector2 launchVelocity, float extraMagnetDelay)
    {
        experienceValue = Mathf.Max(1, value);
        velocity = launchVelocity;
        individualMagnetDelay = MagnetDelay + Mathf.Max(0f, extraMagnetDelay);
        gameObject.name = "ExperienceOrb_" + experienceValue + "XP";
    }

    private void Update()
    {
        if (collected)
        {
            return;
        }

        elapsed += Time.deltaTime;
        AnimateSprite();
        MoveOrb();

        if (elapsed >= MaxLifetime)
        {
            Destroy(gameObject);
        }
    }

    private void AnimateSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        animationTimer += Time.deltaTime * AnimationFps;
        if (frames != null && frames.Length > 0)
        {
            int frameIndex = Mathf.FloorToInt(animationTimer) % frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
        }

        float pulse = 1f + Mathf.Sin((Time.time + bobSeed) * 8f) * 0.08f;
        transform.localScale = Vector3.one * BaseScale * pulse;

        Color color = spriteRenderer.color;
        color.a = elapsed < FadeStartTime ? 1f : Mathf.Clamp01(1f - ((elapsed - FadeStartTime) / (MaxLifetime - FadeStartTime)));
        spriteRenderer.color = color;
    }

    private void MoveOrb()
    {
        velocity = Vector3.Lerp(velocity, Vector3.zero, 5.5f * Time.deltaTime);
        transform.position += velocity * Time.deltaTime;

        if (target == null)
        {
            target = FindPlayerTransform();
        }

        if (target == null || elapsed < individualMagnetDelay)
        {
            transform.position += Vector3.up * (Mathf.Sin((Time.time + bobSeed) * 3.2f) * 0.018f * Time.deltaTime);
            return;
        }

        Vector3 targetPosition = target.position + Vector3.up * 0.2f;
        float distance = Vector2.Distance(transform.position, targetPosition);
        if (distance <= CollectDistance)
        {
            Collect();
            return;
        }

        if (distance > AttractionRange)
        {
            transform.position += Vector3.up * (Mathf.Sin((Time.time + bobSeed) * 3.2f) * 0.018f * Time.deltaTime);
            return;
        }

        float normalized = 1f - Mathf.Clamp01(distance / AttractionRange);
        float pullSpeed = Mathf.Lerp(1.8f, 9.5f, normalized * normalized);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, pullSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || collected)
        {
            return;
        }

        if (other.GetComponentInParent<KaisenController>() != null || other.GetComponentInParent<PlayerHealth>() != null)
        {
            Collect();
        }
    }

    private void Collect()
    {
        if (collected)
        {
            return;
        }

        collected = true;
        float grantedExperience = experienceValue;
        if (SystemManager.Instance != null)
        {
            grantedExperience *= SystemManager.Instance.GetPerceptionMultiplier();
            SystemManager.Instance.AddExperience(grantedExperience);
        }

        ExperienceSpriteEffect.PlayCollectBurst(transform.position);
        FloatingTextPopup.Spawn("+" + Mathf.RoundToInt(grantedExperience) + " XP", new Color(0.45f, 0.9f, 1f, 1f), transform.position + Vector3.up * 0.38f);
        Destroy(gameObject);
    }

    private static Transform FindPlayerTransform()
    {
        KaisenController player = FindFirstObjectByType<KaisenController>();
        return player != null ? player.transform : null;
    }
}
