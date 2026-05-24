using System;
using UnityEngine;

public static class WeaponVisualResolver
{
    private const float IconPixelsPerUnit = 1024f;
    private const float ProjectilePixelsPerUnit = 512f;
    private const string BluePowerPath = "PowerSprites/PowerBlue";
    private const string OrangePowerPath = "PowerSprites/PowerOrange";
    private const string BlueAuraProjectilePath = "KnifeAura/Projectile";
    private const string OrangeFireballPath = "OrangeFireball/FireballSheet";
    private const int FireballColumns = 3;
    private const int FireballRows = 3;

    private static Sprite bluePowerIcon;
    private static Sprite orangePowerIcon;
    private static Sprite[] blueAuraProjectileFrames;
    private static Sprite[] orangeFireballFrames;
    private static Sprite wornKnifeIcon;
    private static Sprite stoneGauntletsIcon;

    public static Sprite GetWeaponIcon(WeaponData weaponData)
    {
        if (weaponData == null)
        {
            return null;
        }

        if (IsBluePower(weaponData))
        {
            return LoadFullTextureSprite(BluePowerPath, ref bluePowerIcon, IconPixelsPerUnit) ?? weaponData.weaponIcon;
        }

        if (IsOrangePower(weaponData))
        {
            return LoadFullTextureSprite(OrangePowerPath, ref orangePowerIcon, IconPixelsPerUnit) ?? weaponData.weaponIcon;
        }

        if (IsStoneGauntlets(weaponData))
        {
            return ResolveIconOrFallback(weaponData.weaponIcon, GetStoneGauntletsIcon);
        }

        if (IsWornBlade(weaponData))
        {
            return ResolveIconOrFallback(weaponData.weaponIcon, GetWornKnifeIcon);
        }

        return weaponData.weaponIcon;
    }

    public static Sprite[] GetProjectileFrames(WeaponData weaponData)
    {
        if (weaponData != null && IsBluePower(weaponData))
        {
            return GetBlueAuraProjectileFrames();
        }

        if (weaponData != null && IsOrangePower(weaponData))
        {
            return GetOrangeFireballFrames();
        }

        return weaponData != null ? weaponData.animationFrames : null;
    }

    public static bool IsPowerWeapon(WeaponData weaponData)
    {
        return IsBluePower(weaponData) || IsOrangePower(weaponData);
    }

    public static bool IsBluePower(WeaponData weaponData)
    {
        string id = GetWeaponId(weaponData);
        return id.Contains("runic")
            || id.Contains("cristal")
            || id.Contains("azul")
            || id.Contains("blue");
    }

    public static bool IsOrangePower(WeaponData weaponData)
    {
        string id = GetWeaponId(weaponData);
        return id.Contains("orange")
            || id.Contains("naranja")
            || id.Contains("fuego")
            || id.Contains("fire");
    }

    private static bool IsStoneGauntlets(WeaponData weaponData)
    {
        string id = GetWeaponId(weaponData);
        return id.Contains("stone")
            || id.Contains("piedra")
            || id.Contains("gauntlet")
            || id.Contains("guantes");
    }

    private static bool IsWornBlade(WeaponData weaponData)
    {
        string id = GetWeaponId(weaponData);
        return id.Contains("worn")
            || id.Contains("desgast")
            || id.Contains("cuchillo")
            || id.Contains("knife")
            || id.Contains("sword")
            || id.Contains("espada");
    }

    private static string GetWeaponId(WeaponData weaponData)
    {
        if (weaponData == null)
        {
            return string.Empty;
        }

        return ((weaponData.weaponName ?? string.Empty) + " " + weaponData.name).ToLowerInvariant();
    }

    private static Sprite[] GetBlueAuraProjectileFrames()
    {
        if (blueAuraProjectileFrames == null)
        {
            blueAuraProjectileFrames = LoadOrderedSprites(BlueAuraProjectilePath);
        }

        return blueAuraProjectileFrames;
    }

