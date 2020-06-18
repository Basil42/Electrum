using RotaryHeart.Lib.SerializableDictionary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;

[Serializable]
public class AffinityDictionary : SerializableDictionaryBase<Trait, AnimationCurve> { }


[CreateAssetMenu(fileName ="new Action", menuName = "Electrum/Action")]
public class Action : ScriptableObject
{
    [Tooltip("traits, along with its influence on the attractivity of the action for a character who have these trait")]
    public AffinityDictionary affinitiesRules;
    [Tooltip("Mandatory role the action requires to be taken")]
    public List<RoleBinding> ActorControlledRoles;
    [Tooltip("Extra role of character that the action may involve")]
    public List<RoleBinding> EngineControlledRoles;
    [Tooltip("potential effects of the action")]
    public List<ActionEffect> effects;
    
}
public class ActionInstance
{
    public Action Template;
    public Dictionary<Role, Character> InvolvedCharacters;//I was not sure wether I should use Charater or role as key, code will be faster and clearer this way I think, it will need to be refactored to allow multiple bindings to the same role.
    public float Affinity = 1.0f;
    public float ExpectedImmediateUtility = 0.0f;
    public float ExpectedTotalUtility = 0.0f;//based on the expected utility of future actions after that one.
    public ActionInstance(Action template, Dictionary<Role, Character> involvedCharacters)
    {
        Template = template;
        InvolvedCharacters = new Dictionary<Role, Character>(involvedCharacters);
    }
    public ActionInstance(ActionInstance original)
    {
        Template = original.Template;
        InvolvedCharacters = new Dictionary<Role, Character>(original.InvolvedCharacters);
        Affinity = original.Affinity;
    }

    

    



    /*Used to adjust the affinity of the instance based on the attractiveness of the bindings the Character has control over.*/
    internal void RunCharacterControlledPreferenceRules()
    {
        foreach (var binding in Template.ActorControlledRoles)
        {
            foreach (var rule in binding.DesirabilityRules)
            {
                Affinity *= rule.getAffinityMod(this);
            }
        }
    }

    internal void RunEngineControlledPreferenceRules()
    {
        foreach(var binding in Template.EngineControlledRoles)
        {
            foreach(var rule in binding.DesirabilityRules)
            {
                Affinity *= rule.getAffinityMod(this);
            }
        }
    }

    internal float EvaluateLikelyhoodWeight()
    {
        float likelyhoodWeight = 1.0f;
        foreach(var binding in Template.EngineControlledRoles)
        {
            foreach(var rule in binding.LikelyhoodRules)
            {
                likelyhoodWeight *= rule.getAffinityMod(this);
            }
        }
        return likelyhoodWeight;
    }

    internal void VirtualRun(ref WorldModel newModel)
    {
        throw new NotImplementedException();
    }
}



[Serializable]
public class ActionEffect
{
    public Effect effect;
    public List<Condition> conditions;//condition that must be met for the effect to be applied, allow for more nuance with smaller authoring overhead, condition is marked unmet if it refers to an unbound role
    public List<influenceRule> influenceRules;
}
[Serializable]
public class RoleBinding
{
    public Role role = Role.witness;
    public bool Mandatory= true;//is this role allowed to be unbound ?
    //to do latter, add possibility to optionally allow several character to be bound to the same role
    [HideInInspector]public Character holder;
    [Tooltip("take care to not reference unbound roles in these, mandatory role up the list and the actor are usually safe to reference")]
    public List<Condition> conditions;//condition that must be fullfilled for the role to be bound, reference to the role will test the candidate state
    public List<influenceRule> DesirabilityRules;//influenceRules that let the character mesure how desirable a binding is (if the actor controls that bindings
    public List<influenceRule> LikelyhoodRules;//influence rules that determine the likelyhood for a set of bindings not controlled by the actor to be picked, should be empty for an Actor controleld bindings, as they would be the same as the desirability ones

}
public enum Role
{
    none,
    actor,
    target,
    witness,
    assistant,
    hinderer,
    allInvolved
}
public enum ValueChangeOperator
{
    add,
    set
}
public enum ValueComparisonOperator//removing equals might be a good call as floats are rarely equal anyways, a range can be expressed with two conditions (and we could easily have interface to author a range condition)
{
    lessThan,
    MoreThan,
    Equals
}
[Serializable]
public class Condition//used to check if a condition is fullfilled, can be evaluated in the "real" world state, a character world state or a virtual worldstate used for decision making
{
    public InfoType type = InfoType.relationship; //type of information, for example a relation;
    public Role holder;//initially a list, but created complications. having multiple conditions should take care of most cases, but we might want to have more complex conditions later on.
    public Role recipient;//should be empty if the info is a trait
    [Header("traits only")]
    public Trait trait;
    public ValueComparisonOperator Operator = ValueComparisonOperator.Equals;
    [Tooltip("only for the equal operator, define how much more or less the provided value can be compared to the condition value to pass the check.")]
    public float tolerance = 0.1f;
    public float value =0.0f;
    [Header("relationship only")]
    public bool BoolValue;
    //add values for goals and opinions here, vastly more complicated
    

    internal bool isMet(ActionInstance instance,ref WorldModel worldModel)//indicate if the condition is met in the perspective of the character holding this worldmodel uses
    {
        CharModel holderModel;
        if (!worldModel.Characters.TryGetValue(instance.InvolvedCharacters[holder], out holderModel)) return false;//sometimes unbound roles will be checked against in influencerules, but it might hide poorly configured condition order
        CharModel recipientModel;
        if (!worldModel.Characters.TryGetValue(instance.InvolvedCharacters[recipient], out recipientModel) && type == InfoType.relationship) return false;
        switch (type)
        {
            case InfoType.relationship:
                return (BoolValue == holderModel.Relationships.ContainsKey(recipientModel.Character));
            case InfoType.trait:
                float TraitValue;
                if (!holderModel.traits.TryGetValue(trait, out TraitValue)) return  Operator == ValueComparisonOperator.lessThan;
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
    List<influenceMod> Modifiers;

    internal float getAffinityMod(ActionInstance actionInstance)
    {
        float AffinityMod = baseInfluence;
        var ActorWorldModel = actionInstance.InvolvedCharacters[Role.actor].worldModel;
        foreach (var condition in conditions) if (!condition.isMet(actionInstance,ref ActorWorldModel))return 1.0f;//It might be good to put this value in the hands of the user, but the UI is busy enough as it is. Good once/if we have custom editors.
        foreach(var mod in Modifiers)
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