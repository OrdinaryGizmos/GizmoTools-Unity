using System;
using UnityEngine;
using UnityEngine.Events;

namespace StateMachine
{
    public abstract class Decision : ScriptableObject
    {
        public UnityEvent ExternalDecision;
        public abstract bool Decide(BaseStateMachine machine);
    }
}