    private static Sprite[] GetOrangeFireballFrames()
    {
        if (orangeFireballFrames != null)
        {
            return orangeFireballFrames;
        }

        Texture2D texture = Resources.Load<Texture2D>(OrangeFireballPath);
        if (texture == null)
        {
            orangeFireballFrames = new Sprite[0];
            return orangeFireballFrames;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        int frameWidth = texture.width / FireballColumns;
        int frameHeight = texture.height / FireballRows;
        int projectileFrameCount = 5;
        orangeFireballFrames = new Sprite[projectileFrameCount];
        for (int i = 0; i < projectileFrameCount; i++)
        {
            int row = i / FireballColumns;
            int column = i % FireballColumns;
            Rect rect = new Rect(
                column * frameWidth,
                texture.height - ((row + 1) * frameHeight),
                frameWidth,
                frameHeight);
            orangeFireballFrames[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), ProjectilePixelsPerUnit);
        }

        return orangeFireballFrames;
    }

    private static Sprite[] LoadOrderedSprites(string resourcePath)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
        if (sprites == null || sprites.Length == 0)
        {
            return new Sprite[0];
        }

        Array.Sort(sprites, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
        return sprites;
    }

    private static Sprite LoadFullTextureSprite(string resourcePath, ref Sprite cachedSprite, float pixelsPerUnit)
    {
        if (cachedSprite != null)
        {
            return cachedSprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture != null)
        {
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            cachedSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            return cachedSprite;
        }

        cachedSprite = Resources.Load<Sprite>(resourcePath);
        return cachedSprite;
    }

    private static Sprite ResolveIconOrFallback(Sprite assetIcon, Func<Sprite> fallbackFactory)
    {
        if (!ShouldUseGeneratedIcon(assetIcon))
        {
            return assetIcon;
        }

        return fallbackFactory != null ? fallbackFactory() : assetIcon;
    }

    private static bool ShouldUseGeneratedIcon(Sprite assetIcon)
    {
        if (assetIcon == null)
        {
            return true;
        }

        return assetIcon.rect.width <= 16f && assetIcon.rect.height <= 16f;
    }

