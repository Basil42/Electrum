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
    public ConditionContainer targetConditions;//conditions the holder of that goal try to make true
    public ConditionContainer DiscardConditions;//conditions that if fullfilled would make the goal holder discard it. 
    public RoleCharacterDictionnary involvedCharacter;//creates more serialization problems. At this point it's just worthwhile to make custom editor code for the whole thing.

    public Goal(ConditionContainer _target,ConditionContainer _discardConditions, RoleCharacterDictionnary _characters,float _importance)
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
        
        for (int i = 0; i < targetConditions.conditions.Count; i++)
        {
            var condition = targetConditions.conditions[i];
            startDist = condition.getDistance(start, involvedCharacter.Clone());//cloning function is used to deal with type conversion issue with serializable dictionaries, hopefully not that expensive
            endDist = condition.getDistance(finish, involvedCharacter.Clone());
            if (startDist < 0.0f && endDist < 0.0f)individualProgress.Add(0.0f);//condition already met and not broken afterwards, it will create some problems on edge cases where actively trying to overshoot a condition is useful.
            else if (endDist < 0.0f)individualProgress.Add(startDist);//condition met at destination, we for now give the same value to meeting the condition, ignoring how valuable or not overshooting could be(it prevent more problem that it will cause, probably)
            else individualProgress.Add(startDist - endDist);//regular distance, negative value indicate the new worldstate is further away form meeting this condition.
        }
        float squaredTotal = 0.0f;
        foreach(var value in individualProgress)
        {
            squaredTotal += value*value;
        }
        return Mathf.Sqrt(squaredTotal);
    }
}
[Serializable]
public class RoleCharacterDictionnary : SerializableDictionaryBase<Role,Character> { }
