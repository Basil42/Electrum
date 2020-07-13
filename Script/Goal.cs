using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RotaryHeart.Lib.SerializableDictionary;
using TMPro;

[Serializable]
public class Goal
{
    public float importance;
    public List<Condition> targetConditions;//conditions the holder of that goal try to make true
    public List<Condition> DiscardConditions;//conditions that if fullfilled would make the goal holder discard it. 
    public RoleCharacterDictionnary involvedCharacter;//creates more serialization problems. At this point it's just worthwhile to make custom editor code for the whole thing.

    public Goal(List<Condition> _target,List<Condition> _discardConditions, RoleCharacterDictionnary _characters,float _importance)
    {
        importance = _importance;
        targetConditions = _target;
        DiscardConditions = _discardConditions;
        involvedCharacter = _characters;
        
    }
    public float getProgress(WorldModel start, WorldModel finish)
    {
        List<float> individualProgress = new List<float>();
        float startDist;
        float endDist;
        for (int i = 0; i < targetConditions.Count; i++)
        {
            startDist = targetConditions[i].getDistance(start, involvedCharacter);
            endDist = targetConditions[i].getDistance(finish, involvedCharacter);
            if (startDist < 0.0f && endDist < 0.0f)individualProgress.Add(0.0f);//condition already met and not broken afterwards, it will create some problems on edge cases where actively trying to overshoot a condition is useful.
            else if (endDist < 0.0f)individualProgress.Add(startDist);//condition met at destination, we for now give the same value to meeting the condition, ignoring how valuable or not overshooting could be(it prevent more problem that it will cause, probably)
            else individualProgress.Add(startDist - endDist);//regular distance, negative value indicate the new worldstate is further away form meeting this condition.
        }
        float total = 0.0f;
        foreach(var value in individualProgress)
        {
            total += value;
        }
        return total;
    }
}
[Serializable]
public class RoleCharacterDictionnary : SerializableDictionaryBase<Role,Character> { }
