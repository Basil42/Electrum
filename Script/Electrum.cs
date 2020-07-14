using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Electrum : MonoBehaviour
{
    public ElectrumSettings settings;
    [SerializeField]private ActionSet Actions = null;
    
    public List<Character> cast;
    public List<NarrativeGoal> beats;

    //static members
    public static Electrum singleton;
    public static ActionSet actionSet;
    public static float affinityTreshold;
    public static Action DoNothingAction;

    public static int MaxBindingCandidatesWarning { get; internal set; }
    public static int MaxBindingCandidatesAbort { get; internal set; }

    private void Awake()
    {
        if (settings == null) Debug.LogError("please assign a setting object to Electrum before initializing it.");
        if (Actions == null || Actions.actions.Count == 0) Debug.LogError("No Action set assigned, or action set has no actions");
        if(actionSet == null) actionSet = Actions;
        if (singleton == null)
        {
            singleton = this;
            affinityTreshold = settings.affinityTreshold;
            MaxBindingCandidatesWarning = settings.MaxBindingsCandidateWarning;
            MaxBindingCandidatesAbort = settings.MaxBindingsCandidatesAbort;
        }
        else
        {
            Debug.LogError("Two instances of Electrum currently instancied");
            Destroy(this);
        }
        foreach (var character in cast) character.ConstructownModel();
    }
    
}



public enum InfoType//may later add the goal and opinion types
{
    relationship,
    trait,
    opinion
}
public enum OpinionType
{
    trait,
    relationship,
    goal
}




