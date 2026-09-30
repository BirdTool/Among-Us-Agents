using AMG.Enums.AgentEnums;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        private float _observingSince = -1f;
        private float _observeDuration;
        private PlayerControl _observeTarget;
        private AgentState _stateBeforeObserving;

        public void RequestObserve(PlayerControl target, float duration)
        {
            if (currentState == AgentState.Observing)
            {
                float remaining = _observeDuration - (Time.time - _observingSince);
                if (remaining < duration)
                {
                    _observeDuration = duration;
                    _observeTarget = target;
                }
                return;
            }

            _stateBeforeObserving = currentState;
            _observeTarget = target;
            _observeDuration = duration;
            _observingSince = Time.time;
            SetState(AgentState.Observing);
        }

        private void UpdateObserving()
        {
            if (_observeTarget == null || _observeTarget.Data == null || _observeTarget.Data.IsDead)
            {
                FinishObserving();
                return;
            }

            if (Time.time - _observingSince > _observeDuration)
            {
                FinishObserving();
                return;
            }

            ResetPath();
        }

        private void FinishObserving()
        {
            _observeTarget = null;
            _observingSince = -1f;

            var resumeState = _stateBeforeObserving == AgentState.Observing
                ? AgentState.Calculating
                : _stateBeforeObserving;

            SetState(resumeState);
        }
    }
}