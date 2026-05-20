using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HeartPickup : MonoBehaviour
{
    [SerializeField] private float healAmount = 10f;

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
        {
            return;
        }

        playerHealth.Heal(healAmount);
        FloatingTextPopup.Spawn("+HP", new Color(0f, 1f, 0.53f, 1f), transform.position + Vector3.up * 0.35f);
        Destroy(gameObject);
    }
}

