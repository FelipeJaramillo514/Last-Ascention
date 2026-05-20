using System;
using UnityEngine;

[Serializable]
public class KaisenStats
{
    public int systemLevel = 1;
    public float strength = 5f;
    public float agility = 5f;
    public float resistance = 5f;
    public float perception = 5f;
    public int skillPoints = 0;
    public string officialRank = "E";
    public float currentGold = 0f;
    public float hospitalDebt = 50000f;
    public float bonusMaxHP = 0f;
    public float swordDamageBonusPercent = 0f;
    public float bonusDodgeCooldownReduction = 0f;
    public float runAttackMultiplier = 1f;
    public float runMoveSpeedMultiplier = 1f;
    public float runMaxHpMultiplier = 1f;
    public float runPerceptionMultiplier = 1f;
    public float runDodgeCooldownMultiplier = 1f;
    public bool runVisualMuted = false;

    public float MaxHP => (50f + (resistance * 3f) + bonusMaxHP) * runMaxHpMultiplier;
    public float MoveSpeed => (5f + (agility * 0.05f)) * runMoveSpeedMultiplier;
    public float AttackDamage => (5f + (strength * 1.2f)) * runAttackMultiplier;
    public float DodgeCooldown => Mathf.Max(0.2f, (0.8f - (agility * 0.01f) - bonusDodgeCooldownReduction) * runDodgeCooldownMultiplier);
    public float EffectivePerception => perception * runPerceptionMultiplier;

    public static KaisenStats CreateDefault()
    {
        KaisenStats stats = new KaisenStats();
        stats.ResetRuntimeModifiers();
        return stats;
    }

    public KaisenStats Clone()
    {
        return new KaisenStats
        {
            systemLevel = systemLevel,
            strength = strength,
            agility = agility,
            resistance = resistance,
            perception = perception,
            skillPoints = skillPoints,
            officialRank = officialRank,
            currentGold = currentGold,
            hospitalDebt = hospitalDebt,
            bonusMaxHP = bonusMaxHP,
            swordDamageBonusPercent = swordDamageBonusPercent,
            bonusDodgeCooldownReduction = bonusDodgeCooldownReduction,
            runAttackMultiplier = runAttackMultiplier,
            runMoveSpeedMultiplier = runMoveSpeedMultiplier,
            runMaxHpMultiplier = runMaxHpMultiplier,
            runPerceptionMultiplier = runPerceptionMultiplier,
            runDodgeCooldownMultiplier = runDodgeCooldownMultiplier,
            runVisualMuted = runVisualMuted
        };
    }

    public void CopyFrom(KaisenStats source)
    {
        if (source == null)
        {
            return;
        }

        systemLevel = source.systemLevel;
        strength = source.strength;
        agility = source.agility;
        resistance = source.resistance;
        perception = source.perception;
        skillPoints = source.skillPoints;
        officialRank = source.officialRank;
        currentGold = source.currentGold;
        hospitalDebt = source.hospitalDebt;
        bonusMaxHP = source.bonusMaxHP;
        swordDamageBonusPercent = source.swordDamageBonusPercent;
        bonusDodgeCooldownReduction = source.bonusDodgeCooldownReduction;
        runAttackMultiplier = source.runAttackMultiplier;
        runMoveSpeedMultiplier = source.runMoveSpeedMultiplier;
        runMaxHpMultiplier = source.runMaxHpMultiplier;
        runPerceptionMultiplier = source.runPerceptionMultiplier;
        runDodgeCooldownMultiplier = source.runDodgeCooldownMultiplier;
        runVisualMuted = source.runVisualMuted;
    }

    public void ResetRuntimeModifiers()
    {
        runAttackMultiplier = 1f;
        runMoveSpeedMultiplier = 1f;
        runMaxHpMultiplier = 1f;
        runPerceptionMultiplier = 1f;
        runDodgeCooldownMultiplier = 1f;
        runVisualMuted = false;
    }
}
