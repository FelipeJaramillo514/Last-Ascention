using UnityEngine;

public class PutridAcolyteBolt : MonoBehaviour
{
    private const float Lifetime = 2.8f;
    private static Sprite cachedBoltSprite;

    private Vector2 direction;
    private Vector2 origin;
    private float damage;
    private float speed;
    private float maxDistance;
    private float lifeTimer;

    public static void Spawn(Vector2 position, Vector2 direction, float damage, float speed, float maxDistance)
    {
        GameObject boltObject = new GameObject("PutridAcolyteBolt");
        boltObject.transform.position = position;
        boltObject.transform.right = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;

        SpriteRenderer renderer = boltObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetBoltSprite();
        renderer.color = new Color32(75, 236, 255, 255);
        renderer.sortingLayerName = "Projectiles";
        renderer.sortingOrder = 4;

        CircleCollider2D collider = boltObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.18f;

        Rigidbody2D body = boltObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        PutridAcolyteBolt bolt = boltObject.AddComponent<PutridAcolyteBolt>();
        bolt.Configure(position, direction, damage, speed, maxDistance);
    }

    private void Configure(Vector2 position, Vector2 launchDirection, float damageValue, float speedValue, float maxDistanceValue)
    {
        origin = position;
        direction = launchDirection.sqrMagnitude > 0.001f ? launchDirection.normalized : Vector2.right;
        damage = Mathf.Max(0f, damageValue);
        speed = Mathf.Max(0.1f, speedValue);
        maxDistance = Mathf.Max(0.5f, maxDistanceValue);
        lifeTimer = Lifetime;
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        lifeTimer -= Time.deltaTime;

        if (lifeTimer <= 0f || Vector2.Distance(origin, transform.position) >= maxDistance)
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

        if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            Destroy(gameObject);
            return;
        }

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        ShadowSoldier shadow = other.GetComponentInParent<ShadowSoldier>();
        if (shadow != null)
        {
            shadow.TakeDamage(damage * 0.7f);
            Destroy(gameObject);
            return;
        }

        CoverObject cover = other.GetComponentInParent<CoverObject>();
        if (cover != null)
        {
            cover.TakeDamage(damage, direction);
            Destroy(gameObject);
        }
    }

    private static Sprite GetBoltSprite()
    {
        if (cachedBoltSprite != null)
        {
            return cachedBoltSprite;
        }

        Texture2D texture = new Texture2D(24, 12, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Vector2 center = new Vector2(8f, 5.5f);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - distance / 7.5f);
                float tail = Mathf.Clamp01(1f - Mathf.Abs(y - center.y) / 4.5f) * Mathf.Clamp01((texture.width - x) / 18f);
                alpha = Mathf.Max(alpha, tail * 0.72f);
                Color color = Color.Lerp(new Color(0.12f, 0.38f, 0.72f, alpha), new Color(0.72f, 1f, 1f, alpha), Mathf.Clamp01(alpha));
                texture.SetPixel(x, y, alpha <= 0.04f ? Color.clear : color);
            }
        }

        texture.Apply();
        cachedBoltSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.72f, 0.5f), 48f);
        cachedBoltSprite.name = "PutridAcolyteBolt";
        return cachedBoltSprite;
    }
}
