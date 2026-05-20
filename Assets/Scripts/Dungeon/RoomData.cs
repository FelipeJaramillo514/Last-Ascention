using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemySpawnEntry
{
    public EnemyData data;
    public int minCount = 1;
    public int maxCount = 1;
    public float weight = 1f;
}

[CreateAssetMenu(fileName = "RoomData", menuName = "Kaisen/Dungeon/Room Data")]
public class RoomData : ScriptableObject
{
    public RoomType roomType = RoomType.Normal;
    public Vector2Int minSize = new Vector2Int(20, 15);
    public Vector2Int maxSize = new Vector2Int(20, 15);
    public List<EnemySpawnEntry> enemySpawnTable = new List<EnemySpawnEntry>();
    public bool canHaveAmbush;
    public GameObject[] possiblePropPrefabs;
}
