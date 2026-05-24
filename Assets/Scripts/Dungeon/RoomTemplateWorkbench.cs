using UnityEngine;

/// <summary>
/// Raíz de la escena RoomTemplates: agrupa esquemas visibles y enlaza la biblioteca de generación.
/// </summary>
public class RoomTemplateWorkbench : MonoBehaviour
{
    [SerializeField] private RoomTemplateLibrary templateLibrary;
    [SerializeField] private float templateSpacing = 28f;
    [SerializeField] private RoomTemplateSlot[] slots = System.Array.Empty<RoomTemplateSlot>();

    public RoomTemplateLibrary TemplateLibrary => templateLibrary;
    public float TemplateSpacing => templateSpacing;
    public RoomTemplateSlot[] Slots => slots;

    public void CollectSlotsFromChildren()
    {
        slots = GetComponentsInChildren<RoomTemplateSlot>(true);
    }

#if UNITY_EDITOR
    public void RefreshSlotReferences()
    {
        CollectSlotsFromChildren();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
