#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Reflection;

[InitializeOnLoad]
public static class ProjectFinalPolishConfigurator
{
    private const string SessionKey = "Kaisen_T10_Config_Ran";

    static ProjectFinalPolishConfigurator()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += ApplyConfiguration;
    }

    private static void ApplyConfiguration()
    {
        bool changed = false;
        changed |= EnsureGameBalanceAsset();
        changed |= EnsureMainMenuScene();
        changed |= ConfigureBuildSettings();
        changed |= ConfigurePlayerSettings();
        changed |= ConfigureTextureImports();

        if (changed)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static bool EnsureGameBalanceAsset()
    {
        const string resourcesFolder = "Assets/Resources";
        const string assetPath = "Assets/Resources/GameBalanceData.asset";
        if (!AssetDatabase.IsValidFolder(resourcesFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        GameBalanceData asset = AssetDatabase.LoadAssetAtPath<GameBalanceData>(assetPath);
        if (asset != null)
        {
            return false;
        }

        asset = ScriptableObject.CreateInstance<GameBalanceData>();
        asset.ResetToDefaults();
        AssetDatabase.CreateAsset(asset, assetPath);
        return true;
    }

    private static bool EnsureMainMenuScene()
    {
        const string sourcePath = "Assets/Scenes/SampleScene.unity";
        const string targetPath = "Assets/Scenes/MainMenu.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(targetPath) != null)
        {
            return false;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(sourcePath) == null)
        {
            return false;
        }

        return AssetDatabase.CopyAsset(sourcePath, targetPath);
    }

    private static bool ConfigureBuildSettings()
    {
        EditorBuildSettingsScene[] desiredScenes =
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/CityArken.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/GameplayScene.unity", true)
        };

        EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
        if (currentScenes.Length == desiredScenes.Length)
        {
            bool identical = true;
            for (int i = 0; i < currentScenes.Length; i++)
            {
                if (currentScenes[i].path != desiredScenes[i].path || currentScenes[i].enabled != desiredScenes[i].enabled)
                {
                    identical = false;
                    break;
                }
            }

            if (identical)
            {
                return false;
            }
        }

        EditorBuildSettings.scenes = desiredScenes;
        EditorUserBuildSettings.development = true;
        return true;
    }

    private static bool ConfigurePlayerSettings()
    {
        bool changed = false;

        if (PlayerSettings.companyName != "Kaisen Game")
        {
            PlayerSettings.companyName = "Kaisen Game";
            changed = true;
        }

        if (PlayerSettings.productName != "Kaisen Game")
        {
            PlayerSettings.productName = "Kaisen Game";
            changed = true;
        }

        if (PlayerSettings.defaultScreenWidth != 1280)
        {
            PlayerSettings.defaultScreenWidth = 1280;
            changed = true;
        }

        if (PlayerSettings.defaultScreenHeight != 720)
        {
            PlayerSettings.defaultScreenHeight = 720;
            changed = true;
        }

        if (PlayerSettings.fullScreenMode != FullScreenMode.FullScreenWindow)
        {
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            changed = true;
        }

        if (!PlayerSettings.runInBackground)
        {
            PlayerSettings.runInBackground = true;
            changed = true;
        }

        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.Direct3D11 });
        TrySetStandaloneScriptingBackend();
        TrySetStandaloneApiCompatibility();
        changed = true;
        return changed;
    }

    private static bool ConfigureTextureImports()
    {
        bool changed = false;
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        for (int i = 0; i < textureGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            if (importer.textureType != TextureImporterType.Sprite)
            {
                continue;
            }

            bool importerChanged = false;
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                importerChanged = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importerChanged = true;
            }

            if (!importerChanged)
            {
                continue;
            }

            importer.SaveAndReimport();
            changed = true;
        }

        return changed;
    }

    private static void TrySetStandaloneScriptingBackend()
    {
        Type namedBuildTargetType = Type.GetType("UnityEditor.Build.NamedBuildTarget, UnityEditor.CoreModule");
        if (namedBuildTargetType != null)
        {
            PropertyInfo standaloneProperty = namedBuildTargetType.GetProperty("Standalone", BindingFlags.Public | BindingFlags.Static);
            MethodInfo method = typeof(PlayerSettings).GetMethod("SetScriptingBackend", new[] { namedBuildTargetType, typeof(ScriptingImplementation) });
            if (standaloneProperty != null && method != null)
            {
                object standalone = standaloneProperty.GetValue(null, null);
                method.Invoke(null, new[] { standalone, (object)ScriptingImplementation.IL2CPP });
                return;
            }
        }

        MethodInfo fallbackMethod = typeof(PlayerSettings).GetMethod("SetScriptingBackend", new[] { typeof(BuildTargetGroup), typeof(ScriptingImplementation) });
        if (fallbackMethod != null)
        {
            fallbackMethod.Invoke(null, new object[] { BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP });
        }
    }

    private static void TrySetStandaloneApiCompatibility()
    {
        Type enumType = typeof(ApiCompatibilityLevel);
        object compatibilityValue = null;
        string[] candidateNames = { "NET_Standard", "NET_Standard_2_1" };
        for (int i = 0; i < candidateNames.Length; i++)
        {
            if (Enum.IsDefined(enumType, candidateNames[i]))
            {
                compatibilityValue = Enum.Parse(enumType, candidateNames[i]);
                break;
            }
        }

        if (compatibilityValue == null)
        {
            return;
        }

        Type namedBuildTargetType = Type.GetType("UnityEditor.Build.NamedBuildTarget, UnityEditor.CoreModule");
        if (namedBuildTargetType != null)
        {
            PropertyInfo standaloneProperty = namedBuildTargetType.GetProperty("Standalone", BindingFlags.Public | BindingFlags.Static);
            MethodInfo method = typeof(PlayerSettings).GetMethod("SetApiCompatibilityLevel", new[] { namedBuildTargetType, enumType });
            if (standaloneProperty != null && method != null)
            {
                object standalone = standaloneProperty.GetValue(null, null);
                method.Invoke(null, new[] { standalone, compatibilityValue });
                return;
            }
        }

        MethodInfo fallbackMethod = typeof(PlayerSettings).GetMethod("SetApiCompatibilityLevel", new[] { typeof(BuildTargetGroup), enumType });
        if (fallbackMethod != null)
        {
            fallbackMethod.Invoke(null, new object[] { BuildTargetGroup.Standalone, compatibilityValue });
        }
    }
}
#endif
