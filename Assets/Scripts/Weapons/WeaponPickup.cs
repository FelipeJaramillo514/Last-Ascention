using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private int ammoOverride = int.MinValue;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobPeriod = 0.3f;

    private WeaponManager nearbyManager;
    private Vector3 basePosition;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
        basePosition = transform.position;
        RefreshVisual();
    }

    private void Update()
    {
        float bobPhase = bobPeriod > 0f ? (Time.time / bobPeriod) * Mathf.PI * 2f : 0f;
        transform.position = basePosition + Vector3.up * Mathf.Sin(bobPhase) * bobHeight;

        if (nearbyManager != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            bool equipped = nearbyManager.EquipWeapon(weaponData, ammoOverride);
            if (equipped)
            {
                if (NotificationSystem.Instance != null && weaponData != null)
                {
                    NotificationSystem.Instance.ShowNotification("ARMA EQUIPADA: " + weaponData.weaponName, new Color(0f, 0.75f, 1f, 1f), 1.5f);
                }

                InteractionPromptUI.Instance?.HidePrompt(this);
                Destroy(gameObject);
            }
        }
    }

    public void AssignWeaponData(WeaponData data, int ammo = int.MinValue)
    {
        weaponData = data;
        ammoOverride = ammo;
        basePosition = transform.position;
        RefreshVisual();
    }

    public WeaponData GetWeaponData()
    {
        return weaponData;
    }

    public int GetAmmoOverride()
    {
        return ammoOverride;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        WeaponManager manager = other.GetComponentInParent<WeaponManager>();
        if (manager == null)
        {
            return;
        }

        nearbyManager = manager;
        if (weaponData != null && InteractionPromptUI.Instance != null)
        {
            InteractionPromptUI.Instance.ShowPrompt(this, "[E] Recoger - " + weaponData.weaponName);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        WeaponManager manager = other.GetComponentInParent<WeaponManager>();
        if (manager == null || manager != nearbyManager)
        {
            return;
        }

        nearbyManager = null;
        InteractionPromptUI.Instance?.HidePrompt(this);
    }

    private void RefreshVisual()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sprite = WeaponVisualResolver.GetWeaponIcon(weaponData);
        spriteRenderer.enabled = spriteRenderer.sprite != null;
    }
}
