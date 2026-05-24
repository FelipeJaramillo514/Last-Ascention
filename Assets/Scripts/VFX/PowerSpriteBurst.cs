using UnityEngine;

public class PowerSpriteBurst : MonoBehaviour
{
    private const float PixelsPerUnit = 512f;

    private SpriteRenderer spriteRenderer;
    private float duration = 0.38f;
    private float elapsed;
    private float startScale = 0.14f;
    private float endScale = 0.28f;
    private Vector3 drift = Vector3.up * 0.45f;
    private Vector3 origin;

    public static void Spawn(string resourcePath, Vector3 position, float scale = 0.28f, float lifetime = 0.38f)
    {
        Sprite sprite = LoadSprite(resourcePath);
        if (sprite == null)
        {
            return;
        }

        GameObject effectObject = new GameObject("PowerSpriteBurst");
        effectObject.transform.position = position;

        SpriteRenderer renderer = effectObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "VFX";
        renderer.sortingOrder = 25;

        PowerSpriteBurst burst = effectObject.AddComponent<PowerSpriteBurst>();
        burst.spriteRenderer = renderer;
        burst.duration = Mathf.Max(0.08f, lifetime);
        burst.endScale = Mathf.Max(0.05f, scale);
        burst.startScale = burst.endScale * 0.45f;
        burst.origin = position;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float eased = 1f - Mathf.Pow(1f - t, 3f);

        transform.position = origin + drift * eased;
        float scale = Mathf.Lerp(startScale, endScale, eased) * (1f + Mathf.Sin(t * Mathf.PI) * 0.12f);
        transform.localScale = new Vector3(scale, scale, 1f);

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f - Mathf.SmoothStep(0.55f, 1f, t);
            spriteRenderer.color = color;
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private static Sprite LoadSprite(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture != null)
        {
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit);
        }

        return Resources.Load<Sprite>(resourcePath);
    }
}
