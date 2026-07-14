using AMG.Enums.AgentEnums;
using AMG.Utilities;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private void UpdateFixingSabotage()
        {
            if (!Utils.IsAnySabotageActive || currentSabotageStep == null)
            {
                currentSabotageStep = null;
                SetState(AgentState.Calculating);
                return;
            }

            if (sabotageTimer.IsOver())
            {
                currentSabotageStep.CompleteStep(this);
                Utils.CurrentSabotage.CompleteSabotage(ShipStatus.Instance);
                sabotageTimer.StartDelay(0.5f);
            }
        }
    }
}