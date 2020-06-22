using RotaryHeart.Lib.SerializableDictionary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using TMPro;
using UnityEngine;

//default reference for the serialized dictionary need to be setup in advance

[Serializable]
public class TraitValueDictionary : SerializableDictionaryBase<Trait, float> { }


[CreateAssetMenu(fileName = "new character", menuName = "Electrum/Character")]
public class Character : ScriptableObject
{
    //trait values must be between 0f and 1f
    public TraitValueDictionary m_traits;
    public RelationShipDictionary m_relationships;
    public List<Goal> m_goals =new List<Goal>();
    public WorldModel worldModel;


    public ActionInstance ChooseAction()
    {
        var finalActions = new List<ActionInstance>();
        foreach(var action in Electrum.actionSet.actions)
        {
            float BaseAffinity = 1.0f;
            //evaluate the affinity score of this action, discard all potential instances if below affinity treshold (defined in the engine settings object)
            foreach(var affinityRule in action.affinitiesRules)
            {
                if (!m_traits.ContainsKey(affinityRule.Key))continue;
                BaseAffinity *= affinityRule.Value.Evaluate(m_traits[affinityRule.Key]);//get the affinity modifier associated with this trait value.
            }
            if (BaseAffinity < Electrum.affinityTreshold) continue;
            //get candidate actionInstance with the different possible role bindings
            List<ActionInstance> ActionCandidates = FindControlledBindings(action);
            if (ActionCandidates == null) continue;//the action has been aborted;
            for (int i = 0; i < ActionCandidates.Count; )
            {
                ActionInstance instance = ActionCandidates[i];
                if (!EstimateBindingsQuality(instance, worldModel))
                {
                    ActionCandidates.Remove(instance);//bindings are expected to be too bad to consider taking the action 
                }
                else
                {
                    
                    i++;
                }
            }
            finalActions.AddRange(ActionCandidates);
            //here we might have the engine fit for narrative goals, we probably want to sort the actions first though;

        }
        throw new NotImplementedException();//REMEMBER TO REMOVE THIS AFTER THE FUNCTION IS DONE
    }

    

