using AMG.AI.Navigation;
using AMG.AI.Tools;
using AMG.Enums.AgentEnums;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private void UpdateNavigating()
        {
            ReplaceNameTag(DefaultTags.States.Navigating);

            bool hasReachedDestination = ProcessPathMovement();

            if (hasReachedDestination)
            {
                ResetPath();
                SetState(AgentState.Wandering);
            }
        }
    }
}
