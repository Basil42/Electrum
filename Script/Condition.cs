using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor;
using System.Runtime.Serialization;

[Serializable]
public class Condition//used to check if a condition is fullfilled, can be evaluated in the "real" world state, a character world state or a virtual worldstate used for decision making
{
    /*TODO: see if a custom inspector will work with the serialisation to make the inspector cleaner and more usable (possibly having inheritence on condition s too)*/
    //See custom propriety drawer
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
    [Header("opinion only")]
    OpinionType opinionType;

    internal bool isMet(ActionInstance instance, WorldModel worldModel)//indicate if the condition is met in the perspective of the character holding this worldmodel uses
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
    internal bool isMet(ActionInstance instance)//check if this action meets the condition in the "real world", to do
    {
        throw new NotImplementedException();
    }
    internal float getDistance(WorldModel worldModel, Dictionary<Role,Character> involvedCharacter)//negative results indicates the condition is fullfilled in this world state, this is a character evaluation
    {
        switch (type)
        {
            case InfoType.relationship:
                throw new NotImplementedException();//will require reference to "trigger rules". Make it so it targets the most imediate path towards that relationship (more advanced heuristics might be desirable later)
                break;
            case InfoType.trait:
                CharModel characterModel;
                float traitValue;
                if (!worldModel.Characters.TryGetValue(involvedCharacter[holder], out characterModel) || !characterModel.traits.TryGetValue(trait, out traitValue)) traitValue = 0.5f;// assume middling trait in the absence of information, I'm thinking of having a sort of "reputation" object that provides default assumption about a character.
                switch (Operator)
                {
                    case ValueComparisonOperator.lessThan:
                        return traitValue - value;
                    case ValueComparisonOperator.MoreThan:
                        return value - traitValue;
                    case ValueComparisonOperator.Equals:
                        return Mathf.Abs(value - traitValue);
                    default:
                        break;
                }
                break;
            case InfoType.opinion:
                if (recipient == Role.none) 
                {
                    Debug.LogError("Opinion condition has undefined recipient. Aborting distance evaluation");
                    //add proper exeption handling
                }
                throw new NotImplementedException();
                
                break;
            default:
                break;
        }
        throw new NotImplementedException();
    }
   
}
public enum ConditionType
{
    trait,
    relationship,
    opinion,
    goal
}
[Serializable]
public abstract class NewCondition
{
    [SerializeField] ConditionType _type;
    //there will pretty much always be a holder, but it might not always be the case so it will be reimplemented in each inherited class.
    internal abstract bool isMet(Dictionary<Role,Character> involvedCharacters, WorldModel worldModel);
    internal abstract bool isCurrentlyMet(Dictionary<Role,Character> involvedCharacters);//check if the condition is "really" met
    internal abstract float getDistance(WorldModel worldModel, Dictionary<Role,Character> involvedCharacters);//negative results indicate the condition is fullfilled;
    internal abstract float getCurrentDistance(Dictionary<Role,Character> involvedCharacters);
}

[Serializable]
public class TraitCondition : NewCondition
{
    [SerializeField]Trait _trait;
    [SerializeField]Role _holder;
    [SerializeField]float _value;
    [SerializeField]ValueComparisonOperator _operator;
    [SerializeField] float tolerance;//for the equal operator only, maybe use several layers of inheritance?

    internal override float getCurrentDistance(Dictionary<Role,Character> involvedCharacters)
    {
        Character holderRef = involvedCharacters[_holder];
        if (!involvedCharacters.TryGetValue(_holder, out holderRef))
        {
            Debug.Log("evaluated a condition with unassigned role");
            return 5000.0f;//role wasn't assigned to a character, probably a sign that something is amiss
        }
        float traitValue;
        if (!holderRef.m_traits.TryGetValue(_trait, out traitValue)) return 5000.0f;//character does not have the specified trait, this is a potential expected behavior(we could populate unspecified trait values with default ones
        switch (_operator)
        {
            case ValueComparisonOperator.lessThan:
                return traitValue - _value;
            case ValueComparisonOperator.MoreThan:
                return _value - traitValue;
            case ValueComparisonOperator.Equals:
                return Mathf.Abs(_value - traitValue);
            default:
                Debug.LogError("Invalid comparaison operator used in distance estimation to Trait condition. Trait: " + _operator.ToString());
                throw new NotImplementedException(); //using this exception because we don't need much extra info.
        }
    }

    internal override float getDistance(WorldModel worldModel, Dictionary<Role,Character> involvedCharacters)
    {
        CharModel holderModel;
        float traitValue;
        if (!worldModel.Characters.TryGetValue(involvedCharacters[_holder], out holderModel)) traitValue = 0.5f;
        else if (!holderModel.traits.TryGetValue(_trait, out traitValue)) traitValue = 0.5f;
        switch (_operator)
        {
            case ValueComparisonOperator.lessThan:
                return traitValue - _value;
            case ValueComparisonOperator.MoreThan:
                return _value - traitValue;
            case ValueComparisonOperator.Equals:
                return Mathf.Abs(_value - traitValue);
            default:
                Debug.LogError("Invalid comparaison operator used in distance estimation to Trait condition. Trait: " + _operator.ToString());
                throw new NotImplementedException(); //using this exception because we don't need much extra info.
        }
    }

    internal override bool isCurrentlyMet(Dictionary<Role,Character> involvedCharacters)
    {
        throw new NotImplementedException();
    }

    internal override bool isMet(Dictionary<Role,Character> involvedCharacters, WorldModel worldModel)
    {
        CharModel holderModel;
        float traitValue;
        if (!worldModel.Characters.TryGetValue(involvedCharacters[_holder], out holderModel)) traitValue = 0.5f;
        else if (!holderModel.traits.TryGetValue(_trait, out traitValue)) traitValue = 0.5f;
        switch (_operator)
        {
            case ValueComparisonOperator.lessThan:
                return traitValue < _value;
            case ValueComparisonOperator.MoreThan:
                return _value < traitValue;
            case ValueComparisonOperator.Equals:
                return Mathf.Abs(_value - traitValue) <  tolerance;
            default:
                Debug.LogError("Invalid comparaison operator used in distance estimation to Trait condition. Trait: " + _operator.ToString());
                throw new NotImplementedException(); //using this exception because we don't need much extra info.
        }
    }
}

[Serializable]
public class ConditionContainer : ISerializationCallbackReceiver
{
    [NonSerialized]public List<NewCondition> conditions = new List<NewCondition>();
    [SerializeField] List<string> conditionData = new List<string>();
    [SerializeField] List<string> conditionTypes = new List<string>();

    public void OnAfterDeserialize()
    {
        conditions.Clear();//just to be sure there is no junk left in the list.
        for(int i = 0; i < conditionData.Count; i++)
        {
            conditions.Add((NewCondition)JsonUtility.FromJson(conditionData[i], Type.GetType(conditionTypes[i])));
        }
    }

    public void OnBeforeSerialize()
    {
        //could add some more error checking here to avoid loosing data if something goes wrong
        conditionData = new List<string>();
        conditionTypes = new List<string>();
        foreach(var condition in conditions)
        {
            conditionTypes.Add(condition.GetType().ToString());
            conditionData.Add(JsonUtility.ToJson(condition, false));
        }
    }
}


