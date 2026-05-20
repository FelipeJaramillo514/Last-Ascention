using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    public const float DefaultHospitalTarget = 50000f;

    private static string SavePath
    {
        get { return Path.Combine(Application.persistentDataPath, "kaisen_save.json"); }
    }

    public static SaveData CreateNewSave()
    {
        SaveData data = new SaveData();
        Normalize(data);
        return data;
    }

    public static void Save(SaveData data)
    {
        if (data == null)
        {
            return;
        }

        Normalize(data);
        data.hasSave = true;
        data.lastSaveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        PlayerPrefs.SetString("hasSave", "true");
        PlayerPrefs.Save();
    }

    public static SaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Normalize(data);
            return data;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("SaveSystem.Load failed. Returning null. " + exception.Message);
            return null;
        }
    }

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }

        PlayerPrefs.DeleteKey("hasSave");
        PlayerPrefs.Save();
    }

    public static SaveData Clone(SaveData source)
    {
        if (source == null)
        {
            return CreateNewSave();
        }

        SaveData clone = new SaveData();
        clone.hasSave = source.hasSave;
        clone.systemLevel = source.systemLevel;
        clone.strength = source.strength;
        clone.agility = source.agility;
        clone.resistance = source.resistance;
        clone.perception = source.perception;
        clone.currentGold = source.currentGold;
        clone.hospitalDebt = source.hospitalDebt;
        clone.liraHealthPercent = source.liraHealthPercent;
        clone.shadowExtractionUnlocked = source.shadowExtractionUnlocked;
        clone.maxShadowSlots = source.maxShadowSlots;
        clone.purchasedUpgrades = source.purchasedUpgrades != null ? new List<string>(source.purchasedUpgrades) : new List<string>();
        clone.totalRunsCompleted = source.totalRunsCompleted;
        clone.totalEnemiesKilled = source.totalEnemiesKilled;
        clone.lastSaveDateTime = source.lastSaveDateTime;
        clone.skillPoints = source.skillPoints;
        clone.officialRank = source.officialRank;
        clone.totalGoldEarned = source.totalGoldEarned;
        clone.goldForHospital = source.goldForHospital;
        clone.runsSinceHospitalReset = source.runsSinceHospitalReset;
        clone.shadowUnlockAnnouncementShown = source.shadowUnlockAnnouncementShown;
        clone.bonusMaxHP = source.bonusMaxHP;
        clone.swordDamageBonusPercent = source.swordDamageBonusPercent;
        clone.bonusDodgeCooldownReduction = source.bonusDodgeCooldownReduction;
        Normalize(clone);
        return clone;
    }

    public static void Normalize(SaveData data)
    {
        if (data == null)
        {
            return;
        }

        if (data.purchasedUpgrades == null)
        {
            data.purchasedUpgrades = new List<string>();
        }

        data.systemLevel = Mathf.Max(1, data.systemLevel);
        data.maxShadowSlots = Mathf.Max(1, data.maxShadowSlots);
        data.officialRank = string.IsNullOrWhiteSpace(data.officialRank) ? "E" : data.officialRank;
        data.goldForHospital = Mathf.Clamp(data.goldForHospital, 0f, DefaultHospitalTarget);
        data.hospitalDebt = Mathf.Max(0f, DefaultHospitalTarget - data.goldForHospital);
        data.liraHealthPercent = DefaultHospitalTarget <= 0f ? 0f : Mathf.Clamp01(data.goldForHospital / DefaultHospitalTarget);
    }

    public static KaisenStats BuildStats(SaveData data)
    {
        Normalize(data);
        KaisenStats stats = new KaisenStats();
        stats.systemLevel = data.systemLevel;
        stats.strength = data.strength;
        stats.agility = data.agility;
        stats.resistance = data.resistance;
        stats.perception = data.perception;
        stats.skillPoints = data.skillPoints;
        stats.officialRank = data.officialRank;
        stats.currentGold = data.currentGold;
        stats.hospitalDebt = data.hospitalDebt;
        stats.bonusMaxHP = data.bonusMaxHP;
        stats.swordDamageBonusPercent = data.swordDamageBonusPercent;
        stats.bonusDodgeCooldownReduction = data.bonusDodgeCooldownReduction;
        return stats;
    }

    public static void ApplyStatsToSave(SaveData target, KaisenStats stats)
    {
        if (target == null || stats == null)
        {
            return;
        }

        target.systemLevel = stats.systemLevel;
        target.strength = stats.strength;
        target.agility = stats.agility;
        target.resistance = stats.resistance;
        target.perception = stats.perception;
        target.currentGold = stats.currentGold;
        target.hospitalDebt = stats.hospitalDebt;
        target.skillPoints = stats.skillPoints;
        target.officialRank = stats.officialRank;
        target.bonusMaxHP = stats.bonusMaxHP;
        target.swordDamageBonusPercent = stats.swordDamageBonusPercent;
        target.bonusDodgeCooldownReduction = stats.bonusDodgeCooldownReduction;
        Normalize(target);
    }
}
