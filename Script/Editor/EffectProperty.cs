using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ICSharpCode.NRefactory.Visitors;
using System;

[CustomPropertyDrawer(typeof(EffectInstance))]
public class EffectProperty : PropertyDrawer 
{
    private float Vstep = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EffectInstance container = (EffectInstance)EditorHelper.GetTargetObjectOfProperty(property);
        float offset = 0.0f;
        if (container == null) return;
        //to do: make it a fold out
        EditorGUI.PrefixLabel(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("Effect Instance"));
        EditorGUI.indentLevel++;
        EditorGUI.BeginChangeCheck();
        container.type = (InfoType)EditorGUI.EnumPopup(EditorHelper.GetNextRectangle(position, ref offset, Vstep),new GUIContent("Type"), container.type);
        if (EditorGUI.EndChangeCheck())
        {
            switch (container.type)
            {
                case InfoType.relationship:
                    container.effect = new RelationshipEffect();
                    break;
                case InfoType.trait:
                    container.effect = new TraitEffect();
                    break;
                case InfoType.opinion:
                    container.effect = new TraitOpinionEffect();//used as default for opinions
                    break;
                default:
                    break;
            }
        }
        if(container.type == InfoType.opinion)
        {
            EditorGUI.BeginChangeCheck();
            OpinionEffect opinionEffect = (OpinionEffect)container.effect;
            opinionEffect.opinionType = (OpinionType)EditorGUI.EnumPopup(EditorHelper.GetNextRectangle(position, ref offset,  Vstep),new GUIContent("Opinion Type"), opinionEffect.opinionType);
            if (EditorGUI.EndChangeCheck())
            {
                switch (opinionEffect.opinionType)
                {
                    case OpinionType.trait:
                        container.effect = new TraitOpinionEffect();
                        break;
                    case OpinionType.relationship:
                        container.effect = new RelationshipOpinionEffect();
                        break;
                    default:
                        Debug.LogError("Invalid opinion type: " + opinionEffect.opinionType.ToString());
                        throw new NotImplementedException();
                }
            }
        }
        switch (container.effect.GetType().ToString())
        {
            case "TraitEffect":
                TraitEffectOnGUI((TraitEffect)container.effect, position, ref offset);
                break;
            case "RelationshipEffect":
                RelationshipEffectOnGUI((RelationshipEffect)container.effect, position, ref offset);
                break;
            case "TraitOpinionEffect":
                TraitOpinionEffectOnGUI((TraitOpinionEffect)container.effect, position, ref offset);
                break;
            case "RelationshipOpinionEffect":
                RelationshipOpinionEffectonGUI((RelationshipOpinionEffect)container.effect, position, ref offset);
                break;
            default:
                Debug.LogError("invalid or partially implemented action effect type: " + container.effect.GetType().ToString());
                throw new NotImplementedException();
        }
        EditorGUI.indentLevel--;
    }

    private void RelationshipOpinionEffectonGUI(RelationshipOpinionEffect effect, Rect position, ref float offset)
    {
        effect.OpinionHolder = (Role)EditorGUI.EnumPopup(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("Opinion Holder"), effect.OpinionHolder);
        effect.relationship = (RelationshipType)EditorGUI.EnumPopup(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("Relationship"), effect.relationship);
        effect.RelationshipHolder = (Role)EditorGUI.EnumPopup(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("Relationship Holder"), effect.RelationshipHolder);
        effect.RelationshipRecipient = (Role)EditorGUI.EnumPopup(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("Relationship Recipient"), effect.RelationshipRecipient);
        effect.RelationshipStatus = EditorGUI.Toggle(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("New status"), effect.RelationshipStatus);
    }

    private void TraitOpinionEffectOnGUI(TraitOpinionEffect effect, Rect position, ref float offset)
    {
        effect.OpinionHolder = (Role)EditorGUI.EnumPopup(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("Opinion Holder"), effect.OpinionHolder);
        effect.trait = (Trait)EditorGUI.EnumPopup(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("Trait"),effect.trait);
        effect.TraitHolder = (Role)EditorGUI.EnumPopup(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("Trait Holder"), effect.TraitHolder);
        effect.Operator = (ValueChangeOperator)EditorGUI.EnumPopup(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("Operator"), effect.Operator);
        effect.traitValue = EditorGUI.FloatField(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("Value"), effect.traitValue);
    }

    private void RelationshipEffectOnGUI(RelationshipEffect effect, Rect position, ref float offset)
    {
        effect.relationship = (RelationshipType)EditorGUI.EnumPopup(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("Relationship"), effect.relationship);
        effect.Holder = (Role)EditorGUI.EnumPopup(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("Holder"), effect.Holder);
        effect.Recipient = (Role)EditorGUI.EnumPopup(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("Recipient"), effect.Recipient);
        effect.status = EditorGUI.Toggle(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("new Status"), effect.status);
    }

    private void TraitEffectOnGUI(TraitEffect effect, Rect position, ref float offset)
    {
        effect.trait = (Trait)EditorGUI.EnumPopup(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("Trait"), effect.trait);
        effect.Holder = (Role)EditorGUI.EnumPopup(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("Holder"), effect.Holder);
        effect.Operator = (ValueChangeOperator)EditorGUI.EnumPopup(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("Operator"), effect.Operator);
        effect.value = EditorGUI.FloatField(EditorHelper.GetNextRectangle(position, ref offset, Vstep), new GUIContent("Value"), effect.value);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = Vstep*2.0f;
        EffectInstance Container = (EffectInstance)EditorHelper.GetTargetObjectOfProperty(property);
        if (Container == null) return Vstep;
        switch (Container.effect.GetType().ToString())
        {
            case "TraitEffect":
                height += Vstep * 4.0f;
                break;
            case "RelationshipEffect":
                height += Vstep * 4.0f;
                break;
            case "RelationshipOpinionEffect":
                height += Vstep * 6.0f;
                break;
            case "TraitOpinionEffect":
                height += Vstep * 6.0f;
                break;
            default:
                break;
        }
        if (Container.AllWitnesses) height += (Container.Witnesses.Count + 1.0f) * Vstep;
        return height;

    }
}
