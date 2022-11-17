using System;
using System.Collections.Generic;
using UnityEngine;
using OrdinaryGizmos.GizmoTools.DS.Data;

namespace OrdinaryGizmos.GizmoTools.DS.ScriptableObjects
{
    using Data;
    using Enumerations;

    public class DSDialogueSO : ScriptableObject
    {
        [field: SerializeField] public string DialogueName;
        [field: SerializeField][field: TextArea()] public string Text;
        [field: SerializeField] public List<DSDialogueChoiceData> Choices;
        [field: SerializeField] public DSDialogueType DialogueType;
        [field: SerializeField] public bool IsStartingDialogue;
        [field: SerializeField] public List<EventData> StartEvents;
        [field: SerializeField] public List<EventData> EndEvents;

        public void Initialize(string dialogueName, string text, List<DSDialogueChoiceData> choices, DSDialogueType dialogueType, bool isStartingDialogue)
        {
            DialogueName = dialogueName;
            Text = text;
            Choices = choices;
            DialogueType = dialogueType;
            IsStartingDialogue = isStartingDialogue;
        }
    }
}
