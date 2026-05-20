using System;
using UnityEngine;

[Serializable]
public struct StatChanges
{
    public int previousLevel;
    public int newLevel;
    public float strengthDelta;
    public float agilityDelta;
    public float resistanceDelta;
    public float perceptionDelta;
    public int skillPointsDelta;
    public float currentStrength;
    public float currentAgility;
    public float currentResistance;
    public float currentPerception;
    public int currentSkillPoints;

    public bool HasAnyChange
    {
        get
        {
            return !Mathf.Approximately(strengthDelta, 0f)
                || !Mathf.Approximately(agilityDelta, 0f)
                || !Mathf.Approximately(resistanceDelta, 0f)
                || !Mathf.Approximately(perceptionDelta, 0f)
                || skillPointsDelta != 0;
        }
    }

    public bool HasSkillPointGain
    {
        get { return skillPointsDelta > 0; }
    }
}
