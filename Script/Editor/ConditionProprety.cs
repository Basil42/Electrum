using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using ICSharpCode.NRefactory.Ast;

//[CustomPropertyDrawer(typeof(NewCondition))]
//public class ConditionProprety : PropertyDrawer
//{
//    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//    {
//        var condition = (NewCondition)EditorHelper.GetTargetObjectOfProperty(property);
//        //type change is done in the container GUI, doesn't seem safe to construct a new condition here.
//        if (condition == null) property.managedReferenceValue = new TraitCondition();
//        EditorGUI.BeginProperty(position, label, property);

//        switch (condition._type)
//        {
//            case ConditionType.trait:
//                TraitConditionOnGUI(position, (TraitCondition)condition);
//                break;
//            case ConditionType.relationship:
//                RelationshipConditionOnGUI(position, (RelationshipCondition)condition);
//                break;
//            case ConditionType.opinion:
//                OpinionConditionOnGUI(position, (OpinionCondition)condition, property); 
//                break;
//            default:
//                Debug.LogError("invalid condition type " + condition._type.ToString() + " set as property");
//                break;
//        }
//        //type change (done at the end to minimize interference with the rest of the GUI
//        EditorGUI.BeginChangeCheck();
//        ConditionType selectedType = (ConditionType)EditorGUI.EnumPopup(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), condition._type);
//        if (EditorGUI.EndChangeCheck())
//        {
//            if (selectedType != condition._type)
//            {
//                property.managedReferenceValue = CreateCondition(selectedType);
//            }
//        }
//        EditorGUI.EndProperty();
//    }

//    private NewCondition CreateCondition(ConditionType selectedType)
//    {
//        switch (selectedType)
//        {
//            case ConditionType.trait:
//                return new TraitCondition();
//            case ConditionType.relationship:
//                return new RelationshipCondition();
//            case ConditionType.opinion:
//                return new TraitOpinionCondition();//using this type as the default one.
//            default:
//                Debug.LogError("attempted to create a condition of type " + selectedType + " which is not implemented in the property drawer");
//                throw new NotImplementedException();
//        }
//    }

//    private void OpinionConditionOnGUI(Rect position, OpinionCondition condition, SerializedProperty property)
//    {
//        switch (condition.opinionType)
//        {
//            case OpinionType.trait:
//                OpinionTraitConditionOnGUI(position, (TraitOpinionCondition)condition);
//                break;
//            case OpinionType.relationship:
//                OpinionRelationshipOnGUI(position, (RelationshipOpinionCondition)condition);
//                break;
//            default:
//                Debug.LogError("invalid opinion condition type : " + condition.opinionType.ToString());
//                throw new NotImplementedException();
//        }
//        EditorGUI.BeginChangeCheck();
//        OpinionType opinionTypeSelected = (OpinionType)EditorGUI.EnumPopup(new Rect(position.x, position.y + (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 4.0f/*unsure*/, position.width, EditorGUIUtility.singleLineHeight),condition.opinionType);
//        if (EditorGUI.EndChangeCheck() && opinionTypeSelected != condition.opinionType)
//        {
//            switch (opinionTypeSelected)
//            {
//                case OpinionType.trait:
//                    property.managedReferenceValue = new TraitOpinionCondition();
//                    break;
//                case OpinionType.relationship:
//                    property.managedReferenceValue = new RelationshipOpinionCondition();
//                    break;
//                default:
//                    break;
//            }
//        }
//    }

//    private void OpinionRelationshipOnGUI(Rect position, RelationshipOpinionCondition condition)
//    {
//        throw new NotImplementedException();
//    }

//    private void OpinionTraitConditionOnGUI(Rect position, TraitOpinionCondition condition)
//    {
//        throw new NotImplementedException();
//    }

//    private void RelationshipConditionOnGUI(Rect position, RelationshipCondition condition)
//    {
//        throw new NotImplementedException();
//    }

//    private void TraitConditionOnGUI(Rect position, TraitCondition condition)
//    {
//        

//        }
//    }

