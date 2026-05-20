using UnityEngine;

public class LootSpawner : MonoBehaviour
{
    public static LootSpawner Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject crystalPickupPrefab;
    [SerializeField] private GameObject heartPickupPrefab;
    [SerializeField] private GameObject weaponPickupPrefab;

    [Header("Weapons")]
    [SerializeField] private WeaponData[] weaponDropTable;

    [Header("Scatter")]
    [SerializeField] private float scatterRadius = 0.45f;
    [SerializeField] private float scatterImpulse = 0.75f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public GameObject SpawnDrop(GameObject dropPrefab, Vector3 position)
    {
        if (dropPrefab == null)
        {
            return null;
        }

        GameObject drop = Instantiate(dropPrefab, position, Quaternion.identity);
        ConfigureDrop(drop);
        ApplyScatter(drop);
        return drop;
    }

    public GameObject SpawnCrystal(Vector3 position)
    {
        return SpawnDrop(crystalPickupPrefab, position);
    }

    public GameObject SpawnHeart(Vector3 position)
    {
        return SpawnDrop(heartPickupPrefab, position);
    }

    public GameObject SpawnRandomWeaponDrop(Vector3 position)
    {
        if (weaponPickupPrefab == null)
        {
            return null;
        }

        GameObject drop = Instantiate(weaponPickupPrefab, position, Quaternion.identity);
        WeaponPickup pickup = drop.GetComponent<WeaponPickup>();
        if (pickup != null)
        {
            pickup.AssignWeaponData(GetRandomWeaponData());
        }

        ApplyScatter(drop);
        return drop;
    }

    private void ConfigureDrop(GameObject drop)
    {
        if (drop == null)
        {
            return;
        }

        WeaponPickup weaponPickup = drop.GetComponent<WeaponPickup>();
        if (weaponPickup != null && weaponPickup.GetWeaponData() == null)
        {
            weaponPickup.AssignWeaponData(GetRandomWeaponData());
        }
    }

    private WeaponData GetRandomWeaponData()
    {
        if (weaponDropTable == null || weaponDropTable.Length == 0)
        {
            return null;
        }

        return weaponDropTable[Random.Range(0, weaponDropTable.Length)];
    }

    private void ApplyScatter(GameObject drop)
    {
        if (drop == null)
        {
            return;
        }

        Vector2 scatterOffset = Random.insideUnitCircle * scatterRadius;
        drop.transform.position += (Vector3)scatterOffset;

        Rigidbody2D rigidbody2D = drop.GetComponent<Rigidbody2D>();
        if (rigidbody2D != null)
        {
            rigidbody2D.AddForce(scatterOffset.normalized * scatterImpulse, ForceMode2D.Impulse);
        }
    }
}

