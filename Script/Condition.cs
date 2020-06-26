using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class Condition//used to check if a condition is fullfilled, can be evaluated in the "real" world state, a character world state or a virtual worldstate used for decision making
{
    /*TODO: see if a custom inspector will work with the serialisation to make the inspector cleaner*/
    public InfoType type = InfoType.relationship; //type of information, for example a relation;
    public Role holder;//initially a list, but created complications. having multiple conditions should take care of most cases, but we might want to have more complex conditions later on.
    public Role recipient;//should be empty if the info is a trait
    [Header("traits only")]
    public Trait trait;
    public ValueComparisonOperator Operator = ValueComparisonOperator.Equals;
    [Tooltip("only for the equal operator, define how much more or less the provided value can be compared to the condition value to pass the check.")]
    public float tolerance = 0.1f;
    public float value = 0.0f;
    [Header("relationship only")]
    public RelationshipType relationshipType;
    public bool BoolValue;
    //add values for goals and opinions here, vastly more complicated


    internal bool isMet(ActionInstance instance, ref WorldModel worldModel)//indicate if the condition is met in the perspective of the character holding this worldmodel uses
    {
        CharModel holderModel;
        if (!worldModel.Characters.TryGetValue(instance.InvolvedCharacters[holder], out holderModel)) return false;//sometimes unbound roles will be checked against in influencerules, but it might hide poorly configured condition order
        CharModel recipientModel;
        if (!worldModel.Characters.TryGetValue(instance.InvolvedCharacters[recipient], out recipientModel) && type == InfoType.relationship) return false;
        switch (type)
        {
            case InfoType.relationship:
                if (!holderModel.Relationships.ContainsKey(recipientModel.Character))
                {
                    return !BoolValue;

                }
                return BoolValue == holderModel.Relationships[recipientModel.Character].relationships.Exists(x => x == relationshipType);
            case InfoType.trait:
                float TraitValue;
                if (!holderModel.traits.TryGetValue(trait, out TraitValue)) return Operator == ValueComparisonOperator.lessThan;
                switch (Operator)
                {
                    case ValueComparisonOperator.lessThan:
                        return TraitValue < value;
                    case ValueComparisonOperator.MoreThan:
                        return TraitValue > value;
                    case ValueComparisonOperator.Equals:
                        return (TraitValue < value - tolerance && TraitValue > value + tolerance);//not very elegant, you usually would rather use two conditions anyway.
                    default:
                        Debug.LogError("Attempting to use an unimplented operator: " + Operator.ToString());
                        return false;
                }
            case InfoType.opinion:
                Debug.LogError("Warning, do not use conditions involving opinions that will be evaluated from the POV of a character.\n Used in action: " + instance.Template.name);
                return false;
            default:
                Debug.LogError("unimplemented Information type while checking if a condition was met. Type : " + type.ToString());
                return false;
        }
    }
    internal bool isMet(ActionInstance instance)//check if this action meets the condition in the "real world", might be useful sometimes, left unimplemented for now
    {
        throw new NotImplementedException();
    }
}
[Serializable]
public class influenceRule
{
    public List<Condition> conditions;//can be empty
    [Tooltip("base influence generated if the conditions are met")]
    public float baseInfluence = 1.0f;
    [Header("modifiers")]
    [Tooltip("you can check for a value(never a relationship among participants of an action and generate influence based on this value (make more influence the more a character likes another for example)")]
    public List<influenceMod> Modifiers;

    internal float getAffinityMod(ActionInstance actionInstance)
    {
        float AffinityMod = baseInfluence;
        var ActorWorldModel = actionInstance.InvolvedCharacters[Role.actor].worldModel;
        foreach (var condition in conditions) if (!condition.isMet(actionInstance, ref ActorWorldModel)) return 1.0f;//It might be good to put this value in the hands of the user, but the UI is busy enough as it is. Good once/if we have custom editors.
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
