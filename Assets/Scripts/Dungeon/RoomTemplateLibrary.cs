using System;
using UnityEngine;

/// <summary>
/// Catálogo de prefabs de sala usados por DungeonBuilder al generar un nivel.
/// </summary>
[CreateAssetMenu(fileName = "RoomTemplateLibrary", menuName = "Kaisen/Dungeon/Room Template Library")]
public class RoomTemplateLibrary : ScriptableObject
{
    [Header("Plantillas por tipo")]
    [Tooltip("Sala de inicio de la run.")]
    public GameObject entryRoom;

    [Tooltip("Variantes para salas normales y secretas.")]
    public GameObject[] normalRooms = Array.Empty<GameObject>();

    [Tooltip("Sala de jefe.")]
    public GameObject bossRoom;

    [Tooltip("Sala de tienda.")]
    public GameObject shopRoom;

    [Tooltip("Opcional. Si está vacío, las salas Secret usan una normal aleatoria.")]
    public GameObject secretRoom;

    public GameObject SelectPrefab(RoomType roomType, System.Random random)
    {
        if (roomType == RoomType.Entry)
        {
            return entryRoom;
        }

        if (roomType == RoomType.Boss)
        {
            return bossRoom != null ? bossRoom : SelectRandomNormal(random);
        }

        if (roomType == RoomType.Shop)
        {
            return shopRoom != null ? shopRoom : SelectRandomNormal(random);
        }

        if (roomType == RoomType.Secret)
        {
            return secretRoom != null ? secretRoom : SelectRandomNormal(random);
        }

        return SelectRandomNormal(random);
    }

    public GameObject SelectRandomNormal(System.Random random)
    {
        if (normalRooms == null || normalRooms.Length == 0)
        {
            return null;
        }

        int index = random != null ? random.Next(normalRooms.Length) : UnityEngine.Random.Range(0, normalRooms.Length);
        return normalRooms[index];
    }

    public bool IsConfigured()
    {
        return entryRoom != null && bossRoom != null && shopRoom != null && normalRooms != null && normalRooms.Length > 0;
    }
}
