using UnityEngine;

public enum WeaponType
{
    Melee,
    Ranged,
    Hybrid
}

[CreateAssetMenu(fileName = "WeaponData", menuName = "Kaisen/Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string weaponName = "Weapon";
    public Sprite weaponIcon;

    [Header("Core Stats")]
    public float damage = 10f;
    public float fireRate = 1f;
    public int maxAmmo = -1;
    public float projectileSpeed;
    public float projectileRange = 3f;
    public bool isPiercing;
    public bool isExplosive;
    public WeaponType weaponType = WeaponType.Melee;

    [Header("Assets")]
    public AudioClip fireSound;
    public GameObject projectilePrefab;
    public Sprite[] animationFrames;

    [Header("Melee")]
    public float meleeRadius = 0.8f;
    public float meleeDistance = 1.5f;
    [Range(0f, 360f)] public float meleeArc = 120f;
    public float knockbackForce;

    [Header("Ranged")]
    public float[] spreadAngles = new float[0];
}
