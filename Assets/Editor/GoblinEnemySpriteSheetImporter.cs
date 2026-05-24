using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class GoblinEnemySpriteSheetImporter
{
    private const string SourceFileName = "gobling sprite.png";
    private const string FrameFolder = "Assets/Art/Characters/Enemies/Goblin/Frames";
    private const string ClipFolder = "Assets/Animations/Enemies";
    private const string ControllerPath = "Assets/Animations/Enemies/EnemyBase.controller";
    private const int CellSize = 64;
    private const int Columns = 11;
    private const int PixelsPerUnit = 48;
    private const int FramePadding = 2;

    private struct SequenceDefinition
    {
        public string name;
        public string clipName;
        public int row;
        public int firstColumn;
        public int lastColumn;
        public float frameRate;
        public bool loop;

        public SequenceDefinition(string name, string clipName, int row, int firstColumn, int lastColumn, float frameRate, bool loop)
        {
            this.name = name;
            this.clipName = clipName;
            this.row = row;
            this.firstColumn = firstColumn;
            this.lastColumn = lastColumn;
            this.frameRate = frameRate;
            this.loop = loop;
        }
    }

    [MenuItem("Tools/Last Ascention/Build Goblin Enemy Sprites")]
    public static void BuildGoblinSprites()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string sourcePath = Path.Combine(projectRoot, SourceFileName);
        if (!File.Exists(sourcePath))
        {
            Debug.LogError("Goblin sprite sheet not found at: " + sourcePath);
            return;
        }

        EnsureFolder("Assets/Art", "Characters");
        EnsureFolder("Assets/Art/Characters", "Enemies");
        EnsureFolder("Assets/Art/Characters/Enemies", "Goblin");
        EnsureFolder("Assets/Art/Characters/Enemies/Goblin", "Frames");
        ClearGeneratedAssets();

        Texture2D sourceTexture = LoadTexture(sourcePath);
        Dictionary<string, List<Sprite>> spritesBySequence = new Dictionary<string, List<Sprite>>();
        Dictionary<string, AnimationClip> clipsByState = new Dictionary<string, AnimationClip>();
        SequenceDefinition[] sequences = CreateSequences();

        foreach (SequenceDefinition sequence in sequences)
        {
            List<Sprite> sprites = ExportFrames(sourceTexture, sequence);
            if (sprites.Count == 0)
            {
                Debug.LogWarning("No goblin frames exported for sequence: " + sequence.name);
                continue;
            }

            spritesBySequence[sequence.name] = sprites;
            clipsByState[sequence.name] = CreateClip(sequence, sprites);
        }

        AnimatorController controller = BuildAnimatorController(clipsByState);
        UpdateEnemyPrefabs(controller, spritesBySequence);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Object.DestroyImmediate(sourceTexture);

        int totalSprites = 0;
        foreach (List<Sprite> sprites in spritesBySequence.Values)
        {
            totalSprites += sprites.Count;
        }

        Debug.Log("Built goblin enemy sprites: " + totalSprites + " sprites, " + clipsByState.Count + " clips.");
    }

    private static SequenceDefinition[] CreateSequences()
    {
        return new[]
        {
            new SequenceDefinition("Idle", "Enemy_Idle", 0, 0, Columns - 1, 8f, true),
            new SequenceDefinition("Move", "Enemy_Move", 1, 0, Columns - 1, 10f, true),
            new SequenceDefinition("Attack", "Enemy_Attack", 3, 0, Columns - 1, 12f, false),
            new SequenceDefinition("Death", "Enemy_Death", 4, 0, Columns - 1, 8f, false),
        };
    }

    private static Texture2D LoadTexture(string sourcePath)
    {
        byte[] bytes = File.ReadAllBytes(sourcePath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        ImageConversion.LoadImage(texture, bytes, false);
        texture.name = "GoblinSourceSheet";
        return texture;
    }

    private static List<Sprite> ExportFrames(Texture2D sourceTexture, SequenceDefinition sequence)
    {
        List<Sprite> sprites = new List<Sprite>();
        Color32[] sourcePixels = sourceTexture.GetPixels32();
        int width = sourceTexture.width;
        int height = sourceTexture.height;

        for (int column = sequence.firstColumn; column <= sequence.lastColumn; column++)
        {
            RectInt cell = new RectInt(column * CellSize, sequence.row * CellSize, CellSize, CellSize);
            RectInt bounds = FindVisibleBounds(sourcePixels, width, height, cell);
            if (bounds.width <= 0 || bounds.height <= 0)
            {
                continue;
            }

            bounds = ExpandBounds(bounds, width, height, FramePadding);
            Texture2D frameTexture = new Texture2D(bounds.width, bounds.height, TextureFormat.RGBA32, false);
            Color32[] framePixels = new Color32[bounds.width * bounds.height];

            for (int outY = 0; outY < bounds.height; outY++)
            {
                int sourceY = height - 1 - (bounds.y + bounds.height - 1 - outY);
                for (int outX = 0; outX < bounds.width; outX++)
                {
                    int sourceX = bounds.x + outX;
                    Color32 color = sourcePixels[sourceY * width + sourceX];
                    if (color.a < 8)
                    {
                        color = new Color32(0, 0, 0, 0);
                    }

                    framePixels[outY * bounds.width + outX] = color;
                }
            }

            frameTexture.SetPixels32(framePixels);
            frameTexture.Apply(false, false);

            string assetPath = FrameFolder + "/" + sequence.name + "_" + sprites.Count.ToString("00") + ".png";
            string absolutePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
            File.WriteAllBytes(absolutePath, ImageConversion.EncodeToPNG(frameTexture));
            Object.DestroyImmediate(frameTexture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            ConfigureSpriteImporter(assetPath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                sprites.Add(sprite);
            }
        }

        return sprites;
    }

    private static RectInt FindVisibleBounds(Color32[] pixels, int width, int height, RectInt cell)
    {
        int minX = width;
        int minYTop = height;
        int maxX = -1;
        int maxYTop = -1;

        int maxCellX = Mathf.Min(width, cell.x + cell.width);
        int maxCellYTop = Mathf.Min(height, cell.y + cell.height);
        for (int yTop = cell.y; yTop < maxCellYTop; yTop++)
        {
            int y = height - 1 - yTop;
            for (int x = cell.x; x < maxCellX; x++)
            {
                if (pixels[y * width + x].a < 8)
                {
                    continue;
                }

                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minYTop = Mathf.Min(minYTop, yTop);
                maxYTop = Mathf.Max(maxYTop, yTop);
            }
        }

        if (maxX < minX || maxYTop < minYTop)
        {
            return new RectInt(0, 0, 0, 0);
        }

        return new RectInt(minX, minYTop, maxX - minX + 1, maxYTop - minYTop + 1);
    }

    private static RectInt ExpandBounds(RectInt bounds, int width, int height, int padding)
    {
        int x = Mathf.Max(0, bounds.x - padding);
        int y = Mathf.Max(0, bounds.y - padding);
        int right = Mathf.Min(width, bounds.x + bounds.width + padding);
        int bottom = Mathf.Min(height, bounds.y + bounds.height + padding);
        return new RectInt(x, y, right - x, bottom - y);
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
        settings.spritePivot = new Vector2(0.5f, 0f);
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
    }

    private static AnimationClip CreateClip(SequenceDefinition sequence, List<Sprite> sprites)
    {
        string clipPath = ClipFolder + "/" + sequence.clipName + ".anim";
        AssetDatabase.DeleteAsset(clipPath);

        AnimationClip clip = new AnimationClip
        {
            name = sequence.clipName,
            frameRate = sequence.frameRate
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / sequence.frameRate,
                value = sprites[i]
            };
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = sequence.loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static AnimatorController BuildAnimatorController(Dictionary<string, AnimationClip> clipsByState)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        controller.parameters = new AnimatorControllerParameter[0];
        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine stateMachine = layer.stateMachine;
        ClearStateMachine(stateMachine);

        AnimatorState idle = AddState(stateMachine, "Idle", clipsByState, new Vector3(200f, 0f, 0f));
        AddState(stateMachine, "Move", clipsByState, new Vector3(240f, 80f, 0f));
        AddState(stateMachine, "Attack", clipsByState, new Vector3(280f, 160f, 0f));
        AnimatorState death = AddState(stateMachine, "Death", clipsByState, new Vector3(320f, 240f, 0f));
        death.speed = 0.9f;
        stateMachine.defaultState = idle;
        return controller;
    }

    private static AnimatorState AddState(
        AnimatorStateMachine stateMachine,
        string stateName,
        Dictionary<string, AnimationClip> clipsByState,
        Vector3 position)
    {
        AnimatorState state = stateMachine.AddState(stateName, position);
        AnimationClip clip;
        if (clipsByState.TryGetValue(stateName, out clip))
        {
            state.motion = clip;
        }

        return state;
    }

    private static void ClearStateMachine(AnimatorStateMachine stateMachine)
    {
        ChildAnimatorState[] states = stateMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            stateMachine.RemoveState(states[i].state);
        }

        AnimatorStateTransition[] anyStateTransitions = stateMachine.anyStateTransitions;
        for (int i = 0; i < anyStateTransitions.Length; i++)
        {
            stateMachine.RemoveAnyStateTransition(anyStateTransitions[i]);
        }

        AnimatorTransition[] entryTransitions = stateMachine.entryTransitions;
        for (int i = 0; i < entryTransitions.Length; i++)
        {
            stateMachine.RemoveEntryTransition(entryTransitions[i]);
        }
    }

    private static void UpdateEnemyPrefabs(AnimatorController controller, Dictionary<string, List<Sprite>> spritesBySequence)
    {
        List<Sprite> idleSprites;
        if (!spritesBySequence.TryGetValue("Idle", out idleSprites) || idleSprites.Count == 0)
        {
            return;
        }

        UpdateEnemyPrefab("Assets/Prefabs/Enemies/Goblin.prefab", controller, idleSprites[0]);
        UpdateEnemyPrefab("Assets/Prefabs/Enemies/BeastMinor.prefab", controller, idleSprites[0]);
    }

    private static void UpdateEnemyPrefab(string prefabPath, AnimatorController controller, Sprite defaultSprite)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            SpriteRenderer renderer = prefabRoot.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = prefabRoot.AddComponent<SpriteRenderer>();
            }

            Animator animator = prefabRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = prefabRoot.AddComponent<Animator>();
            }

            renderer.sprite = defaultSprite;
            renderer.color = Color.white;
            renderer.sortingLayerName = "Characters";
            renderer.sortingOrder = 0;
            animator.runtimeAnimatorController = controller;
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
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

    private static void ClearGeneratedAssets()
    {
        DeleteAssetsInFolder(FrameFolder, "png");
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
