using System;
using System.Linq;
using AMG.Enums.AgentEnums;
using AMG.Enums.SafeRpcEnums;
using AMG.Utilities;
using Il2CppSystem.Runtime.CompilerServices;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        public Vent currentVentToEnter = null;
        private Vent currentVent = null;
        public Func<bool> InVentLogic = null;
        private float _lastTimeChangedPosition = 0.0f; // unity time.time
        private const float TIME_TO_CHANGE_POSITION = 5f; // seconds
        private float _nextVentChangeTime = 0.0f;
        private float _lastTimeCheckedItsOutsideVent = 0.0f;
        private const float TIME_TO_CHECK_ITS_OUTSIDE_VENT = 0.5f;
        private const float GRACE_PERIOD_AFTER_VENT_CHANGE = 1.0f;

        private void UpdateInVent()
        {
            try
            {
                UpdateInVentInternal();
            }
            catch (Exception ex)
            {
                LogManager.LogDebug($"[VENT] Exceção em UpdateInVent: {ex}");
                SetState(AgentState.Calculating);
            }
        }

        private void UpdateInVentInternal()
        {
            if (Agent.inVent == false)
            {
                SetState(AgentState.Calculating);
                return;
            }

            if (currentVent == null)
            {
                LogManager.LogDebug("[VENT] currentVent está null enquanto Agent.inVent é true. Forçando saída.");
                Agent.inVent = false;
                SetState(AgentState.Calculating);
                return;
            }

            if (_lastTimeCheckedItsOutsideVent == 0.0f) _lastTimeCheckedItsOutsideVent = Time.time;
            var secondsSinceLastCheck = Time.time - _lastTimeCheckedItsOutsideVent;
            var hasToCheckItsOutsideVent = secondsSinceLastCheck >= TIME_TO_CHECK_ITS_OUTSIDE_VENT;

            if (hasToCheckItsOutsideVent && CheckIfItsOutsideVent())
            {
                LogManager.LogDebug(
                    String.Concat(
                        "[VENT] Agent " + Agent.Data.PlayerName + " está fora da vent. Set state to Calculating.\n",
                        "\nPosição do jogador: " + Vector2Position,
                        "\nPosição da vent: " + currentVent.transform.position,
                        "\nDistância calculada: " + Vector2.Distance(Vector2Position, currentVent.transform.position),
                        "\nDistância que era pra estar: 3.0"
                    )
                );
                SafeLeaveVent(currentVent);
                currentVent = null;
                SetState(AgentState.Calculating);
                return;
            }

            if (InVentLogic != null)
            {
                if (InVentLogic() == true)
                {
                    InVentLogic = null;
                }
                return; // Don't put to calculate if it is executing a plan
            }

            if (IsImpostor)
                ImpostorVentLogic();
            else
                EngineerVentLogic();
        }

        private void EngineerVentLogic()
        {
            var engineerRole = Agent.Data.Role.Cast<EngineerRole>();

            if (engineerRole.inVentTimeRemaining <= 0.0f)
            {
                LogManager.LogDebug("[VENT] Tempo dentro da vent acabou");
                SafeLeaveVent(currentVent);
                currentVent = null;
                SetState(AgentState.Calculating);
                return;
            }

            if (_lastTimeChangedPosition == 0.0f) _lastTimeChangedPosition = Time.time;
            if (_nextVentChangeTime == 0.0f) _nextVentChangeTime = Utils.GetDisturbTime(TIME_TO_CHANGE_POSITION, 3.2f);

            var secondsSinceLastChange = Time.time - _lastTimeChangedPosition;
            var hasToChangePosition = secondsSinceLastChange >= _nextVentChangeTime;

            if (hasToChangePosition)
            {
                var nearbyVents = currentVent.NearbyVents;
                var randomVent = nearbyVents
                    .Where(vent => vent != null && vent.Id != currentVent.Id)
                    .ToList()
                    .GetRandomItemSecureOrDefault();

                if (randomVent == null || randomVent == currentVent)
                {
                    LogManager.LogDebug("[VENT] Não achou outra vent para ir. Saindo da vent.");
                    SafeLeaveVent(currentVent);
                    currentVent = null;
                    SetState(AgentState.Calculating);
                    return;
                }

                LogManager.LogDebug($"[VENT] Indo da vent {currentVent.Id} para a vent {randomVent.Id}");

                if (IsItTheRealPlayer)
                {
                    VentingResultEnum moveResult;
                    if (currentVent.Left == randomVent)
                    {
                        moveResult = SafeVentGoLeft(currentVent);
                        currentVent = randomVent;
                    }
                    else if (currentVent.Right == randomVent)
                    {
                        moveResult = SafeVentGoRight(currentVent);
                        currentVent = randomVent;
                    }
                    else if (currentVent.Center == randomVent)
                    {
                        moveResult = SafeVentGoCenter(currentVent);
                        currentVent = randomVent;
                    }
                    else
                    {
                        LogManager.LogDebug("[VENT] randomVent não é Left/Right/Center de currentVent — inconsistência.");
                        SafeLeaveVent(currentVent);
                        currentVent = null;
                        SetState(AgentState.Calculating);
                        return;
                    }

                    if (moveResult != VentingResultEnum.SUCCESS)
                    {
                        LogManager.LogDebug($"[VENT] Falha ao mover: {moveResult}");
                        return;
                    }
                }
                else
                {
                    Agent.transform.position = randomVent.transform.position;
                }

                currentVent = randomVent;
                _lastTimeChangedPosition = Time.time;
                _nextVentChangeTime = 0.0f;
                _lastTimeCheckedItsOutsideVent = Time.time + GRACE_PERIOD_AFTER_VENT_CHANGE;
            }
        }

        private void ImpostorVentLogic()
        {
            // not implemented yet
        }

        private bool CheckIfItsOutsideVent()
        {
            if (currentVent == null) return true;
            return !Utils.IsCloseToLocation(Vector2Position, currentVent.transform.position, 3f);
        }
    }
}
