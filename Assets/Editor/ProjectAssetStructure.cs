#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Asegura la jerarquía estándar de carpetas del proyecto (Art, UI, _Dev).
/// Menú: Last Ascention / Validate Asset Folders
/// </summary>
public static class ProjectAssetStructure
{
    private static readonly string[] RequiredFolders =
    {
        "Assets/_Dev/Screenshots",
        "Assets/Art/Characters/Player/Kaisen/Frames",
        "Assets/Art/Characters/Enemies/Goblin/Frames",
        "Assets/Art/Backgrounds/Hub",
        "Assets/Art/Backgrounds/Menu",
        "Assets/Art/Backgrounds/Dungeon",
        "Assets/Art/Environment/Cover",
        "Assets/Art/Environment/Tiles",
        "Assets/Art/Items/Weapons",
        "Assets/Art/Items/Pickups",
        "Assets/Art/Items/Projectiles",
        "Assets/Art/Placeholder",
        "Assets/UI/Authored/MainMenu",
        "Assets/UI/Catalog",
        "Assets/UI/Sprites",
        "Assets/UI/Prefabs",
        "Assets/Scenes/RoomTemplates.unity",
        "Assets/Data/Rooms",
    };

    [MenuItem("Last Ascention/Validate Asset Folders")]
    public static void ValidateFolders()
    {
        foreach (string path in RequiredFolders)
        {
            EnsureFolderPath(path);
        }

        AssetDatabase.Refresh();
        Debug.Log("[Last Ascention] Carpetas de assets validadas. Ver UIInterfaceRegistry para catálogo de interfaces.");
    }

    private static void EnsureFolderPath(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            return;
        }

        string[] parts = assetPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
#endif