//    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//    {
//        var condition = (NewCondition)EditorHelper.GetTargetObjectOfProperty(property);
//        switch (condition._type)
//        {
//            case ConditionType.trait:
//                float height = EditorGUIUtility.singleLineHeight * 5.0f;
//                if (((TraitCondition)condition)._operator == ValueComparisonOperator.Equals) height += EditorGUIUtility.singleLineHeight;
//                return height;
//            case ConditionType.relationship:
//                return EditorGUIUtility.singleLineHeight * 5.0f;
//            case ConditionType.opinion:
//                switch (((OpinionCondition)condition).opinionType)
//                {
//                    case OpinionType.trait:
//                        return EditorGUIUtility.singleLineHeight * 8.0f;
//                    case OpinionType.relationship:
//                        return EditorGUIUtility.singleLineHeight * 7.0f;
//                    default:
//                        Debug.LogError("Invalid or unimplemented opinion type " + ((OpinionCondition)condition).opinionType.ToString() + " set as property");
//                        return 0.0f;
//                }
//            default:
//                Debug.LogError("invalid or unimplemented condition type " + condition._type.ToString() + " set as property.");
//                return 0.0f;
//        }
//    }
//}
[CustomPropertyDrawer(typeof(ConditionContainer))]
public class ConditionContDrawer : PropertyDrawer
{
    float Vstep = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
    float height = EditorGUIUtility.singleLineHeight;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        //add a frame for this
        EditorGUI.BeginProperty(position,new GUIContent("Conditions"),property);
        ConditionContainer cont = (ConditionContainer)EditorHelper.GetTargetObjectOfProperty(property);
        var list = cont.conditions;
        float offset =0.0f;
        for (int i = 0; i < list.Count; i++)
        {
            NewCondition condition = list[i];
            EditorGUI.BeginChangeCheck();
            ConditionType requestedType = (ConditionType)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, EditorGUIUtility.singleLineHeight), condition._type);
            if (EditorGUI.EndChangeCheck() && requestedType != condition._type)
            {
                list[i] = CreateCondition(requestedType);
            }
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
        }
        EditorGUI.EndProperty();
        //add buttons and stuff
        
    }

    private void OpinionCondDrawer(List<NewCondition> list, int i, ref float offset, Rect position)
    {
        OpinionCondition condition = (OpinionCondition)list[i];
        EditorGUI.BeginChangeCheck();
        condition.opinionType =(OpinionType)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), condition.opinionType);
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
        condition.relationship = (RelationshipType)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), condition.relationship);
        offset += Vstep;
        condition.holder = (Role)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), condition.holder);
        offset += Vstep;
        condition.RelationShipRecipient = (Role)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), condition.RelationShipRecipient);
        offset += Vstep;
        condition.RelationshipStatus = EditorGUI.Toggle(new Rect(position.x, position.y + offset, position.width, height), condition.RelationshipStatus);
        offset += Vstep;
    }

    private void TraitOpinnionCondDraw(List<NewCondition> list, int i, ref float offset, Rect position)
    {
        TraitOpinionCondition condition = (TraitOpinionCondition)list[i];
        condition.holder = (Role)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), condition.holder);
        offset += Vstep;
        condition.trait = (Trait)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), condition.trait);
        offset += Vstep;
        EditorGUI.BeginChangeCheck();
        condition.OpinionOperator = (ValueComparisonOperator)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), condition.OpinionOperator);
        offset += Vstep;
        if (EditorGUI.EndChangeCheck())
        {
            if(condition.OpinionOperator == ValueComparisonOperator.Equals)
            {
                condition.tolerance = EditorGUI.FloatField(new Rect(position.x, position.y + offset, position.width, height), condition.tolerance);
                offset += Vstep;
            }
        }
    }

    private void RelationshipCondDraw(List<NewCondition> list, int i, ref float offset, Rect position)
    {
        RelationshipCondition condition = (RelationshipCondition)list[i];

        condition.relationship = (RelationshipType)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), condition.relationship);
        offset += Vstep;
        condition.holder = (Role)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), condition.holder);
        offset += Vstep; 
        condition.recipient = (Role)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), condition.recipient);
        offset += Vstep;
        condition.relationshipStatus = EditorGUI.Toggle(new Rect(position.x, position.y + offset, position.width, height), condition.relationshipStatus);
        offset += Vstep;
    }

    private void TraitCondDraw(List<NewCondition> list, int i, ref float offset, Rect position)
    {
        TraitCondition condition = (TraitCondition)list[i];
        
        condition._trait = (Trait)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width,height),condition._trait);
        offset += Vstep;
        condition._holder = (Role)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), condition._holder);
        offset += Vstep;
        condition._value = EditorGUI.FloatField(new Rect(position.x, position.y + offset, position.width, height), condition._value);
        offset += Vstep;
        condition._operator = (ValueComparisonOperator)EditorGUI.EnumPopup(new Rect(position.x, position.y + offset, position.width, height), condition._operator);
        offset += Vstep;
        if(condition._operator == ValueComparisonOperator.Equals)
        {
            condition.tolerance = EditorGUI.FloatField(new Rect(position.x, position.y + offset, position.width, height), condition.tolerance);
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
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return base.GetPropertyHeight(property, label);
    }
}
