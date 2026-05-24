#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DungeonRoom))]
[CanEditMultipleObjects]
public class DungeonRoomEditor : Editor
{
    private static readonly string[] DoorNames = { "Norte", "Sur", "Este", "Oeste" };
    private static readonly Vector3[] DoorOffsets =
    {
        new Vector3(0f, 7.5f, 0f),
        new Vector3(0f, -7.5f, 0f),
        new Vector3(10f, 0f, 0f),
        new Vector3(-10f, 0f, 0f),
    };

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox(
            "Edita esta sala en la escena RoomTemplates o en modo Prefab. " +
            "Los puntos de spawn, props y puertas se dibujan como gizmos.",
            MessageType.Info);
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.InSelectionHierarchy)]
    private static void DrawRoomGizmos(DungeonRoom room, GizmoType gizmoType)
    {
        if (room == null)
        {
            return;
        }

        RoomTemplateSlot slot = room.GetComponent<RoomTemplateSlot>();
        Color color = slot != null ? slot.GizmoColor : RoomTemplateSlot.GetDefaultColor(RoomType.Normal);
        Vector2 size = slot != null ? slot.GizmoSize : new Vector2(20f, 15f);

        Gizmos.color = new Color(color.r, color.g, color.b, 0.35f);
        Gizmos.DrawCube(room.transform.position, new Vector3(size.x, size.y, 0.1f));
        Gizmos.color = color;
        Gizmos.DrawWireCube(room.transform.position, new Vector3(size.x, size.y, 0.1f));

        SerializedObject so = new SerializedObject(room);
        DrawTransformList(so.FindProperty("spawnPoints"), room.transform, Color.red, "S");
        DrawTransformList(so.FindProperty("propPoints"), room.transform, Color.yellow, "P");
        DrawDoorGizmos(so, room.transform, color);
    }

    private static void DrawTransformList(SerializedProperty listProperty, Transform roomTransform, Color color, string label)
    {
        if (listProperty == null || !listProperty.isArray)
        {
            return;
        }

        Gizmos.color = color;
        for (int i = 0; i < listProperty.arraySize; i++)
        {
            SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
            Transform point = element.objectReferenceValue as Transform;
            if (point == null)
            {
                continue;
            }

            Gizmos.DrawSphere(point.position, 0.25f);
            Handles.color = color;
            Handles.Label(point.position + Vector3.up * 0.35f, label + i);
        }
    }

    private static void DrawDoorGizmos(SerializedObject so, Transform roomTransform, Color color)
    {
        SerializedProperty doorPoints = so.FindProperty("doorPoints");
        if (doorPoints == null || !doorPoints.isArray)
        {
            return;
        }

        Handles.color = color;
        for (int i = 0; i < Mathf.Min(4, doorPoints.arraySize); i++)
        {
            SerializedProperty element = doorPoints.GetArrayElementAtIndex(i);
            Transform door = element.objectReferenceValue as Transform;
            Vector3 position = door != null ? door.position : roomTransform.position + DoorOffsets[i];
            Gizmos.color = color;
            Gizmos.DrawWireCube(position, new Vector3(2.2f, 2.2f, 0.1f));
            Handles.Label(position + Vector3.up * 0.5f, DoorNames[i]);
        }
    }
}
#endif
