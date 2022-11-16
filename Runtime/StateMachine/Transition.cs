using System;
using UnityEngine;
using UnityEngine.Events;

namespace StateMachine
{
    [CreateAssetMenu(menuName = "State Manager/Transition")]
    public sealed class Transition : ScriptableObject
    {
        public Decision Decision;
        public BaseState TrueState;
        public BaseState FalseState;

        public void Execute(BaseStateMachine stateMachine)
        {
            if (Decision.Decide(stateMachine) && !(TrueState is DoNothingState))
                stateMachine.CurrentState = TrueState;
            else if (!Decision.Decide(stateMachine) && !(FalseState is DoNothingState))
                stateMachine.CurrentState = FalseState;
        }
    }
}