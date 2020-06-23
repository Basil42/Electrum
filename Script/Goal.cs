using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

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
