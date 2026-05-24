using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DoorTrigger : MonoBehaviour
{
    [SerializeField] private DungeonRoom sourceRoom;
    [SerializeField] private DungeonRoom destinationRoom;
    [SerializeField] private int sourceDoorIndex;
    [SerializeField] private int destinationDoorIndex;
    [SerializeField] private float reentryCooldown = 0.25f;
    [SerializeField] private bool traversalEnabled = true;

    private float nextAllowedTriggerTime;

    public DungeonRoom SourceRoom { get { return sourceRoom; } }
    public DungeonRoom DestinationRoom { get { return destinationRoom; } }
    public int SourceDoorIndex { get { return sourceDoorIndex; } }
    public int DestinationDoorIndex { get { return destinationDoorIndex; } }
    public bool TraversalEnabled { get { return traversalEnabled; } }

    private void Awake()
    {
        BoxCollider2D triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
    }

    public void Configure(DungeonRoom source, DungeonRoom destination, int sourceIndex, int destinationIndex)
    {
        sourceRoom = source;
        destinationRoom = destination;
        sourceDoorIndex = sourceIndex;
        destinationDoorIndex = destinationIndex;
    }

    public void SetTraversalEnabled(bool enabled)
    {
        traversalEnabled = enabled;
        BoxCollider2D triggerCollider = GetComponent<BoxCollider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.enabled = enabled;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!traversalEnabled)
        {
            return;
        }

        if (Time.time < nextAllowedTriggerTime)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (DungeonBuilder.Instance == null)
        {
            return;
        }

        if (sourceRoom != null && sourceRoom.IsCombatLocked)
        {
            nextAllowedTriggerTime = Time.time + reentryCooldown;
            return;
        }

        nextAllowedTriggerTime = Time.time + reentryCooldown;
        DungeonBuilder.Instance.HandleDoorTransition(this, other.transform);
    }
}
