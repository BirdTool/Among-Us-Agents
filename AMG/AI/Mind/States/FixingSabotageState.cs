using AMG.AI.Tools;
using AMG.Enums.AgentEnums;
using AMG.Utilities;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private readonly CooldownTimer sabotageTimer = new();

        private void UpdateFixingSabotage()
        {
            if (!Utils.IsAnySabotageActive || currentSabotageStep == null)
            {
                currentSabotageStep = null;
                sabotageTimer.Stop();
                SetState(AgentState.Calculating);
                return;
            }

            if (!sabotageTimer.IsStarted)
            {
                if (currentSabotageStep.IsCompleted && currentSabotageStep is AMG.AI.Mind.Decisions.Sabotages.GenericSabotageStepBothSides)
                    return;

                sabotageTimer.StartDelay(currentSabotageStep.TimeToFix + GetReactionTime());
                return;
            }

            if (sabotageTimer.Consume())
            {
                currentSabotageStep.CompleteStep(this);
                
                if (currentSabotageStep is AMG.AI.Mind.Decisions.Sabotages.GenericSabotageStepBothSides && Utils.IsAnySabotageActive)
                    return; 

                currentSabotageStep = null;
                isGoingToFixASabotage = false;
                
                SetState(AgentState.Calculating);
            }
        }
    }
}