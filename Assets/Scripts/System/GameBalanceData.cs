using UnityEngine;

[CreateAssetMenu(fileName = "GameBalanceData", menuName = "Kaisen/Balance Data")]
public class GameBalanceData : ScriptableObject
{
    private static GameBalanceData runtimeInstance;

    [Header("Progression")]
    public float baseEXPToLevel = 100f;
    public float expScalePerLevel = 1.3f;

    [Header("Economy")]
    public float goldRewardMultiplier = 1f;

    [Header("Combat")]
    public float enemyDamageMultiplier = 1f;
    public float bossHPMultiplier = 1f;

    [Header("Dungeon")]
    public AnimationCurve difficultyByFloor = AnimationCurve.Linear(1f, 1f, 5f, 1.65f);
    public int roomsPerFloor_Min = 8;
    public int roomsPerFloor_Max = 12;

    public static GameBalanceData Instance
    {
        get
        {
            if (runtimeInstance == null)
            {
                runtimeInstance = Resources.Load<GameBalanceData>("GameBalanceData");
                if (runtimeInstance == null)
                {
                    runtimeInstance = CreateInstance<GameBalanceData>();
                    runtimeInstance.name = "RuntimeGameBalanceData";
                    runtimeInstance.ResetToDefaults();
                }
            }

            return runtimeInstance;
        }
    }

    public float EvaluateDifficulty(int floorNumber)
    {
        AnimationCurve curve = difficultyByFloor;
        if (curve == null || curve.length == 0)
        {
            return 1f;
        }

        return Mathf.Max(0.1f, curve.Evaluate(Mathf.Max(1, floorNumber)));
    }

    public int GetRoomsForFloor(System.Random random)
    {
        int minRooms = Mathf.Max(1, roomsPerFloor_Min);
        int maxRooms = Mathf.Max(minRooms, roomsPerFloor_Max);
        if (random == null || minRooms == maxRooms)
        {
            return minRooms;
        }

        return random.Next(minRooms, maxRooms + 1);
    }

    public float GetScaledEnemyDamage(float baseDamage, int floorNumber)
    {
        return baseDamage * Mathf.Max(0.1f, enemyDamageMultiplier) * EvaluateDifficulty(floorNumber);
    }

    public float GetScaledBossHP(float baseHp, int floorNumber)
    {
        return baseHp * Mathf.Max(0.1f, bossHPMultiplier) * EvaluateDifficulty(floorNumber);
    }

    public void ResetToDefaults()
    {
        baseEXPToLevel = 100f;
        expScalePerLevel = 1.3f;
        goldRewardMultiplier = 1f;
        enemyDamageMultiplier = 1f;
        bossHPMultiplier = 1f;
        roomsPerFloor_Min = 8;
        roomsPerFloor_Max = 12;
        difficultyByFloor = AnimationCurve.Linear(1f, 1f, 5f, 1.65f);
    }
}
