using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
[Serializable]
public class influenceRule
{
    public ConditionContainer conditions;//can be empty
    [Tooltip("base influence generated if the conditions are met")]
    public float baseInfluence = 1.0f;
    [Header("modifiers")]
    [Tooltip("you can check for a value(never a relationship among participants of an action and generate influence based on this value (make more influence the more a character likes another for example)")]
    public List<influenceMod> Modifiers;

    internal float getAffinityMod(ActionInstance actionInstance)
    {
        float AffinityMod = baseInfluence;
        var ActorWorldModel = actionInstance.InvolvedCharacters[Role.actor].worldModel;
        foreach (var condition in conditions.conditions) if (!condition.isMet(actionInstance.InvolvedCharacters, ActorWorldModel)) return 1.0f;//It might be good to put this value in the hands of the user, but the UI is busy enough as it is. Good once/if we have custom editors.
        foreach (var mod in Modifiers)
        {
            float traitValue;
            if (!ActorWorldModel.Characters[actionInstance.InvolvedCharacters[mod.holder]].traits.TryGetValue(mod.trait, out traitValue)) continue;
            AffinityMod *= mod.curve.Evaluate(traitValue);
        }
        return AffinityMod;
    }
}

[Serializable]
public class influenceMod//The nested implentation is for dancing around the inspector limitations. Might trigger the serialization depth issue again.
{
    
    public modValueSource source;//should be hidden away by the custom inspector
    public AnimationCurve curve;//curve of the influence this value generate

    internal float Evaluate(WorldModel model, Dictionary<Role, Character> involvedCharacters)
    {
        var value =  source.EvaluateValue(model, involvedCharacters);
        if (value < 0.0f) return 1.0f;//this a fallback to handle bad bidings;
        return curve.Evaluate(value);
    }
}

public abstract class modValueSource
{
    internal abstract float EvaluateValue(WorldModel model, Dictionary<Role, Character> involvedCharacters);
    internal abstract float GetActualValue(Dictionary<Role, Character> involvedCharacters);
}
public class TraitModValueSource : modValueSource
{
    public Trait trait;
    public Role holder = Role.allInvolved;
    internal override float EvaluateValue(WorldModel model, Dictionary<Role, Character> involvedCharacters)
    {
        Character holderChar;
        if (!involvedCharacters.TryGetValue(holder, out holderChar)) return -1.0f;
        CharModel charModel;
        if (!model.Characters.TryGetValue(holderChar, out charModel)) return 0.5f;//assume average
        float traitValue;
        if (!charModel.traits.TryGetValue(trait, out traitValue)) return 0.5f;//assume average
        return traitValue;
    }

    internal override float GetActualValue(Dictionary<Role, Character> involvedCharacters)
    {
        throw new NotImplementedException();
    }
}
public class OpinionTraitModSource : modValueSource
{
    public Role OpinionHolder;
    public Role TraitHolder;
    internal override float EvaluateValue(WorldModel model, Dictionary<Role, Character> involvedCharacters)
    {
        throw new NotImplementedException();
    }

    internal override float GetActualValue(Dictionary<Role, Character> involvedCharacters)
    {
        throw new NotImplementedException();
    }
}