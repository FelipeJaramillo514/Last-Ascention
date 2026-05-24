using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PlayerSpriteSheetImporter
{
    private const string SourceFileName = "sprites.png";
    private const string FrameFolder = "Assets/Art/Characters/Player/Kaisen/Frames";
    private const string ClipFolder = "Assets/Animations/Player";
    private const string ControllerPath = "Assets/Animations/KaisenPlayer.controller";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const int PixelsPerUnit = 48;
    private const int FramePadding = 3;

    private struct SequenceDefinition
    {
        public string name;
        public int top;
        public int bottom;
        public int left;
        public int right;
        public int mergePadding;
        public int minWidth;
        public int minHeight;
        public float frameRate;
        public bool loop;

        public SequenceDefinition(
            string name,
            int top,
            int bottom,
            int left,
            int right,
            int mergePadding,
            int minWidth,
            int minHeight,
            float frameRate,
            bool loop)
        {
            this.name = name;
            this.top = top;
            this.bottom = bottom;
            this.left = left;
            this.right = right;
            this.mergePadding = mergePadding;
            this.minWidth = minWidth;
            this.minHeight = minHeight;
            this.frameRate = frameRate;
            this.loop = loop;
        }
    }

    [MenuItem("Tools/Last Ascention/Build Player Sprites From Sheet")]
    public static void BuildFromSpritesPng()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string sourcePath = Path.Combine(projectRoot, SourceFileName);
        if (!File.Exists(sourcePath))
        {
            Debug.LogError("Player sprite sheet not found at: " + sourcePath);
            return;
        }

        EnsureFolder("Assets/Art", "Characters");
        EnsureFolder("Assets/Art/Characters", "Player");
        EnsureFolder("Assets/Art/Characters/Player", "Kaisen");
        EnsureFolder("Assets/Art/Characters/Player/Kaisen", "Frames");
        EnsureFolder("Assets/Animations", "Player");
        ClearGeneratedAssets();

        Texture2D sourceTexture = LoadTexture(sourcePath);
        Color32[] sourcePixels = sourceTexture.GetPixels32();
        List<Object> generatedAssets = new List<Object>();
        Dictionary<string, List<Sprite>> spritesBySequence = new Dictionary<string, List<Sprite>>();
        Dictionary<string, AnimationClip> clipsBySequence = new Dictionary<string, AnimationClip>();
        SequenceDefinition[] sequences = CreateSequences();

        foreach (SequenceDefinition sequence in sequences)
        {
            List<RectInt> frameBounds = FindFrames(sourcePixels, sourceTexture.width, sourceTexture.height, sequence);
            List<Sprite> sprites = ExportFrames(sourcePixels, sourceTexture.width, sourceTexture.height, sequence, frameBounds);
            spritesBySequence[sequence.name] = sprites;

            if (sprites.Count > 0)
            {
                AnimationClip clip = CreateClip(sequence, sprites);
                clipsBySequence[sequence.name] = clip;
                generatedAssets.Add(clip);
            }
        }

        AnimatorController controller = BuildAnimatorController(clipsBySequence);
        UpdatePlayerPrefab(controller, spritesBySequence);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Object.DestroyImmediate(sourceTexture);

        int totalSprites = 0;
        foreach (List<Sprite> sprites in spritesBySequence.Values)
        {
            totalSprites += sprites.Count;
        }

        Debug.Log("Built player sprites from sprites.png: " + totalSprites + " sprites, " + generatedAssets.Count + " clips.");
    }

    private static SequenceDefinition[] CreateSequences()
    {
        return new[]
        {
            new SequenceDefinition("Idle", 214, 286, 20, 250, 1, 12, 24, 7f, true),
            new SequenceDefinition("Run", 325, 390, 15, 690, 6, 20, 24, 12f, true),
            new SequenceDefinition("Dodge", 420, 495, 15, 500, 2, 20, 24, 12f, false),
            new SequenceDefinition("HitFall", 515, 590, 15, 470, 2, 18, 18, 8f, false),
            new SequenceDefinition("Attack1", 615, 690, 15, 540, 2, 20, 24, 12f, false),
            new SequenceDefinition("Attack2", 615, 690, 720, 1310, 2, 20, 24, 12f, false),
            new SequenceDefinition("Attack3", 735, 820, 15, 720, 2, 20, 24, 12f, false),
            new SequenceDefinition("Attack4", 735, 820, 720, 1570, 2, 20, 24, 12f, false),
            new SequenceDefinition("Attack5", 875, 965, 15, 730, 2, 20, 24, 12f, false),
            new SequenceDefinition("Attack6", 875, 965, 720, 1470, 2, 20, 24, 12f, false),
            new SequenceDefinition("Attack7", 1005, 1110, 15, 780, 2, 20, 24, 12f, false),
        };
    }

    private static Texture2D LoadTexture(string sourcePath)
    {
        byte[] bytes = File.ReadAllBytes(sourcePath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        ImageConversion.LoadImage(texture, bytes, false);
        texture.name = "SungJinWooSourceSheet";
        return texture;
    }

    private static List<RectInt> FindFrames(Color32[] pixels, int width, int height, SequenceDefinition sequence)
    {
        int left = Mathf.Clamp(sequence.left, 0, width - 1);
        int right = Mathf.Clamp(sequence.right, left + 1, width);
        int top = Mathf.Clamp(sequence.top, 0, height - 1);
        int bottom = Mathf.Clamp(sequence.bottom, top + 1, height);
        int columnCount = right - left;
        bool[] occupiedColumns = new bool[columnCount];

        for (int yTop = top; yTop < bottom; yTop++)
        {
            int y = height - 1 - yTop;
            for (int x = left; x < right; x++)
            {
                if (IsSpritePixel(pixels[y * width + x]))
                {
                    occupiedColumns[x - left] = true;
                }
            }
        }

        bool[] mergedColumns = new bool[columnCount];
        for (int i = 0; i < occupiedColumns.Length; i++)
        {
            if (!occupiedColumns[i])
            {
                continue;
            }

            int start = Mathf.Max(0, i - sequence.mergePadding);
            int end = Mathf.Min(columnCount - 1, i + sequence.mergePadding);
            for (int merged = start; merged <= end; merged++)
            {
                mergedColumns[merged] = true;
            }
        }

        List<RectInt> frames = new List<RectInt>();
        int cursor = 0;
        while (cursor < mergedColumns.Length)
        {
            while (cursor < mergedColumns.Length && !mergedColumns[cursor])
            {
                cursor++;
            }

            if (cursor >= mergedColumns.Length)
            {
                break;
            }

            int runStart = cursor;
            while (cursor < mergedColumns.Length && mergedColumns[cursor])
            {
                cursor++;
            }

            int runEnd = cursor - 1;
            RectInt bounds = FindTightBounds(
                pixels,
                width,
                height,
                Mathf.Max(left, left + runStart - sequence.mergePadding),
                Mathf.Min(right - 1, left + runEnd + sequence.mergePadding),
                top,
                bottom);

            if (bounds.width >= sequence.minWidth && bounds.height >= sequence.minHeight)
            {
                frames.Add(ExpandBounds(bounds, width, height, FramePadding));
            }
        }

        return frames;
    }

    private static RectInt FindTightBounds(
        Color32[] pixels,
        int width,
        int height,
        int left,
        int right,
        int top,
        int bottom)
    {
        int minX = width;
        int maxX = -1;
        int minTop = height;
        int maxTop = -1;

        for (int yTop = top; yTop < bottom; yTop++)
        {
            int y = height - 1 - yTop;
            for (int x = left; x <= right; x++)
            {
                if (!IsSpritePixel(pixels[y * width + x]))
                {
                    continue;
                }

                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minTop = Mathf.Min(minTop, yTop);
                maxTop = Mathf.Max(maxTop, yTop);
            }
        }

        if (maxX < minX || maxTop < minTop)
        {
            return new RectInt(0, 0, 0, 0);
        }

        return new RectInt(minX, minTop, maxX - minX + 1, maxTop - minTop + 1);
    }

    private static RectInt ExpandBounds(RectInt bounds, int width, int height, int padding)
    {
        int x = Mathf.Max(0, bounds.x - padding);
        int y = Mathf.Max(0, bounds.y - padding);
        int right = Mathf.Min(width, bounds.x + bounds.width + padding);
        int bottom = Mathf.Min(height, bounds.y + bounds.height + padding);
        return new RectInt(x, y, right - x, bottom - y);
    }

    private static List<Sprite> ExportFrames(Color32[] pixels, int width, int height, SequenceDefinition sequence, List<RectInt> frameBounds)
    {
        List<Sprite> sprites = new List<Sprite>();
        for (int i = 0; i < frameBounds.Count; i++)
        {
            RectInt bounds = frameBounds[i];
            Texture2D frameTexture = new Texture2D(bounds.width, bounds.height, TextureFormat.RGBA32, false);
            Color32[] framePixels = new Color32[bounds.width * bounds.height];

            for (int outY = 0; outY < bounds.height; outY++)
            {
                int sourceTop = bounds.y + bounds.height - 1 - outY;
                int sourceY = height - 1 - sourceTop;
                for (int outX = 0; outX < bounds.width; outX++)
                {
                    int sourceX = bounds.x + outX;
                    Color32 color = pixels[sourceY * width + sourceX];
                    if (IsGreenBackground(color))
                    {
                        color.a = 0;
                    }

                    framePixels[outY * bounds.width + outX] = color;
                }
            }

            frameTexture.SetPixels32(framePixels);
            frameTexture.Apply(false, false);

            string assetPath = FrameFolder + "/" + sequence.name + "_" + i.ToString("00") + ".png";
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
        AnimationClip clip = new AnimationClip
        {
            name = "KaisenPlayer_" + sequence.name,
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

        string clipPath = ClipFolder + "/" + clip.name + ".anim";
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static AnimatorController BuildAnimatorController(Dictionary<string, AnimationClip> clips)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        controller.parameters = new AnimatorControllerParameter[0];
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("DirectionX", AnimatorControllerParameterType.Float);
        controller.AddParameter("DirectionY", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsDodging", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("AttackIndex", AnimatorControllerParameterType.Int);
        controller.AddParameter("IsHit", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine stateMachine = layer.stateMachine;
        ClearStateMachine(stateMachine);

        AnimatorState idle = stateMachine.AddState("Idle", new Vector3(240f, 80f, 0f));
        idle.motion = GetClip(clips, "Idle");
        stateMachine.defaultState = idle;

        AnimatorState run = stateMachine.AddState("Run", new Vector3(520f, 80f, 0f));
        run.motion = GetClip(clips, "Run");

        AnimatorState dodge = stateMachine.AddState("Dodge", new Vector3(520f, 260f, 0f));
        dodge.motion = GetClip(clips, "Dodge");

        AnimatorState hit = stateMachine.AddState("HitFall", new Vector3(800f, 260f, 0f));
        hit.motion = GetClip(clips, "HitFall");

        AnimatorState death = stateMachine.AddState("Death", new Vector3(800f, 440f, 0f));
        death.motion = GetClip(clips, "HitFall");
        death.speed = 0.85f;

        List<AnimatorState> attackStates = new List<AnimatorState>();
        for (int i = 1; i <= 7; i++)
        {
            AnimatorState attack = stateMachine.AddState("Attack" + i, new Vector3(120f + ((i - 1) % 4) * 220f, 440f + ((i - 1) / 4) * 150f, 0f));
            attack.motion = GetClip(clips, "Attack" + i);
            attackStates.Add(attack);
        }

        AddTransition(idle, run, AnimatorConditionMode.Greater, 0.1f, "Speed");
        AddTransition(run, idle, AnimatorConditionMode.Less, 0.1f, "Speed");

        AnimatorStateTransition deathTransition = stateMachine.AddAnyStateTransition(death);
        deathTransition.hasExitTime = false;
        deathTransition.canTransitionToSelf = false;
        deathTransition.duration = 0f;
        deathTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");

        AnimatorStateTransition hitTransition = stateMachine.AddAnyStateTransition(hit);
        hitTransition.hasExitTime = false;
        hitTransition.canTransitionToSelf = false;
        hitTransition.duration = 0f;
        hitTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsHit");

        AnimatorStateTransition dodgeTransition = stateMachine.AddAnyStateTransition(dodge);
        dodgeTransition.hasExitTime = false;
        dodgeTransition.canTransitionToSelf = false;
        dodgeTransition.duration = 0f;
        dodgeTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsDodging");

        for (int i = 0; i < attackStates.Count; i++)
        {
            AnimatorState attack = attackStates[i];
            AnimatorStateTransition attackTransition = stateMachine.AddAnyStateTransition(attack);
            attackTransition.hasExitTime = false;
            attackTransition.canTransitionToSelf = false;
            attackTransition.duration = 0f;
            attackTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsAttacking");
            attackTransition.AddCondition(AnimatorConditionMode.Equals, i, "AttackIndex");
        }

        AnimatorStateTransition dodgeToRun = AddTransition(dodge, run, AnimatorConditionMode.IfNot, 0f, "IsDodging");
        dodgeToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        AnimatorStateTransition dodgeToIdle = AddTransition(dodge, idle, AnimatorConditionMode.IfNot, 0f, "IsDodging");
        dodgeToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        AnimatorStateTransition hitToRun = AddTransition(hit, run, AnimatorConditionMode.IfNot, 0f, "IsHit");
        hitToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        AnimatorStateTransition hitToIdle = AddTransition(hit, idle, AnimatorConditionMode.IfNot, 0f, "IsHit");
        hitToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        for (int i = 0; i < attackStates.Count; i++)
        {
            AnimatorState attack = attackStates[i];
            AnimatorStateTransition attackToRun = AddTransition(attack, run, AnimatorConditionMode.IfNot, 0f, "IsAttacking");
            attackToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            AnimatorStateTransition attackToIdle = AddTransition(attack, idle, AnimatorConditionMode.IfNot, 0f, "IsAttacking");
            attackToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        }

        return controller;
    }

    private static AnimationClip GetClip(Dictionary<string, AnimationClip> clips, string key)
    {
        AnimationClip clip;
        return clips.TryGetValue(key, out clip) ? clip : null;
    }

    private static AnimatorStateTransition AddTransition(
        AnimatorState from,
        AnimatorState to,
        AnimatorConditionMode mode,
        float threshold,
        string parameter)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.AddCondition(mode, threshold, parameter);
        return transition;
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

    private static void UpdatePlayerPrefab(AnimatorController controller, Dictionary<string, List<Sprite>> spritesBySequence)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            Transform visuals = prefabRoot.transform.Find("Visuals");
            if (visuals == null)
            {
                GameObject visualsObject = new GameObject("Visuals");
                visualsObject.transform.SetParent(prefabRoot.transform, false);
                visuals = visualsObject.transform;
            }

            SpriteRenderer renderer = visuals.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = visuals.gameObject.AddComponent<SpriteRenderer>();
            }

            Animator animator = visuals.GetComponent<Animator>();
            if (animator == null)
            {
                animator = visuals.gameObject.AddComponent<Animator>();
            }

            List<Sprite> idleSprites;
            if (spritesBySequence.TryGetValue("Idle", out idleSprites) && idleSprites.Count > 0)
            {
                renderer.sprite = idleSprites[0];
            }

            renderer.sortingLayerName = "Characters";
            renderer.sortingOrder = 0;
            animator.runtimeAnimatorController = controller;
            visuals.localPosition = Vector3.zero;
            visuals.localRotation = Quaternion.identity;
            visuals.localScale = Vector3.one;

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static bool IsSpritePixel(Color32 color)
    {
        return !IsGreenBackground(color) && color.a > 0;
    }

    private static bool IsGreenBackground(Color32 color)
    {
        return color.g > 135
            && color.r < 90
            && color.b < 130
            && color.g > color.r * 1.45f
            && color.g > color.b * 1.45f;
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
        DeleteAssetsInFolder(ClipFolder, "anim");
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
