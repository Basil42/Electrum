using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using ICSharpCode.NRefactory.Ast;
using System.Security.Principal;


[CustomPropertyDrawer(typeof(ConditionContainer))]
public class ConditionContDrawer : PropertyDrawer
{
    float Vstep = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
    float height = EditorGUIUtility.singleLineHeight;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        //add a frame for this
        ConditionContainer cont = (ConditionContainer)EditorHelper.GetTargetObjectOfProperty(property);
        var list = cont.conditions;
        float offset = 0.0f;
        
       
        var WrapperLabel = EditorGUI.BeginProperty(position, label, property);
        EditorGUI.LabelField(new Rect(position.x, position.y + offset, position.width, EditorGUIUtility.singleLineHeight), label);
       
        offset += Vstep;
        
        for (int i = 0; i < list.Count; i++)
        {
            //could do a foldout here
            EditorGUI.indentLevel++;
            EditorGUI.PrefixLabel(GetNextRectangle(position, ref offset), new GUIContent("Condition"));
            EditorGUI.indentLevel++;
            NewCondition condition = list[i];
            EditorGUI.BeginChangeCheck();
            ConditionType requestedType = (ConditionType)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, EditorGUIUtility.singleLineHeight),new GUIContent("Type") ,condition._type);
            offset += Vstep;
            if (EditorGUI.EndChangeCheck() && requestedType != condition._type)
            {
                list[i] = CreateCondition(requestedType);
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }
            EditorGUI.BeginChangeCheck();
            switch (list[i]._type)
            {
                case ConditionType.trait:
                    TraitCondDraw(list, i, ref offset, position);
                    break;
                case ConditionType.relationship:
                    RelationshipCondDraw(list, i, ref offset, position);
                    break;
                case ConditionType.opinion:
                    OpinionCondDrawer(list, i, ref offset, position);
                    break;
                default:
                    break;
            }
            //remove button
            if (GUI.Button(new Rect(position.x, position.y + offset, position.width, EditorGUIUtility.singleLineHeight), new GUIContent("remove")))
            {
                list.RemoveAt(i);
                i--;
            }
            if(EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(property.serializedObject.targetObject); ;
            offset += Vstep;
            EditorGUI.indentLevel -= 2;
        }
        if(GUI.Button(new Rect(position.x,position.y + offset,position.width,EditorGUIUtility.singleLineHeight),new GUIContent("Add")))
        {
            list.Add(new TraitCondition());
            EditorUtility.SetDirty(property.serializedObject.targetObject);
        }
        EditorGUI.EndProperty();
        
    }

    private void OpinionCondDrawer(List<NewCondition> list, int i, ref float offset, Rect position)
    {
        OpinionCondition condition = (OpinionCondition)list[i];
        EditorGUI.BeginChangeCheck();
        condition.opinionType =(OpinionType)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height),new GUIContent("Opinion Type") ,condition.opinionType);
        offset += Vstep;
        if (EditorGUI.EndChangeCheck())
        {
            switch (condition.opinionType)
            {
                case OpinionType.trait:
                    list[i] = new TraitOpinionCondition();
                    break;
                case OpinionType.relationship:
                    list[i] = new RelationshipOpinionCondition();
                    break;
                default:
                    break;
            }
        }
        condition.holder = (Role)EditorGUI.EnumPopup(GetNextRectangle(position, ref offset), new GUIContent("Holder") , condition.holder);
        switch (((OpinionCondition)list[i]).opinionType)
        {
            case OpinionType.trait:
                TraitOpinnionCondDraw(list, i, ref offset, position);
                break;
            case OpinionType.relationship:
                RelationshipOpinionCondDraw(list, i, ref offset, position);
                break;
            default:
                break;
        }
    }

    private void RelationshipOpinionCondDraw(List<NewCondition> list, int i, ref float offset, Rect position)
    {
        RelationshipOpinionCondition condition = (RelationshipOpinionCondition)list[i];
        condition.relationship = (RelationshipType)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height),new GUIContent("relationship Type"), condition.relationship);
        offset += Vstep;
        condition.RelationshipHolder = (Role)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height),new GUIContent("relationship holder") ,condition.RelationshipHolder);
        offset += Vstep;
        condition.RelationShipRecipient = (Role)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height),new GUIContent("relationship recipient"), condition.RelationShipRecipient);
        offset += Vstep;
        condition.RelationshipStatus = EditorGUI.Toggle(new Rect(position.x, position.y + offset, position.width, height),new GUIContent("status") ,condition.RelationshipStatus);
        offset += Vstep;
    }

    private void TraitOpinnionCondDraw(List<NewCondition> list, int i, ref float offset, Rect position)
    {
        TraitOpinionCondition condition = (TraitOpinionCondition)list[i];
        condition.TraitHolder = (Role)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height),new GUIContent("Trait holder"), condition.TraitHolder);
        offset += Vstep;
        condition.trait = (Trait)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), new GUIContent("Trait"), condition.trait);
        offset += Vstep;
        EditorGUI.BeginChangeCheck();
        condition.OpinionOperator = (ValueComparisonOperator)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), new GUIContent("Opinion Operator"), condition.OpinionOperator);
        offset += Vstep;
        if (EditorGUI.EndChangeCheck())
        {
            if(condition.OpinionOperator == ValueComparisonOperator.Equals)
            {
                condition.tolerance = EditorGUI.FloatField(new Rect(position.x, position.y + offset, position.width, height), new GUIContent("Tolerance"), condition.tolerance);
                offset += Vstep;
            }
        }
    }

    private void RelationshipCondDraw(List<NewCondition> list, int i, ref float offset, Rect position)
    {
        RelationshipCondition condition = (RelationshipCondition)list[i];

        condition.relationship = (RelationshipType)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), new GUIContent("Relationship"), condition.relationship);
        offset += Vstep;
        condition.holder = (Role)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), new GUIContent("Holder"), condition.holder);
        offset += Vstep; 
        condition.recipient = (Role)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), new GUIContent("Recipient"), condition.recipient);
        offset += Vstep;
        condition.relationshipStatus = EditorGUI.Toggle(new Rect(position.x, position.y + offset, position.width, height), new GUIContent("Status"), condition.relationshipStatus);
        offset += Vstep;
    }

    private void TraitCondDraw(List<NewCondition> list, int i, ref float offset, Rect position)
    {
        TraitCondition condition = (TraitCondition)list[i];
        
        condition._trait = (Trait)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width,height), new GUIContent("Trait"), condition._trait);
        offset += Vstep;
        condition._holder = (Role)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), new GUIContent("Holder"), condition._holder);
        offset += Vstep;
        condition._value = EditorGUI.FloatField(new Rect(position.x, position.y + offset, position.width, height), new GUIContent("Value"), condition._value);
        offset += Vstep;
        condition._operator = (ValueComparisonOperator)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), new GUIContent("Operator"), condition._operator);
        offset += Vstep;
        if(condition._operator == ValueComparisonOperator.Equals)
        {
            condition.tolerance = EditorGUI.FloatField(new Rect(position.x, position.y + offset, position.width, height), new GUIContent("Tolerance"), condition.tolerance);
            offset += Vstep;
        }
    }
    private NewCondition CreateCondition(ConditionType selectedType)
    {
        switch (selectedType)
        {
            case ConditionType.trait:
                return new TraitCondition();
            case ConditionType.relationship:
                return new RelationshipCondition();
            case ConditionType.opinion:
                return new TraitOpinionCondition();//using this type as the default one.
            default:
                Debug.LogError("attempted to create a condition of type " + selectedType + " which is not implemented in the property drawer");
                throw new NotImplementedException();
        }
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)//next issue to solve
    {
        ConditionContainer cont = (ConditionContainer)EditorHelper.GetTargetObjectOfProperty(property);
        var height = Vstep;// label
        foreach (var condition in cont.conditions)
        {
            height += Vstep;//individual labels
            switch (condition._type)
            {
                case ConditionType.trait:
                    height += Vstep * 4.0f;//trait,holder,value,operator
                    if (((TraitCondition)condition)._operator == ValueComparisonOperator.Equals) height += Vstep;//tolerance
                    
                    break;
                case ConditionType.relationship:
                    height += Vstep * 4.0f;//holder,recipient,relationshiptype,status
                    break;
                case ConditionType.opinion:
                    height += Vstep * 2.0f;//holder, opiniontype
                    switch (((OpinionCondition)condition).opinionType)
                    {
                        case OpinionType.trait:
                            height += Vstep * 4.0f;//trait, traitHolder,value,operator
                            if (((TraitOpinionCondition)condition).OpinionOperator == ValueComparisonOperator.Equals) height += Vstep;//tolerance
                            break;
                        case OpinionType.relationship:
                            height += Vstep * 4.0f;//relationship,relationshipHolder, relationship recipient,status
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
            height += Vstep;//remove button
            height += EditorGUIUtility.singleLineHeight;//cumulative offset from stacked objects ?
        } 
        
        
        
        height += Vstep; //add button
        return height;
    }

    private Rect GetNextRectangle(Rect position, ref float offset)
    {
        var result = new Rect(position.x, position.y + offset , position.width, EditorGUIUtility.singleLineHeight);
        offset += Vstep;
        return result;
    }
}
