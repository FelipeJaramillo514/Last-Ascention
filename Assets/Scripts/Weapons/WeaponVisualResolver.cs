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
}
