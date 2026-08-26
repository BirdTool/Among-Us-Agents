using AMG.AI.Mind.StructuredAgentBrain.Decisions;
using AMG.AI.Tools;
using AMG.Interfaces;
using AMG.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        private readonly CooldownTimer _calculatingTimer = new();
        private int _calculatingTries = 0;

        // Door-lockout wandering
        private bool _isWaitingForDoor = false;
        private float _doorWaitTimeout = 0f;

        private void OnDoorOpenedWhileWaiting()
        {
            if (!_isWaitingForDoor) return;
            _isWaitingForDoor = false;
            Utils.OnDoorStateChanged -= OnDoorOpenedWhileWaiting;
            SetState(Enums.AgentEnums.AgentState.Calculating);
            // LogManager.LogDebug($"[AI Brain] {baseName} retomando cálculo — porta aberta!");
        }

        private void UpdateCalculating()
        {
            ResetPath();

            // If we were waiting for a door, check timeout
            if (_isWaitingForDoor)
            {
                if (Time.time >= _doorWaitTimeout)
                {
                    _isWaitingForDoor = false;
                    Utils.OnDoorStateChanged -= OnDoorOpenedWhileWaiting;
                    LogManager.LogDebug($"[AI Brain] {BaseName} timeout de espera de porta expirou. Re-calculando.");
                }
                else
                {
                    return; // Still waiting — let the Wandering state run (registered in _updateActions)
                }
            }

            if (_calculatingTries >= 3)
            {
                // LogManager.LogDebug("[AI Brain] Limite de falhas alcançado! Mudando para SmartWandering.");
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
                    // LogManager.LogDebug($"[AI Brain] Tentando decisão: {item.decision.GetType().Name} ({item.points} pts)");
                    SetState(Enums.AgentEnums.AgentState.Stopped);

                    bool success = item.decision.Execute(this);

                    if (success)
                    {
                        // LogManager.LogDebug($"[AI Brain] Sucesso na decisão: {item.decision.GetType().Name}");
                        decisionExecuted = true;
                        _calculatingTries = 0;
                        break;
                    }
                    else
                    {
                        // LogManager.LogDebug($"[AI Brain] Falhou ao executar {item.decision.GetType().Name}. Passando para a próxima opção");
                    }
                }

                if (!decisionExecuted)
                {
                    _calculatingTries++;

                    // Check if the agent is trapped behind a locked door.
                    // If so, wander locally instead of retrying expensive A* every 0.4 s.
                    bool isRoomLocked = Utils.IsRoomClosed(Utils.GetPlayerRoom(Agent));
                    if (isRoomLocked)
                    {
                        // LogManager.LogDebug($"[AI Brain] {baseName} está preso em sala fechada. Aguardando porta abrindo...");
                        _isWaitingForDoor = true;
                        _doorWaitTimeout = Time.time + 2f; // Safety timeout: try again after 2s anyway
                        Utils.OnDoorStateChanged += OnDoorOpenedWhileWaiting;
                        SetState(Enums.AgentEnums.AgentState.Wandering); // Cheap random walk inside the room
                    }
                    else
                    {
                        // LogManager.LogDebug("[AI Brain] Nenhuma decisão pôde ser executada! Incrementando falha e aguardando...");
                        SetState(Enums.AgentEnums.AgentState.Calculating);
                        _calculatingTimer.StartDelay(0.4f);
                    }
                }
            }
        }
    }
}
