using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CrystalPickup : MonoBehaviour
{
    [SerializeField] private float crystalValue = 10f;

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
        KaisenController player = other.GetComponentInParent<KaisenController>();
        if (player == null)
        {
            return;
        }

        if (SystemManager.Instance != null)
        {
            SystemManager.Instance.AddGold(crystalValue);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCue(AudioCueId.CrystalPickup, transform.position, 0.8f, 1f, false, 0f);
        }

        FloatingTextPopup.Spawn("+" + Mathf.RoundToInt(crystalValue), new Color(0f, 0.75f, 1f, 1f), transform.position + Vector3.up * 0.35f);
        Destroy(gameObject);
    }
}
