using UnityEngine;

public class ExperienceSpriteEffect : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private Vector3 origin;
    private Vector3 drift;
    private float duration = 0.35f;
    private float elapsed;
    private float startScale = 0.6f;
    private float endScale = 1f;

    public static void PlayDeathBurst(Vector3 position)
    {
        Spawn(
            "XPDeathBurstVFX",
            ExperienceSpriteCache.DeathBurstFrames,
            position,
            0.7f,
            1.12f,
            0.48f,
            Vector3.up * 0.08f,
            24);
    }

    public static void PlayCollectBurst(Vector3 position)
    {
        Spawn(
            "XPCollectBurstVFX",
            ExperienceSpriteCache.CollectBurstFrames,
            position,
            0.42f,
            0.82f,
            0.34f,
            Vector3.up * 0.16f,
            28);
    }

    private static void Spawn(string objectName, Sprite[] sourceFrames, Vector3 position, float start, float end, float lifetime, Vector3 driftValue, int sortingOrder)
    {
        if (sourceFrames == null || sourceFrames.Length == 0)
        {
            return;
        }

        GameObject effectObject = new GameObject(objectName);
        effectObject.transform.position = position;

        SpriteRenderer renderer = effectObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sourceFrames[0];
        renderer.sortingLayerName = "VFX";
        renderer.sortingOrder = sortingOrder;

        ExperienceSpriteEffect effect = effectObject.AddComponent<ExperienceSpriteEffect>();
        effect.spriteRenderer = renderer;
        effect.frames = sourceFrames;
        effect.origin = position;
        effect.startScale = Mathf.Max(0.05f, start);
        effect.endScale = Mathf.Max(effect.startScale, end);
        effect.duration = Mathf.Max(0.08f, lifetime);
        effect.drift = driftValue;
        effect.transform.localScale = Vector3.one * effect.startScale;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float eased = 1f - Mathf.Pow(1f - t, 3f);

        transform.position = origin + drift * eased;
        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, eased);

        if (spriteRenderer != null && frames != null && frames.Length > 0)
        {
            int frameIndex = Mathf.Clamp(Mathf.FloorToInt(t * frames.Length), 0, frames.Length - 1);
            spriteRenderer.sprite = frames[frameIndex];

            Color color = spriteRenderer.color;
            color.a = 1f - Mathf.SmoothStep(0.72f, 1f, t);
            spriteRenderer.color = color;
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
