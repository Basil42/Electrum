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
public class influenceMod//might accept other type of information than traits, but I don't see a use case not covered by conditions
{
    public Trait trait;
    public Role holder = Role.allInvolved;

    public AnimationCurve curve;//curve of the influence this value generate


}