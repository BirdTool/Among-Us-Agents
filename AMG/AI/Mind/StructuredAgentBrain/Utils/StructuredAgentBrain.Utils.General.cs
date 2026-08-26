using AMG.AI.Navigation;
using AMG.Enums.AgentEnums;
using AMG.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        public float ReactionTime => Utils.GetDisturbTime(delayTime, delayDisturb);

        public void SetState(AgentState newState)
        {
            LogManager.LogDebug($"[Mudança de estado] {BaseName} : {currentState} -> {newState}");
            
            if (currentState != newState)
            {
                currentState = newState;
                _updateTags.TryGetValue(newState, out var tag);
                if (tag != null)
                {
                    ReplaceNameTag(tag);
                }
            }
        }
    }
}
