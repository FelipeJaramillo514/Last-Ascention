using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public bool hasSave = true;
    public int systemLevel = 1;
    public float strength = 5f;
    public float agility = 5f;
    public float resistance = 5f;
    public float perception = 5f;
    public float currentGold = 0f;
    public float hospitalDebt = 50000f;
    public float liraHealthPercent = 0f;
    public bool shadowExtractionUnlocked = false;
    public int maxShadowSlots = 1;
    public List<string> purchasedUpgrades = new List<string>();
    public int totalRunsCompleted = 0;
    public int totalEnemiesKilled = 0;
    public string lastSaveDateTime = string.Empty;

    // Compatibility fields used by the current prototype systems.
    public int skillPoints = 0;
    public string officialRank = "E";
    public float totalGoldEarned = 0f;
    public float goldForHospital = 0f;
    public int runsSinceHospitalReset = 0;
    public bool shadowUnlockAnnouncementShown = false;
    public float bonusMaxHP = 0f;
    public float swordDamageBonusPercent = 0f;
    public float bonusDodgeCooldownReduction = 0f;
}
