using UnityEngine;

public class PlayerDodgedEvent
{
    public GameObject player { get; }
    public Vector2 position { get; }
    public Vector2 direction { get; }

    public PlayerDodgedEvent(GameObject player, Vector2 position, Vector2 direction)
    {
        this.player = player;
        this.position = position;
        this.direction = direction;
    }
}
