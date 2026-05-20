using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private WeaponData[] equippedWeapons = new WeaponData[2];
    [SerializeField] private int activeSlot;
    [SerializeField] private WeaponData defaultWeapon;
    [SerializeField] private GameObject weaponPickupPrefab;
    [SerializeField] private Transform weaponMount;
    [SerializeField] private WeaponBase weaponRuntime;

    private readonly int[] slotAmmo = new int[2];

    private KaisenController controller;
    private WeaponHudUI weaponHudUI;
    private Coroutine swapRoutine;

    public int ActiveSlot { get { return activeSlot; } }
    public WeaponBase WeaponRuntime { get { return weaponRuntime; } }

    private void Awake()
    {
        controller = GetComponent<KaisenController>();
        weaponHudUI = FindFirstObjectByType<WeaponHudUI>();
        EnsureWeaponRuntime();
        InitializeSlots();
        ApplyActiveWeapon();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            SwitchWeapon();
        }

        if (weaponRuntime != null && controller != null)
        {
            weaponRuntime.SetAimDirection(controller.AimDirection);
        }
    }

    public bool TryFireActiveWeapon(Vector2 direction)
    {
        if (weaponRuntime == null)
        {
            return false;
        }

        if (weaponRuntime.Data != null && weaponRuntime.CurrentAmmo == 0)
        {
            EventBus.Publish(new WeaponEmptyEvent(weaponRuntime.Data, activeSlot));
            RefreshHud();
            return false;
        }

        bool fired = weaponRuntime.TryFire(direction);
        if (fired)
        {
            if (controller != null)
            {
                controller.PlayAttackAnimation(GetAttackAnimationDuration());
            }

            slotAmmo[activeSlot] = weaponRuntime.CurrentAmmo;
            RefreshHud();
        }

        return fired;
    }

    public bool EquipWeapon(WeaponData weaponData, int ammoOverride = int.MinValue)
    {
        if (weaponData == null)
        {
            return false;
        }

        int resolvedAmmo = ResolveAmmoForEquip(weaponData, ammoOverride);
        int emptySlot = GetEmptySlot();
        if (emptySlot >= 0)
        {
            equippedWeapons[emptySlot] = weaponData;
            slotAmmo[emptySlot] = resolvedAmmo;
            activeSlot = emptySlot;
            ApplyActiveWeapon();
            return true;
        }

        WeaponData droppedWeapon = equippedWeapons[activeSlot];
        int droppedAmmo = slotAmmo[activeSlot];
        equippedWeapons[activeSlot] = weaponData;
        slotAmmo[activeSlot] = resolvedAmmo;
        ApplyActiveWeapon();
        DropWeapon(droppedWeapon, droppedAmmo);
        return true;
    }

    public WeaponData GetWeaponAtSlot(int slot)
    {
        if (slot < 0 || slot >= equippedWeapons.Length)
        {
            return null;
        }

        return equippedWeapons[slot];
    }

    public int GetAmmoForSlot(int slot)
    {
        if (slot < 0 || slot >= slotAmmo.Length)
        {
            return -1;
        }

        return slotAmmo[slot];
    }

    private void SwitchWeapon()
    {
        int nextSlot = activeSlot == 0 ? 1 : 0;
        if (equippedWeapons[nextSlot] == null)
        {
            return;
        }

        activeSlot = nextSlot;
        ApplyActiveWeapon();
        if (swapRoutine != null)
        {
            StopCoroutine(swapRoutine);
        }

        swapRoutine = StartCoroutine(SwapAnimationRoutine());
    }

    private void ApplyActiveWeapon()
    {
        EnsureWeaponRuntime();
        WeaponData activeWeapon = equippedWeapons[activeSlot];
        int ammo = slotAmmo[activeSlot];
        weaponRuntime.SetWeaponData(activeWeapon, ammo);
        EventBus.Publish(new WeaponSwappedEvent(activeWeapon, activeSlot));
        RefreshHud();
    }

    private void InitializeSlots()
    {
        if (defaultWeapon != null && equippedWeapons[0] == null && equippedWeapons[1] == null)
        {
            equippedWeapons[0] = defaultWeapon;
        }

        for (int i = 0; i < slotAmmo.Length; i++)
        {
            if (equippedWeapons[i] == null)
            {
                slotAmmo[i] = -1;
                continue;
            }

            if (slotAmmo[i] == 0)
            {
                slotAmmo[i] = GetInitialAmmo(equippedWeapons[i]);
            }
        }
    }

    private void EnsureWeaponRuntime()
    {
        if (weaponMount == null)
        {
            Transform existingMount = transform.Find("WeaponMount");
            if (existingMount != null)
            {
                weaponMount = existingMount;
            }
            else
            {
                GameObject mountObject = new GameObject("WeaponMount");
                mountObject.transform.SetParent(transform, false);
                mountObject.transform.localPosition = Vector3.zero;
                weaponMount = mountObject.transform;
            }
        }

        if (weaponRuntime != null)
        {
            return;
        }

        weaponRuntime = weaponMount.GetComponentInChildren<WeaponBase>();
        if (weaponRuntime != null)
        {
            return;
        }

        GameObject weaponObject = new GameObject("EquippedWeapon");
        weaponObject.transform.SetParent(weaponMount, false);
        weaponObject.transform.localPosition = new Vector3(0.35f, 0f, 0f);
        SpriteRenderer renderer = weaponObject.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = "Projectiles";
        renderer.sortingOrder = 2;
        weaponRuntime = weaponObject.AddComponent<WeaponBase>();
    }

    private void DropWeapon(WeaponData weaponData, int ammo)
    {
        if (weaponData == null || weaponPickupPrefab == null)
        {
            return;
        }

        GameObject pickupObject = Instantiate(weaponPickupPrefab, transform.position + Vector3.right * 0.6f, Quaternion.identity);
        WeaponPickup pickup = pickupObject.GetComponent<WeaponPickup>();
        if (pickup != null)
        {
            pickup.AssignWeaponData(weaponData, ammo);
        }
    }

    private int GetEmptySlot()
    {
        for (int i = 0; i < equippedWeapons.Length; i++)
        {
            if (equippedWeapons[i] == null)
            {
                return i;
            }
        }

        return -1;
    }

    private int GetInitialAmmo(WeaponData weaponData)
    {
        return weaponData != null && weaponData.maxAmmo >= 0 ? weaponData.maxAmmo : -1;
    }

    private int ResolveAmmoForEquip(WeaponData weaponData, int ammoOverride)
    {
        if (weaponData == null)
        {
            return -1;
        }

        if (weaponData.maxAmmo < 0)
        {
            return -1;
        }

        return ammoOverride != int.MinValue ? Mathf.Clamp(ammoOverride, 0, weaponData.maxAmmo) : GetInitialAmmo(weaponData);
    }

    private float GetAttackAnimationDuration()
    {
        WeaponData weaponData = weaponRuntime != null ? weaponRuntime.Data : null;
        if (weaponData == null || weaponData.fireRate <= 0f)
        {
            return 0.22f;
        }

        return Mathf.Clamp(1f / weaponData.fireRate, 0.12f, 0.35f);
    }

    private void RefreshHud()
    {
        if (weaponHudUI == null)
        {
            weaponHudUI = FindFirstObjectByType<WeaponHudUI>();
        }

        if (weaponHudUI != null)
        {
            weaponHudUI.Refresh(this);
        }
    }

    private IEnumerator SwapAnimationRoutine()
    {
        if (weaponMount == null)
        {
            yield break;
        }

        Vector3 baseScale = Vector3.one;
        float elapsed = 0f;
        while (elapsed < 0.08f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.08f);
            weaponMount.localScale = Vector3.Lerp(baseScale, new Vector3(0.6f, 0.6f, 1f), t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 0.08f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.08f);
            weaponMount.localScale = Vector3.Lerp(new Vector3(0.6f, 0.6f, 1f), baseScale, t);
            yield return null;
        }

        weaponMount.localScale = baseScale;
        swapRoutine = null;
    }
}
