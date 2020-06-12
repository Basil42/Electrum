using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "new Electrum settings",menuName = "Electrum/Settings")]
public class ElectrumSettings : ScriptableObject
{
    [Tooltip("affinity value below which a character will automatically discard an action. Lower value allow more extreme/desperate actions.")]
    [Range(0.0f,1.0f)]
    public float affinityTreshold = 0.5f;

    [Tooltip("Number of bindings a character can consider before a warning is displayed in the console")]
    public int MaxBindingsCandidateWarning = 100;
    [Tooltip("Number of differently binded instances of an action a character can consider before being forced to abort.")]
    public int MaxBindingsCandidatesAbort = 500;
}
