using System.Linq;
using AMG.AI.Tools;
using AMG.Enums.AgentEnums;
using AMG.Utilities;
using Steamworks;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
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
                    if (Utils.IsCloseToAnyLocation(currentSabotageStep.Locations, WaypointPosition, 1.5f))
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
                    if (Utils.IsCloseToAnyLocation([.. currentLocalTask.Locations], Vector2Position, 1.5f))
                    {
                        SetState(AgentState.DoingTask);
                    }
                    else
                    {
                        SetState(AgentState.Calculating);
                    }
                }
                else if (currentVentToEnter != null)
                {
                    if (Utils.IsCloseToLocation(Vector2Position, currentVentToEnter.transform.position, 3f))
                    {
                        var result = SafeUseVent(currentVentToEnter);
                        LogManager.LogDebug($"[VENT] Resultado de usar a vent: {result}");
                        if (result == Enums.SafeRpcEnums.UseVentRpcEnums.SUCCESS)
                        {
                            currentVent = currentVentToEnter;
                            currentVentToEnter = null;
                            SetState(AgentState.InVent);
                        }
                        else
                        {
                            currentVentToEnter = null;
                            SetState(AgentState.Calculating);
                        }
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
