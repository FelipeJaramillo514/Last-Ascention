using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class KnifeAuraSpriteSheetImporter
{
    private const string SourceAssetPath = "Assets/Art/Player/Aura/sprite aura.png";
    private const string ResourcesRoot = "Assets/Resources/KnifeAura";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const int PixelsPerUnit = 48;
    private const int FramePadding = 4;

    private struct FrameRect
    {
        public string group;
        public int x;
        public int y;
        public int width;
        public int height;

        public FrameRect(string group, int x, int y, int width, int height)
        {
            this.group = group;
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
    }

    [MenuItem("Tools/Last Ascention/Build Knife Aura Sprites")]
    public static void BuildKnifeAuraSprites()
    {
        if (!File.Exists(SourceAssetPath))
        {
            Debug.LogError("Knife aura sprite sheet not found at: " + SourceAssetPath);
            return;
        }

        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "KnifeAura");
        EnsureFolder(ResourcesRoot, "Projectile");
        EnsureFolder(ResourcesRoot, "Impact");
        EnsureFolder(ResourcesRoot, "Trail");
        ClearGeneratedFrames();

        Texture2D sourceTexture = LoadTexture(SourceAssetPath);
        FrameRect[] frames = CreateFrameRects();
        Dictionary<string, int> countersByGroup = new Dictionary<string, int>();
        foreach (FrameRect frame in frames)
        {
            int index = 0;
            countersByGroup.TryGetValue(frame.group, out index);
            ExportFrame(sourceTexture, frame, index);
            countersByGroup[frame.group] = index + 1;
        }

        Object.DestroyImmediate(sourceTexture);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AttachAuraAttackToPlayers();
        Debug.Log("Built knife aura sprites from sprite aura.png.");
    }

    private static FrameRect[] CreateFrameRects()
    {
        return new[]
        {
            new FrameRect("Projectile", 45, 12, 175, 112),
            new FrameRect("Projectile", 275, 12, 185, 112),
            new FrameRect("Projectile", 505, 12, 205, 112),
            new FrameRect("Projectile", 730, 12, 215, 112),
            new FrameRect("Projectile", 980, 12, 195, 112),
            new FrameRect("Projectile", 1200, 12, 190, 112),

            new FrameRect("Impact", 180, 188, 170, 150),
            new FrameRect("Impact", 360, 188, 190, 150),
            new FrameRect("Impact", 550, 188, 180, 150),
            new FrameRect("Impact", 735, 188, 170, 150),
            new FrameRect("Impact", 915, 188, 165, 150),

            new FrameRect("Trail", 0, 405, 190, 132),
            new FrameRect("Trail", 185, 405, 190, 132),
            new FrameRect("Trail", 370, 405, 190, 132),
            new FrameRect("Trail", 555, 405, 185, 132),
            new FrameRect("Trail", 735, 405, 180, 132),
            new FrameRect("Trail", 925, 405, 180, 132),
            new FrameRect("Trail", 1110, 405, 175, 132),
            new FrameRect("Trail", 1265, 405, 142, 132),
        };
    }

    private static Texture2D LoadTexture(string assetPath)
    {
        byte[] bytes = File.ReadAllBytes(assetPath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        ImageConversion.LoadImage(texture, bytes, false);
        texture.name = "KnifeAuraSourceSheet";
        return texture;
    }

    private static void ExportFrame(Texture2D sourceTexture, FrameRect frame, int index)
    {
        Color32[] sourcePixels = sourceTexture.GetPixels32();
        int sourceWidth = sourceTexture.width;
        int sourceHeight = sourceTexture.height;
        Color32[] croppedPixels = new Color32[frame.width * frame.height];

        for (int outY = 0; outY < frame.height; outY++)
        {
            int sourceTop = frame.y + frame.height - 1 - outY;
            int sourceY = sourceHeight - 1 - sourceTop;
            for (int outX = 0; outX < frame.width; outX++)
            {
                int sourceX = frame.x + outX;
                Color32 color = sourcePixels[sourceY * sourceWidth + sourceX];
                if (IsCheckerBackground(color))
                {
                    color = new Color32(0, 0, 0, 0);
                }

                croppedPixels[outY * frame.width + outX] = color;
            }
        }

        RectInt visibleBounds = FindVisibleBounds(croppedPixels, frame.width, frame.height);
        if (visibleBounds.width <= 0 || visibleBounds.height <= 0)
        {
            Debug.LogWarning("Skipping empty knife aura frame: " + frame.group + " " + index);
            return;
        }

        visibleBounds = ExpandBounds(visibleBounds, frame.width, frame.height, FramePadding);
        Texture2D frameTexture = new Texture2D(visibleBounds.width, visibleBounds.height, TextureFormat.RGBA32, false);
        Color32[] framePixels = new Color32[visibleBounds.width * visibleBounds.height];
        for (int y = 0; y < visibleBounds.height; y++)
        {
            for (int x = 0; x < visibleBounds.width; x++)
            {
                int sourceX = visibleBounds.x + x;
                int sourceY = visibleBounds.y + y;
                framePixels[y * visibleBounds.width + x] = croppedPixels[sourceY * frame.width + sourceX];
            }
        }

        frameTexture.SetPixels32(framePixels);
        frameTexture.Apply(false, false);

        string assetPath = ResourcesRoot + "/" + frame.group + "/" + frame.group + "_" + index.ToString("00") + ".png";
        string absolutePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        File.WriteAllBytes(absolutePath, ImageConversion.EncodeToPNG(frameTexture));
        Object.DestroyImmediate(frameTexture);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        ConfigureSpriteImporter(assetPath);
    }

    private static RectInt FindVisibleBounds(Color32[] pixels, int width, int height)
    {
        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a < 8)
                {
                    continue;
                }

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return new RectInt(0, 0, 0, 0);
        }

        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private static RectInt ExpandBounds(RectInt bounds, int width, int height, int padding)
    {
        int x = Mathf.Max(0, bounds.x - padding);
        int y = Mathf.Max(0, bounds.y - padding);
        int right = Mathf.Min(width, bounds.x + bounds.width + padding);
        int top = Mathf.Min(height, bounds.y + bounds.height + padding);
        return new RectInt(x, y, right - x, top - y);
    }

    private static bool IsCheckerBackground(Color32 color)
    {
        if (color.a < 8)
        {
            return true;
        }

        int max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        int min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        return max - min <= 8 && max >= 45 && max <= 145;
    }

    private static void ConfigureSpriteImporter(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 0.5f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static void AttachAuraAttackToPlayers()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            if (prefabRoot.GetComponent<KnifeAuraAttack>() == null)
            {
                prefabRoot.AddComponent<KnifeAuraAttack>();
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        KaisenController[] scenePlayers = Object.FindObjectsByType<KaisenController>(FindObjectsSortMode.None);
        for (int i = 0; i < scenePlayers.Length; i++)
        {
            if (scenePlayers[i] != null && scenePlayers[i].GetComponent<KnifeAuraAttack>() == null)
            {
                scenePlayers[i].gameObject.AddComponent<KnifeAuraAttack>();
                EditorUtility.SetDirty(scenePlayers[i].gameObject);
            }
        }
    }

    private static void EnsureFolder(string parent, string folder)
    {
        string path = parent + "/" + folder;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    private static void ClearGeneratedFrames()
    {
        DeleteAssetsInFolder(ResourcesRoot + "/Projectile", "png");
        DeleteAssetsInFolder(ResourcesRoot + "/Impact", "png");
        DeleteAssetsInFolder(ResourcesRoot + "/Trail", "png");
    }

    private static void DeleteAssetsInFolder(string folder, string extension)
    {
        string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (Path.GetExtension(assetPath).TrimStart('.').ToLowerInvariant() == extension)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }
    }
}