    private static Sprite GetWornKnifeIcon()
    {
        if (wornKnifeIcon != null)
        {
            return wornKnifeIcon;
        }

        Texture2D texture = CreateIconTexture("GeneratedWornKnifeIcon");
        Color outline = new Color32(28, 32, 42, 255);
        Color bladeDark = new Color32(90, 102, 118, 255);
        Color bladeMid = new Color32(155, 168, 185, 255);
        Color bladeLight = new Color32(224, 232, 240, 255);
        Color handleDark = new Color32(58, 38, 28, 255);
        Color handleMid = new Color32(112, 74, 44, 255);
        Color wrap = new Color32(178, 138, 86, 255);

        DrawThickLine(texture, 20, 45, 49, 16, 8, outline);
        DrawThickLine(texture, 22, 43, 50, 15, 5, bladeDark);
        DrawThickLine(texture, 25, 40, 49, 16, 3, bladeMid);
        DrawThickLine(texture, 31, 34, 47, 18, 1, bladeLight);
        DrawThickLine(texture, 19, 46, 10, 55, 8, outline);
        DrawThickLine(texture, 19, 46, 10, 55, 5, handleDark);
        DrawThickLine(texture, 18, 47, 11, 54, 2, handleMid);
        DrawThickLine(texture, 15, 49, 24, 40, 3, outline);
        DrawThickLine(texture, 16, 49, 24, 41, 1, wrap);
        FillRect(texture, 39, 25, 2, 2, outline);
        FillRect(texture, 43, 20, 2, 1, outline);
        FillRect(texture, 28, 37, 2, 1, outline);
        FillRect(texture, 9, 54, 3, 2, new Color32(34, 23, 18, 255));
        FillRect(texture, 5, 58, 4, 2, new Color32(0, 205, 255, 96));
        FillRect(texture, 45, 13, 3, 1, new Color32(255, 167, 66, 120));

        texture.Apply();
        wornKnifeIcon = Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 64f);
        return wornKnifeIcon;
    }

    private static Sprite GetStoneGauntletsIcon()
    {
        if (stoneGauntletsIcon != null)
        {
            return stoneGauntletsIcon;
        }

        Texture2D texture = CreateIconTexture("GeneratedStoneGauntletsIcon");
        Color outline = new Color32(30, 28, 34, 255);
        Color stoneDark = new Color32(72, 72, 78, 255);
        Color stoneMid = new Color32(122, 118, 112, 255);
        Color stoneLight = new Color32(164, 154, 140, 255);
        Color rune = new Color32(43, 222, 255, 230);

        DrawGauntlet(texture, 15, 16, false, outline, stoneDark, stoneMid, stoneLight, rune);
        DrawGauntlet(texture, 36, 16, true, outline, stoneDark, stoneMid, stoneLight, rune);
        DrawThickLine(texture, 19, 49, 26, 54, 3, new Color32(48, 42, 38, 220));
        DrawThickLine(texture, 44, 49, 37, 54, 3, new Color32(48, 42, 38, 220));
        DrawDisc(texture, 31, 49, 3, new Color32(0, 204, 255, 72));
        FillRect(texture, 30, 46, 4, 8, rune);
        FillRect(texture, 24, 23, 2, 9, rune);
        FillRect(texture, 41, 23, 2, 9, rune);

        texture.Apply();
        stoneGauntletsIcon = Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 64f);
        return stoneGauntletsIcon;
    }

    private static Texture2D CreateIconTexture(string name)
    {
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        return texture;
    }

    private static void DrawGauntlet(Texture2D texture, int originX, int originY, bool mirrored, Color outline, Color dark, Color mid, Color light, Color rune)
    {
        int direction = mirrored ? -1 : 1;
        FillRect(texture, originX - 2, originY + 13, 18, 22, outline);
        FillRect(texture, originX, originY + 15, 14, 18, dark);
        FillRect(texture, originX + 2, originY + 20, 12, 11, mid);
        FillRect(texture, originX + 4, originY + 27, 7, 3, light);

        for (int i = 0; i < 4; i++)
        {
            int fingerX = originX + (mirrored ? 12 - (i * 4) : i * 4);
            FillRect(texture, fingerX - 1, originY + 33, 5, 9, outline);
            FillRect(texture, fingerX, originY + 34, 3, 7, mid);
            FillRect(texture, fingerX, originY + 39, 2, 1, light);
        }

        FillRect(texture, originX + (direction > 0 ? 12 : -4), originY + 19, 7, 10, outline);
        FillRect(texture, originX + (direction > 0 ? 13 : -3), originY + 20, 5, 8, mid);
        DrawThickLine(texture, originX + 4, originY + 18, originX + 11, originY + 26, 1, rune);
        FillRect(texture, originX + 8, originY + 21, 2, 2, new Color32(8, 35, 46, 255));
    }

    private static void DrawThickLine(Texture2D texture, int x0, int y0, int x1, int y1, int thickness, Color color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int steps = Mathf.Max(dx, dy);
        if (steps == 0)
        {
            FillRect(texture, x0, y0, thickness, thickness, color);
            return;
        }

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
            int half = Mathf.Max(0, thickness / 2);
            FillRect(texture, x - half, y - half, thickness, thickness, color);
        }
    }

    private static void DrawDisc(Texture2D texture, int centerX, int centerY, int radius, Color color)
    {
        int radiusSquared = radius * radius;
        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                if ((dx * dx) + (dy * dy) <= radiusSquared)
                {
                    SetPixelSafe(texture, x, y, color);
                }
            }
        }
    }

    private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int py = y; py < y + height; py++)
        {
            for (int px = x; px < x + width; px++)
            {
                SetPixelSafe(texture, px, py, color);
            }
        }
    }

    private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
        {
            return;
        }

        texture.SetPixel(x, y, color);
    }
}