    private bool EstimateBindingsQuality(ActionInstance instance, WorldModel context)//calculate expected affinity from the instance, as well as expected utility, recursion would happen here to evaluate future actions
    {
        var OpenCandidateList = new List<CharModel>(context.Characters.Values);
        for(int i =0;i< OpenCandidateList.Count;)
        {
            if (instance.InvolvedCharacters.ContainsValue(OpenCandidateList[i].Character)) OpenCandidateList.RemoveAt(i);
            else i++;
        }
        instance.RunCharacterControlledPreferenceRules();
        var possibleBindingsinstances = RecursiveBindings(instance, OpenCandidateList, instance.Template.EngineControlledRoles);
        
        float likelyhoodWeightCumulated = 0.0f;
        var likelyhoodWeights = new List<float>();
        for(int i =0; i < possibleBindingsinstances.Count; i++)
        {
            likelyhoodWeights.Add(possibleBindingsinstances[i].EvaluateLikelyhoodWeight());
            likelyhoodWeightCumulated += likelyhoodWeights[i];
            possibleBindingsinstances[i].RunEngineControlledPreferenceRules();
        }
        float Attractiveness = 1.0f;
        List<float> BindingProbability = new List<float>();
        for(int i = 0;i < possibleBindingsinstances.Count; i++)
        {
            BindingProbability.Add(likelyhoodWeights[i] / likelyhoodWeightCumulated);
            Attractiveness *= Mathf.Pow(possibleBindingsinstances[i].Affinity, BindingProbability[i]);
        }
        instance.Affinity = Attractiveness;
        if(Attractiveness < Electrum.affinityTreshold)return false;//the action will be discarded, as it is too unattractive
        //utility estimation
        for(int i = 0; i < possibleBindingsinstances.Count; i++)
        {
            var newModel = possibleBindingsinstances[i].VirtualRun(context);
            foreach(var goal in m_goals)
            {
                switch (goal.type)
                {
                    case InfoType.relationship://This implementation is temporary, for "simplicity" (I know), I'll implement a way to estimate the distance to a desired relationship later (probably using the prerequisite for such a relationship
                        if(newModel.Characters[goal.Holder].Relationships.ContainsKey(goal.Recipient) == goal.BooleanValue)
                        {
                            possibleBindingsinstances[i].ExpectedImmediateUtility += goal.Importance;
                        }
                        break;
                    case InfoType.trait:
                        float newValue = 0.0f;
                        newModel.Characters[goal.Holder].traits.TryGetValue(goal.trait, out newValue);
                        float oldValue = 0.0f;
                        context.Characters[goal.Holder].traits.TryGetValue(goal.trait, out oldValue);
                        var goalTarget = goal.value;
                        switch (goal.Operator)
                        {
                            case ValueComparisonOperator.lessThan:
                                if (newValue < goal.value) possibleBindingsinstances[i].ExpectedImmediateUtility += goal.Importance;
                                else possibleBindingsinstances[i].ExpectedImmediateUtility += (oldValue - newValue) * goal.Importance;
                                break;
                            case ValueComparisonOperator.MoreThan:
                                if (newValue > goal.value) possibleBindingsinstances[i].ExpectedImmediateUtility += goal.Importance;
                                else possibleBindingsinstances[i].ExpectedImmediateUtility += (newValue - oldValue) * goal.Importance;
                                break;
                            case ValueComparisonOperator.Equals:
                                if ((newValue - goal.Tolerance) < goal.value && (newValue + goal.Tolerance) > goal.value) possibleBindingsinstances[i].ExpectedImmediateUtility += goal.Importance;
                                else possibleBindingsinstances[i].ExpectedImmediateUtility += (Mathf.Abs(newValue - goal.value) - Mathf.Abs(oldValue - goal.value)) * goal.Importance;
                                break;
                            default:
                                break;
                        }

                        break;
                    case InfoType.opinion:
                        throw new NotImplementedException();//this one is going to require big refactoring of some of the data structure, and generally custom Inspectors to be usable, so it will wait for now.
                        break;
                    default:
                        Debug.LogError("unimplemented goal type : " + goal.type.ToString());
                        break;
                }
            }
        }
        return true;
    }
    private List<ActionInstance> FindControlledBindings(Action action)//Looks for all valid bindings that are under the character control, Proably a major memory hog on some "domains" and also slow
    {
        var unboundInstance = new ActionInstance(action,new Dictionary<Role, Character>());
        unboundInstance.InvolvedCharacters.Add(Role.actor, this);//binds the acting character
        var OpenCandidateList = new List<CharModel>(worldModel.Characters.Values);//this should hopefully never contain a model of the actor 
        //This is one of the point where we could do manual memory management, this is going to make a LOT of garbage
        return RecursiveBindings(unboundInstance ,OpenCandidateList, action.ActorControlledRoles);
    }

    private List<ActionInstance> RecursiveBindings(ActionInstance instanceBase, List<CharModel> openCandidateList, List<RoleBinding> RoleSet, int depth=0)//the ActionInstance passed at the base of the recursion should only have the actor role bound
    {
        List<ActionInstance> boundInstances = new List<ActionInstance>();
        foreach(var character in openCandidateList)
        {
            ActionInstance RecursionInstance = new ActionInstance(instanceBase);
            RecursionInstance.InvolvedCharacters.Add(RoleSet[depth].role, character.Character);
            bool isValidCandidate = true;
            foreach(var condition in RoleSet[depth].conditions)
            {
                if(!condition.isMet(RecursionInstance,ref worldModel))//character did not meet a condition for this role
                {
                    isValidCandidate = false;
                    break;
                }
            }
            if (isValidCandidate)
            {
                var recursionOpenlist = new List<CharModel>(openCandidateList);
                recursionOpenlist.Remove(character);
                if (depth == RoleSet.Count - 1)//this is a completely bound instance, add it to the list to be returned
                {
                    //RecursionInstance.RunControlledPreferenceRules();
                    boundInstances.Add(RecursionInstance);
                }
                var additionalBindings = RecursiveBindings(RecursionInstance, recursionOpenlist, RoleSet, depth + 1);
                if (additionalBindings == null) return null;//action has been aborted
                boundInstances.AddRange(additionalBindings);//this will contain finished bindings only
            }
        }
        if (boundInstances.Count > Electrum.MaxBindingCandidatesAbort)
        {
            Debug.LogError("Error : " + name + " found more possible bindings combinations than the " + Electrum.MaxBindingCandidatesAbort + " limit on the " + instanceBase.Template.name + " action. Action discarded.");
            
            return null;

        }
        if (!instanceBase.Template.ActorControlledRoles[depth].Mandatory && depth != instanceBase.Template.ActorControlledRoles.Count -1)//creates an extra branch with current depth's role unassigned if it is not mandatory
        {
            var additionalBindings = RecursiveBindings(instanceBase, openCandidateList, RoleSet, depth + 1);
            if (additionalBindings == null) return null;//action is aborted
            boundInstances.AddRange(additionalBindings);
        }
        return boundInstances;//should contain all completely bound possible instances of the action. If empty it means no set of characters fullfilled all conditions
    }
    
