using UnityEngine;

namespace StateMachine
{
    public abstract class StateAction : ScriptableObject
    {
        public abstract bool Execute(BaseStateMachine machine);
    }
}