using AMG.AI.Mind.Decisions;
using AMG.AI.Tools;
using AMG.Interfaces;
using AMG.Utilities;
using System.Collections.Generic;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private readonly CooldownTimer _calculatingTimer = new();
        private int _calculatingTries = 0;

        private void UpdateCalculating()
        {
            ResetPath();

            if (_calculatingTries >= 3)
            {
                LogManager.LogDebug("[AI Brain] Limite de falhas alcançado! Mudando para SmartWandering.");
                SetState(Enums.AgentEnums.AgentState.SmartWandering);
                _calculatingTries = 0;
                return;
            }

            if (_calculatingTimer.Consume())
            {
                var decisions = DecisionsGroup.AllMainDecisions;

                List<(IMainDecision decision, float points)> validDecisions = [];

                foreach (var decision in decisions)
                {
                    float points = decision.CalculateUtility(this);
                    if (points > 0)
                    {
                        validDecisions.Add((decision, points));
                    }
                }

                validDecisions.Sort((a, b) => b.points.CompareTo(a.points));

                bool decisionExecuted = false;

                foreach (var item in validDecisions)
                {
                    LogManager.LogDebug($"[AI Brain] Tentando decisão: {item.decision.GetType().Name} ({item.points} pts)");
                    SetState(Enums.AgentEnums.AgentState.Stopped);

                    bool success = item.decision.Execute(this);

                    if (success)
                    {
                        LogManager.LogDebug($"[AI Brain] Sucesso na decisão: {item.decision.GetType().Name}");
                        decisionExecuted = true;
                        _calculatingTries = 0;
                        break;
                    }
                    else
                    {
                        LogManager.LogDebug($"[AI Brain] Falhou ao executar {item.decision.GetType().Name}. Passando para a próxima opção");
                    }
                }

                if (!decisionExecuted)
                {
                    LogManager.LogDebug("[AI Brain] Nenhuma decisão pôde ser executada! Incrementando falha e aguardando...");
                    SetState(Enums.AgentEnums.AgentState.Calculating);
                    _calculatingTries++;
                    _calculatingTimer.StartDelay(0.4f);
                }
            }
        }
    }
}