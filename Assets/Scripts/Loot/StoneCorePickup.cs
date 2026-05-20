using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StoneCorePickup : MonoBehaviour
{
    [SerializeField] private float resistanceBonus = 20f;

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
        KaisenController controller = other.GetComponentInParent<KaisenController>();
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (controller == null || health == null)
        {
            return;
        }

        controller.Stats.resistance += resistanceBonus;
        NotificationSystem.Instance?.ShowNotification("NUCLEO DE PIEDRA: RESISTENCIA +20", new Color(0.15f, 0.9f, 1f, 1f), 2.5f);
        FloatingTextPopup.Spawn("+20 RES", new Color(0.15f, 0.9f, 1f, 1f), transform.position + Vector3.up * 0.35f);
        health.Heal(10f);
        Destroy(gameObject);
    }
}

