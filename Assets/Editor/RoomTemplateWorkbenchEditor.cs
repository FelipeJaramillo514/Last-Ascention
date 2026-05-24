#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(RoomTemplateWorkbench))]
public class RoomTemplateWorkbenchEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RoomTemplateWorkbench workbench = (RoomTemplateWorkbench)target;
        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox(
            "Edita cada sala hijo en esta escena. Al terminar, aplica cambios al prefab (Overrides → Apply All) " +
            "y sincroniza la biblioteca.",
            MessageType.Info);

        if (GUILayout.Button("Refrescar slots"))
        {
            workbench.RefreshSlotReferences();
        }

        if (GUILayout.Button("Sincronizar biblioteca desde escena"))
        {
            RoomTemplateSceneBuilder.SyncLibraryFromScene();
        }

        if (GUILayout.Button("Abrir escena RoomTemplates"))
        {
            EditorSceneManager.OpenScene("Assets/Scenes/RoomTemplates.unity");
        }
    }
}
#endif
