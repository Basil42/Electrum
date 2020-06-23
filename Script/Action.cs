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
    public float expectedProbability = 0.0f;
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

    internal WorldModel VirtualRun(WorldModel Model)//this should be run during the Engine controlled bindings attractiveness evaluation, to avoid having to look for these bindings twice.
    {
        var newModel = Model.Copy();
        
        throw new NotImplementedException();
    }
}



[Serializable]
public class ActionEffect
{
    public List<Effect> effects;
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
public class Effect // this class expresses changes to the world state
{
    public InfoType type;
    public List<Character> holders;
    public List<Character> recipients;//should be empty if the info is a trait
    public ValueChangeOperator Operator;//inspired by the ensemble way of doing things, it kind of work for authoring purposes
    public float value;
}
