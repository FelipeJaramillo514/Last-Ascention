using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class OrangeFireballProjectile : MonoBehaviour
{
    private readonly HashSet<EnemyBase> damagedEnemies = new HashSet<EnemyBase>();

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;
    private Rigidbody2D body;
    private Sprite[] projectileFrames;
    private Vector2 direction = Vector2.right;
    private float damage = 14f;
    private float speed = 8f;
    private float range = 8f;
    private float explosionRadius = 1.8f;
    private float distanceTravelled;
    private float animationTimer;
    private bool hasExploded;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();
        body = GetComponent<Rigidbody2D>();

        circleCollider.isTrigger = true;
        circleCollider.radius = 0.28f;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;

        spriteRenderer.sortingLayerName = "Projectiles";
        spriteRenderer.sortingOrder = 9;
        projectileFrames = FireballSpriteCache.ProjectileFrames;
        spriteRenderer.sprite = projectileFrames != null && projectileFrames.Length > 0
            ? projectileFrames[0]
            : FireballSpriteCache.Sprite;
    }

    public void Fire(Vector2 spawnPosition, Vector2 fireDirection, float damageValue, float projectileSpeed, float projectileRange, float radius)
    {
        transform.position = spawnPosition;
        direction = fireDirection.sqrMagnitude > 0.001f ? fireDirection.normalized : Vector2.right;
        transform.right = direction;
        damage = Mathf.Max(1f, damageValue);
        speed = Mathf.Max(1f, projectileSpeed);
        range = Mathf.Max(1f, projectileRange);
        explosionRadius = Mathf.Max(0.4f, radius);
        distanceTravelled = 0f;
        animationTimer = 0f;
        hasExploded = false;
        damagedEnemies.Clear();
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        distanceTravelled += step;

        float pulse = 1f + Mathf.Sin(Time.time * 18f) * 0.08f;
        transform.localScale = new Vector3(pulse, pulse, 1f);
        animationTimer += Time.deltaTime;
        if (spriteRenderer != null && projectileFrames != null && projectileFrames.Length > 0)
        {
            int frameIndex = Mathf.FloorToInt(animationTimer * 12f) % projectileFrames.Length;
            spriteRenderer.sprite = projectileFrames[frameIndex];
        }

        if (distanceTravelled >= range)
        {
            Explode();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded || other == null)
        {
            return;
        }

        if (other.GetComponentInParent<EnemyBase>() != null
            || other.CompareTag("Cover")
            || other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (hasExploded)
        {
            return;
        }

        hasExploded = true;
        Vector2 origin = transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, explosionRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();
            if (enemy != null && !enemy.IsDead && damagedEnemies.Add(enemy))
            {
                Vector2 knockDirection = ((Vector2)enemy.transform.position - origin).normalized;
                enemy.TakeDamage(damage, knockDirection.sqrMagnitude > 0.001f ? knockDirection : direction);
                enemy.ApplyKnockback(knockDirection, 2.8f);
                continue;
            }

            CoverObject cover = hit.GetComponentInParent<CoverObject>();
            if (cover != null)
            {
                Vector2 coverDirection = ((Vector2)cover.transform.position - origin).normalized;
                cover.TakeDamage(damage * 0.8f, coverDirection.sqrMagnitude > 0.001f ? coverDirection : direction);
            }
        }

        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayExplosion(transform.position);
        }

        OrangeFireballImpact.Spawn(transform.position, explosionRadius);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCue(AudioCueId.CrystalFire, transform.position, 0.8f, 0.78f, true, 0.65f);
        }

        Destroy(gameObject);
    }
}