    internal void ConstructownModel()//should only be called once per character, as it keeps references to the real values;
    {
        var model = new CharModel();
        model.Character = this;
        model.goals = m_goals;
        model.Relationships = m_relationships;
        model.traits = m_traits;
        model.trust = 1.0f;
        if (worldModel.Characters.ContainsKey(this)) worldModel.Characters[this] = model;
        else worldModel.Characters.Add(this, model);
    }
}


[Serializable]
public struct Goal
{
    /*I wanted to have a worldModel object to represent the target state of the goal, but world model can contain objects with goals in them, creating a serialization "loop" that Unity doesn't like.*/

    public InfoType type;
    public float Importance;//how important is the fullfillement of this goal for the character. when the progress towards a goal is converted into utility during action evaluation, the result is multiplied by the importance of the goal, ie progress on a highly important goal is more valuable and setback are more strongly avoided.
    public Character Holder;
    [Tooltip("leave empty for traits")]
    public Character Recipient;
    [Tooltip("leave default for relationships")]
    public float value;
    public ValueComparisonOperator Operator;
    [Tooltip("used for the equals operator")]
    public float Tolerance;
    [Tooltip("leave false if the goal is for a relationship NOT to exist")]
    public bool BooleanValue;
    public Trait trait;
    
   
    
}
[Serializable]
public class CharModelDictionary : SerializableDictionaryBase<Character, CharModel> { }
[Serializable]
public class WorldModel
{
    //public readonly List<CharModel> Characters;
    public CharModelDictionary Characters;
    private WorldModel(CharModelDictionary characters)//this constructor will also copy every character model
    {
        Characters = new CharModelDictionary();//this should work because charmodel is a value type
        foreach(var character in characters.Keys)
        {
            Characters.Add(character, characters[character].copy());
        }
    }
    //utility to navigate and log info about this list or get a specific collection of information
    public WorldModel Copy()
    {
        var result = new WorldModel(Characters);
        return result;
    }
    
}
[Serializable]
public class CharModel//model that the character have of each other
{
    /*These models are much simpler than what we initially intended (information source, and trustworthiness is not being tracked), but it will do for Ensemble level of performance*/
    public Character Character;
    public float trust;
    public TraitValueDictionary traits;
    
    public List<Goal> goals;
    public RelationShipDictionary Relationships;
    public WorldModel worldModel;//little afraid of infinite nested worldModel here, but it opens a whole category of goals.

    internal CharModel copy()
    {
        var result = new CharModel();
        result.Character = Character;
        result.trust = trust;
        result.traits = new TraitValueDictionary();
        result.traits.CopyFrom(traits);
        result.goals = new List<Goal>(goals);
        result.Relationships = new RelationShipDictionary();
        result.Relationships.CopyFrom(Relationships);
        return result;
    }
    public CharModel() { }
}
[Serializable]
public class RelationshipArray//this class only serve to go around a quirk of unity serialization interaction with the serialized dictionaries
{
    public List<RelationshipType> relationships;
}
[Serializable]
public class RelationShipDictionary : SerializableDictionaryBase<Character, RelationshipArray>{ }



