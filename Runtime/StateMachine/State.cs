using System.Collections.Generic;
using UnityEngine;

namespace StateMachine
{
    [CreateAssetMenu(menuName = "State Manager/State")]
    public sealed class State : BaseState
    {
        public List<StateAction> Action = new List<StateAction>();
        public List<Transition> Transitions = new List<Transition>();

        public override void Execute(BaseStateMachine machine)
        {
            foreach (var action in Action)
                action.Execute(machine);

            foreach (var transition in Transitions)
                transition.Execute(machine);
        }
    }
}