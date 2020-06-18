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
    List<Goal> m_goals =new List<Goal>();
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
                if (EstimateBindingsAttractiveness(instance) < Electrum.affinityTreshold)
                {
                    ActionCandidates.Remove(instance);//bindings are expected to be too bad to consider taking the action 
                }
                else
                {
                    /*calculate expected utility of the actions, this were we can add recursion.
                    * But as it is now, the engine already has more feature than Ensemble, so not a priority 
                    * (Also I'd like to see how much garbage it currently generates)*/
                    EstimateUtility(ActionCandidates[i]);
                    i++;
                }
            }
            finalActions.AddRange(ActionCandidates);
            //here we might have the engine fit for narrative goals, we probably want to sort the actions first though;

        }
        throw new NotImplementedException();//REMEMBER TO REMOVE THIS AFTER THE FUNCTION IS DONE
    }

    private void EstimateUtility(ActionInstance actionInstance)
    {
        if (m_goals.Count == 0) return;//the character will simply rank action by affinity, should rarely happen, as some goals are probably universal, if not acetively sought
        //run effect on the worldmodel
        var newModel = worldModel.Copy();
        actionInstance.VirtualRun(ref newModel);

        throw new NotImplementedException();
        
    }

    private float EstimateBindingsAttractiveness(ActionInstance instance)
    {
        var OpenCandidateList = new List<CharModel>(worldModel.Characters.Values);
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
        for(int i = 0;i < possibleBindingsinstances.Count; i++)
        {
            Attractiveness *= Mathf.Pow(possibleBindingsinstances[i].Affinity, (likelyhoodWeights[i] / likelyhoodWeightCumulated));
        }
        instance.Affinity = Attractiveness;
        return Attractiveness;
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
        worldModel.Characters.Add(this, model);
    }
}



public struct Goal
{
    /*NOTE: the logic of goals is still open to be changed with 0 refactoring, as I didn't impplement any logic around it yet
    *Condition/influenceRules structure might be easier to author and be generally more flexible*/

    //propriety of the worldmodel that the character tries to make true.
    WorldModel target; //the fields of this world model will be compared to the character ones
    int maxCost; //the cost the character is willing to pay to accomplish this goal, used to exclude sequence of action that are accomplishing the goal to the detriment of everything else.
    int investement;//The cost value the character has already sunk in the pursuit of that goal.

    //to let character reason on the goals of other character, ways of mesuring similarities between two goals are required
    
}
public readonly struct WorldModel
{
    //public readonly List<CharModel> Characters;
    public readonly Dictionary<Character,CharModel> Characters;
    private WorldModel(Dictionary<Character,CharModel> characters)//this constructor will also copy every character model
    {
        Characters = new Dictionary<Character, CharModel>(characters);//this should work because charmodel is a value type
    }
    //utility to navigate and log info about this list or get a specific collection of information
    public WorldModel Copy()
    {
        var result = new WorldModel(Characters);
        return result;
    }
    
}
public struct CharModel//model that the character have of each other
{
    public Character Character;
    public float trust;//how much the character holding the model trusts information coming from the associated character
    public TraitValueDictionary traits; //traits the character percieves/thinks the target character has, this is a simpler approach than what we intended and the character cannot hold contrarian opinions
    
    public List<Goal> goals; // goals the character thinks the target has.
    public RelationShipDictionary Relationships;
    //we could add estimation of the model the target character holds of this character
}
[Serializable]
public class RelationshipArray//this class only serve to go around a quirk of unity serialization interaction with the serialized dictionaries
{
    public List<RelationshipType> relationships;
}
[Serializable]
public class RelationShipDictionary : SerializableDictionaryBase<Character, RelationshipArray>{ }



