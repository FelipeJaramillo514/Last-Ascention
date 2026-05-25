using System.Collections.Generic;
using UnityEngine;

internal static class ExperienceSpriteCache
{
    private const string OrbSheetPath = "Experience/XPOrbs";
    private const string DeathBurstSheetPath = "Experience/XPDeathBurst";
    private const string CollectBurstSheetPath = "Experience/XPCollectBurst";
    private const float PixelsPerUnit = 512f;

    private static Sprite[] orbFrames;
    private static Sprite[] deathBurstFrames;
    private static Sprite[] collectBurstFrames;
    private static Sprite fallbackOrb;
    private static Sprite fallbackBurst;

    public static Sprite[] OrbFrames
    {
        get
        {
            if (orbFrames == null)
            {
                orbFrames = LoadFrames(OrbSheetPath, 4, 4, 8);
                if (orbFrames.Length == 0)
                {
                    orbFrames = new[] { FallbackOrb };
                }
            }

            return orbFrames;
        }
    }

    public static Sprite[] DeathBurstFrames
    {
        get
        {
            if (deathBurstFrames == null)
            {
                deathBurstFrames = LoadFrames(DeathBurstSheetPath, 4, 4, 12);
                if (deathBurstFrames.Length == 0)
                {
                    deathBurstFrames = new[] { FallbackBurst };
                }
            }

            return deathBurstFrames;
        }
    }

    public static Sprite[] CollectBurstFrames
    {
        get
        {
            if (collectBurstFrames == null)
            {
                collectBurstFrames = LoadFrames(CollectBurstSheetPath, 8, 1, 8);
                if (collectBurstFrames.Length == 0)
                {
                    collectBurstFrames = new[] { FallbackBurst };
                }
            }

            return collectBurstFrames;
        }
    }

    private static Sprite FallbackOrb
    {
        get
        {
            if (fallbackOrb == null)
            {
                fallbackOrb = CreateOrbSprite();
            }

            return fallbackOrb;
        }
    }

    private static Sprite FallbackBurst
    {
        get
        {
            if (fallbackBurst == null)
            {
                fallbackBurst = CreateBurstSprite();
            }

            return fallbackBurst;
        }
    }

    private static Sprite[] LoadFrames(string resourcePath, int columns, int rows, int maxFrames)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null || columns <= 0 || rows <= 0)
        {
            return new Sprite[0];
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        int frameWidth = Mathf.Max(1, texture.width / columns);
        int frameHeight = Mathf.Max(1, texture.height / rows);
        int totalFrames = columns * rows;
        int frameLimit = maxFrames > 0 ? Mathf.Min(maxFrames, totalFrames) : totalFrames;
        List<Sprite> frames = new List<Sprite>(frameLimit);

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int frameIndex = row * columns + column;
                if (frameIndex >= frameLimit)
                {
                    return frames.ToArray();
                }

                Rect rect = new Rect(
                    column * frameWidth,
                    texture.height - ((row + 1) * frameHeight),
                    frameWidth,
                    frameHeight);

                frames.Add(Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), PixelsPerUnit));
            }
        }

        return frames.ToArray();
    }

    private static Sprite CreateOrbSprite()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.48f);
                if (distance > 1f)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                Color color = distance < 0.42f
                    ? Color.Lerp(new Color(0.95f, 1f, 1f, 1f), new Color(0.1f, 0.9f, 1f, 0.95f), distance / 0.42f)
                    : Color.Lerp(new Color(0.18f, 0.55f, 1f, 0.85f), new Color(0.38f, 0.12f, 1f, 0f), Mathf.InverseLerp(0.42f, 1f, distance));

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    private static Sprite CreateBurstSprite()
    {
        const int size = 48;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 offset = new Vector2(x, y) - center;
                float anglePulse = Mathf.Abs(Mathf.Sin(Mathf.Atan2(offset.y, offset.x) * 4f));
                float distance = offset.magnitude / (size * 0.5f);
                float alpha = Mathf.Clamp01((1f - distance) * anglePulse);
                Color color = Color.Lerp(new Color(0.2f, 0.5f, 1f, 0f), new Color(0.55f, 1f, 1f, 1f), alpha);
                texture.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
    }
}
