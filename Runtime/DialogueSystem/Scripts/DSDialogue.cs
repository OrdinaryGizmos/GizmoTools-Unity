using UnityEngine;

namespace OrdinaryGizmos.GizmoTools.DS
{
    using ScriptableObjects;

    public class DSDialogue : MonoBehaviour
    {
        /* Dialogue Scriptable Objects */
        [SerializeField] private DSDialogueContainerSO dialogueContainer;
        [SerializeField] private DSDialogueGroupSO dialogueGroup;
        [SerializeField] private DSDialogueSO dialogue;

        /* Filters */
        [SerializeField] private bool groupedDialogues;
        [SerializeField] private bool startingDialoguesOnly;

        /* Indexes */
        [SerializeField] private int selectedDialogueGroupIndex;
        [SerializeField] private int selectedDialogueIndex;

        public DSDialogueContainerSO DialogueContainer { get { return dialogueContainer; } set { dialogueContainer = value; } }

        public DSDialogueSO NextDialogue(Data.DSDialogueChoiceData choice = null)
        {
            if (choice != null)
            {
                if (dialogue.DialogueType == Enumerations.DSDialogueType.MultipleChoice)
                {
                    foreach (Data.DSDialogueChoiceData choiceData in dialogue.Choices)
                    {
                        if (choiceData == choice)
                        {
                            dialogue = choiceData.NextDialogue;
                            break;
                        }
                    }
                }
                else
                {
                    dialogue = choice.NextDialogue;

                }
            } else if (dialogue.Choices[0].NextDialogue != null)
            {
                dialogue = dialogue.Choices[0].NextDialogue;
            }

            return dialogue;
        }

        public bool HasNextDialogue(Data.DSDialogueChoiceData choice = null)
        {
            if (choice == null)
                return dialogue.Choices[0].NextDialogue != null || dialogue.DialogueType == Enumerations.DSDialogueType.MultipleChoice;
            else
                return choice.NextDialogue != null;
        }

        public DSDialogueSO CurrentDialogue { get { return dialogue; } }
    }
}