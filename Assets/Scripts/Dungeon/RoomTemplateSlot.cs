using UnityEngine;

/// <summary>
/// Marca una instancia de sala en la escena de plantillas (RoomTemplates).
/// Permite ver y editar cada esquema en el Editor.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(DungeonRoom))]
public class RoomTemplateSlot : MonoBehaviour
{
    [SerializeField] private RoomType slotRoomType = RoomType.Normal;
    [SerializeField] private string displayLabel;
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.85f, 1f, 0.85f);
    [SerializeField] private Vector2 gizmoSize = new Vector2(20f, 15f);

    public RoomType SlotRoomType => slotRoomType;
    public string DisplayLabel => string.IsNullOrWhiteSpace(displayLabel) ? slotRoomType.ToString() : displayLabel;
    public Vector2 GizmoSize => gizmoSize;
    public Color GizmoColor => gizmoColor;

    public DungeonRoom DungeonRoom => GetComponent<DungeonRoom>();

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(displayLabel))
        {
            displayLabel = slotRoomType.ToString();
        }

        gizmoColor = GetDefaultColor(slotRoomType);
    }

    public static Color GetDefaultColor(RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.Entry: return new Color(0.35f, 1f, 0.45f, 0.9f);
            case RoomType.Boss: return new Color(1f, 0.35f, 0.25f, 0.9f);
            case RoomType.Shop: return new Color(1f, 0.84f, 0.2f, 0.9f);
            case RoomType.Secret: return new Color(0.75f, 0.45f, 1f, 0.9f);
            default: return new Color(0.25f, 0.75f, 1f, 0.9f);
        }
    }
}
