using AMG.AI.Mind.Decisions;
using AMG.AI.Tools;
using AMG.Interfaces;
using AMG.Utilities;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private readonly CooldownTimer _calculatingTimer = new();
        private int _calculatingTries = 0;

        private void UpdateCalculating()
        {
            ResetPath();
            if (_calculatingTries > 3)
            {
                LogManager.LogDebug("Calculating Tries is bigger than 3");
                SetState(Enums.AgentEnums.AgentState.SmartWandering);
                _calculatingTries = 0;
                return;
            }

            if (_calculatingTimer.Consume())
            {
                var decisions = DecisionsGroup.AllMainDecisions;
                float bestDecisionPoints = 0;
                IMainDecision bestDecision = null;

                foreach (var decision in decisions)
                {
                    float points = decision.CalculateUtility(this);
                    if (points > bestDecisionPoints)
                    {
                        bestDecisionPoints = points;
                        bestDecision = decision;
                    }
                }

                if (bestDecisionPoints > 0 && bestDecision != null)
                {
                    LogManager.LogDebug("Decision match");
                    SetState(Enums.AgentEnums.AgentState.Stopped);

                    bool success = bestDecision.Execute(this);

                    if (!success)
                    {
                        LogManager.LogDebug("Execution failed! Fallback to Calculating.");
                        SetState(Enums.AgentEnums.AgentState.Calculating);
                        _calculatingTries++;
                        _calculatingTimer.StartDelay(GetReactionTime());
                    }
                }
                else
                {
                    LogManager.LogDebug("Decision doesn't match");
                    _calculatingTries++;
                    _calculatingTimer.StartDelay(GetReactionTime());
                }
            }
        }
    }
}
