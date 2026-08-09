using System.Linq;
using AMG.AI.Tools;
using AMG.Enums.AgentEnums;
using AMG.Utilities;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private void UpdateNavigating()
        {
            ReplaceNameTag(DefaultTags.States.Navigating);

            bool? hasReachedDestination = ProcessPathMovement();

            if (hasReachedDestination == true)
            {
                ResetPath();

                if (currentSabotageStep != null)
                {
                    if (Utils.IsCloseToAnyLocation(currentSabotageStep.Locations, WaypointPosition, 1f))
                    {
                        SetState(AgentState.FixingSabotage);
                    }
                    else
                    {
                        SetState(AgentState.Calculating);
                    }
                }
                else if (currentLocalTask != null)
                {
                    if (Utils.IsCloseToAnyLocation([.. currentLocalTask.Locations], Vector2Position, 1f))
                    {
                        SetState(AgentState.DoingTask);
                    }
                    else
                    {
                        SetState(AgentState.Calculating);
                    }
                }
                else
                {
                    SetState(AgentState.Calculating);
                }
            }
            else if (hasReachedDestination == null)
            {
                ResetPath();
                SetState(AgentState.Calculating);
            }
        }
    }
}
