using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyType
{
    Goblin,
    BeastMinor,
    StoneGiant,
    BodylessShadow,
    StoneStatue
}

public enum EnemyAttackType
{
    Melee,
    Ranged,
    Heavy
}

[Serializable]
public class DropEntry
{
    public GameObject prefab;
    [Range(0f, 1f)] public float chance = 1f;
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "Kaisen/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName = "Enemy";
    public float maxHP = 10f;
    public float damage = 1f;
    public float moveSpeed = 2f;
    public float detectionRange = 8f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public int expValue = 1;
    public bool isExtractable;
    public EnemyType enemyType = EnemyType.Goblin;
    public EnemyAttackType attackType = EnemyAttackType.Melee;
    public List<DropEntry> dropTable = new List<DropEntry>();
}
