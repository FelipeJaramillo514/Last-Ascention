using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventBus
{
    private static readonly Dictionary<Type, Delegate> Subscribers = new Dictionary<Type, Delegate>();

    public static void Subscribe<T>(Action<T> callback)
    {
        Type eventType = typeof(T);
        if (Subscribers.TryGetValue(eventType, out Delegate existing))
        {
            Subscribers[eventType] = Delegate.Combine(existing, callback);
            return;
        }

        Subscribers[eventType] = callback;
    }

    public static void Unsubscribe<T>(Action<T> callback)
    {
        Type eventType = typeof(T);
        if (!Subscribers.TryGetValue(eventType, out Delegate existing))
        {
            return;
        }

        Delegate updated = Delegate.Remove(existing, callback);
        if (updated == null)
        {
            Subscribers.Remove(eventType);
            return;
        }

        Subscribers[eventType] = updated;
    }

    public static void Publish<T>(T eventData)
    {
        Type eventType = typeof(T);
        if (!Subscribers.TryGetValue(eventType, out Delegate existing))
        {
            return;
        }

        Action<T> callback = existing as Action<T>;
        callback?.Invoke(eventData);
    }
}

public class EnemyDiedEvent
{
    public GameObject enemy { get; }
    public Vector2 position { get; }
    public int expValue { get; }

    public EnemyDiedEvent(GameObject enemy, Vector2 position, int expValue)
    {
        this.enemy = enemy;
        this.position = position;
        this.expValue = expValue;
    }
}

public class PlayerDamagedEvent
{
    public float damage { get; }
    public float currentHP { get; }

    public PlayerDamagedEvent(float damage, float currentHP)
    {
        this.damage = damage;
        this.currentHP = currentHP;
    }
}

public class PlayerLevelUpEvent
{
    public int previousLevel { get; }
    public int newLevel { get; }
    public StatChanges statChanges { get; }

    public PlayerLevelUpEvent(int previousLevel, int newLevel, StatChanges statChanges)
    {
        this.previousLevel = previousLevel;
        this.newLevel = newLevel;
        this.statChanges = statChanges;
    }

    public PlayerLevelUpEvent(int newLevel)
    {
        previousLevel = Mathf.Max(0, newLevel - 1);
        this.newLevel = newLevel;
        statChanges = default(StatChanges);
    }
}

public class RoomClearedEvent
{
    public DungeonRoom room { get; }

    public RoomClearedEvent(DungeonRoom room)
    {
        this.room = room;
    }
}

public class RoomVisitedEvent
{
    public DungeonRoom room { get; }

    public RoomVisitedEvent(DungeonRoom room)
    {
        this.room = room;
    }
}

public class PenaltyActivatedEvent
{
    public float duration { get; }
    public float maxHpMultiplier { get; }
    public float moveSpeedMultiplier { get; }

    public PenaltyActivatedEvent(float duration, float maxHpMultiplier, float moveSpeedMultiplier)
    {
        this.duration = duration;
        this.maxHpMultiplier = maxHpMultiplier;
        this.moveSpeedMultiplier = moveSpeedMultiplier;
    }
}

public class WeaponFiredEvent
{
    public WeaponData weaponData { get; }
    public int remainingAmmo { get; }
    public int slotIndex { get; }

    public WeaponFiredEvent(WeaponData weaponData, int remainingAmmo, int slotIndex)
    {
        this.weaponData = weaponData;
        this.remainingAmmo = remainingAmmo;
        this.slotIndex = slotIndex;
    }
}

public class WeaponEmptyEvent
{
    public WeaponData weaponData { get; }
    public int slotIndex { get; }

    public WeaponEmptyEvent(WeaponData weaponData, int slotIndex)
    {
        this.weaponData = weaponData;
        this.slotIndex = slotIndex;
    }
}

public class WeaponSwappedEvent
{
    public WeaponData weaponData { get; }
    public int activeSlot { get; }

    public WeaponSwappedEvent(WeaponData weaponData, int activeSlot)
    {
        this.weaponData = weaponData;
        this.activeSlot = activeSlot;
    }
}

public class GoldChangedEvent
{
    public float currentGold { get; }
    public float delta { get; }

    public GoldChangedEvent(float currentGold, float delta)
    {
        this.currentGold = currentGold;
        this.delta = delta;
    }
}

public class BossPhase2Event
{
    public BossBase boss { get; }

    public BossPhase2Event(BossBase boss)
    {
        this.boss = boss;
    }
}

public class ShadowSummonedEvent
{
    public ShadowSoldier soldier { get; }
    public DungeonRoom sourceRoom { get; }
    public Vector2 position { get; }

    public ShadowSummonedEvent(ShadowSoldier soldier)
        : this(soldier, null, soldier != null ? (Vector2)soldier.transform.position : Vector2.zero)
    {
    }

    public ShadowSummonedEvent(ShadowSoldier soldier, DungeonRoom sourceRoom, Vector2 position)
    {
        this.soldier = soldier;
        this.sourceRoom = sourceRoom;
        this.position = position;
    }
}

public class ShadowExtractedEvent
{
    public DungeonRoom sourceRoom { get; }
    public Vector2 position { get; }
    public string soulName { get; }
    public int storedCount { get; }

    public ShadowExtractedEvent(DungeonRoom sourceRoom, Vector2 position, string soulName, int storedCount)
    {
        this.sourceRoom = sourceRoom;
        this.position = position;
        this.soulName = soulName;
        this.storedCount = storedCount;
    }
}

public class ShadowInventoryChangedEvent
{
    public int storedSouls { get; }
    public int activeShadows { get; }
    public int maxActiveShadows { get; }

    public ShadowInventoryChangedEvent(int storedSouls, int activeShadows, int maxActiveShadows)
    {
        this.storedSouls = storedSouls;
        this.activeShadows = activeShadows;
        this.maxActiveShadows = maxActiveShadows;
    }
}

public class ShadowDiedEvent
{
    public ShadowSoldier soldier { get; }

    public ShadowDiedEvent(ShadowSoldier soldier)
    {
        this.soldier = soldier;
    }
}

public class PlayerDeathEvent
{
}