internal class OrangeFireballImpact : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private float duration = 0.22f;
    private float elapsed;
    private float targetScale;

    public static void Spawn(Vector3 position, float radius)
    {
        GameObject effectObject = new GameObject("OrangeFireballImpact");
        effectObject.transform.position = position;
        OrangeFireballImpact effect = effectObject.AddComponent<OrangeFireballImpact>();
        effect.targetScale = Mathf.Max(0.5f, radius * 2f);
    }

    private void Awake()
    {
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = "VFX";
        spriteRenderer.sortingOrder = 22;
        frames = FireballSpriteCache.ImpactFrames;
        spriteRenderer.sprite = frames != null && frames.Length > 0
            ? frames[0]
            : FireballSpriteCache.ImpactSprite;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        transform.localScale = Vector3.one * Mathf.Lerp(0.35f, targetScale, t);
        if (spriteRenderer != null)
        {
            if (frames != null && frames.Length > 0)
            {
                int frameIndex = Mathf.Clamp(Mathf.FloorToInt(t * frames.Length), 0, frames.Length - 1);
                spriteRenderer.sprite = frames[frameIndex];
            }

            Color color = spriteRenderer.color;
            color.a = 1f - t;
            spriteRenderer.color = color;
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}

internal static class FireballSpriteCache
{
    private static Sprite fireballSprite;
    private static Sprite impactSprite;
    private static Sprite[] sheetFrames;
    private static Sprite[] projectileFrames;
    private static Sprite[] impactFrames;
    private const string SheetResourcePath = "OrangeFireball/FireballSheet";
    private const int SheetColumns = 3;
    private const int SheetRows = 3;
    private const float SheetPixelsPerUnit = 512f;

    public static Sprite[] ProjectileFrames
    {
        get
        {
            if (projectileFrames == null)
            {
                Sprite[] frames = SheetFrames;
                if (frames != null && frames.Length >= 5)
                {
                    projectileFrames = new[] { frames[0], frames[1], frames[2], frames[3], frames[4] };
                }
                else
                {
                    projectileFrames = new[] { Sprite };
                }
            }

            return projectileFrames;
        }
    }

    public static Sprite[] ImpactFrames
    {
        get
        {
            if (impactFrames == null)
            {
                Sprite[] frames = SheetFrames;
                if (frames != null && frames.Length >= 9)
                {
                    impactFrames = new[] { frames[5], frames[6], frames[7], frames[8] };
                }
                else
                {
                    impactFrames = new[] { ImpactSprite };
                }
            }

            return impactFrames;
        }
    }

    public static Sprite Sprite
    {
        get
        {
            if (fireballSprite == null)
            {
                Sprite[] frames = SheetFrames;
                fireballSprite = frames != null && frames.Length > 0
                    ? frames[0]
                    : CreateRadialSprite(32, new Color32(255, 245, 170, 255), new Color32(255, 105, 18, 230), new Color32(120, 32, 4, 0), 100f);
            }

            return fireballSprite;
        }
    }

    public static Sprite ImpactSprite
    {
        get
        {
            if (impactSprite == null)
            {
                Sprite[] frames = SheetFrames;
                impactSprite = frames != null && frames.Length > 5
                    ? frames[5]
                    : CreateRadialSprite(48, new Color32(255, 220, 90, 180), new Color32(255, 92, 8, 100), new Color32(255, 28, 0, 0), 100f);
            }

            return impactSprite;
        }
    }

    private static Sprite[] SheetFrames
    {
        get
        {
            if (sheetFrames == null)
            {
                Texture2D sheet = Resources.Load<Texture2D>(SheetResourcePath);
                if (sheet == null)
                {
                    sheetFrames = new Sprite[0];
                }
                else
                {
                    sheetFrames = SliceSheet(sheet);
                }
            }

            return sheetFrames;
        }
    }

    private static Sprite[] SliceSheet(Texture2D sheet)
    {
        int frameWidth = sheet.width / SheetColumns;
        int frameHeight = sheet.height / SheetRows;
        Sprite[] frames = new Sprite[SheetColumns * SheetRows];
        for (int row = 0; row < SheetRows; row++)
        {
            for (int column = 0; column < SheetColumns; column++)
            {
                int frameIndex = row * SheetColumns + column;
                float x = column * frameWidth;
                float y = sheet.height - ((row + 1) * frameHeight);
                Rect rect = new Rect(x, y, frameWidth, frameHeight);
                frames[frameIndex] = Sprite.Create(sheet, rect, new Vector2(0.5f, 0.5f), SheetPixelsPerUnit);
            }
        }

        return frames;
    }

    private static Sprite CreateRadialSprite(int size, Color32 core, Color32 mid, Color32 edge, float pixelsPerUnit)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float normalizedDistance = Vector2.Distance(new Vector2(x, y), center) / radius;
                Color color = normalizedDistance < 0.38f
                    ? Color.Lerp(core, mid, normalizedDistance / 0.38f)
                    : Color.Lerp(mid, edge, Mathf.InverseLerp(0.38f, 1f, normalizedDistance));

                texture.SetPixel(x, y, normalizedDistance <= 1f ? color : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }
}
