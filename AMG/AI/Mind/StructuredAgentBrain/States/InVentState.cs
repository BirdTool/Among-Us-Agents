using System;
using System.Linq;
using AMG.Enums.AgentEnums;
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
        private const float TIME_TO_CHECK_ITS_OUTSIDE_VENT = 0.5f; // seconds

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
                LogManager.LogDebug($"[VENT] Agent {Agent.Data.PlayerName} is outside vent. Set state to Calculating.");
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
                    // try to exit vent
                    SafeLeaveVent(currentVent);
                    currentVent = null;
                    SetState(AgentState.Calculating);
                    return;
                }

                LogManager.LogDebug($"[VENT] Indo da vent {currentVent.Id} para a vent {randomVent.Id}");
                Agent.MyPhysics.RpcExitVent(currentVent.Id);
                Agent.MyPhysics.RpcEnterVent(randomVent.Id);
                LogManager.LogDebug($"[VENT] Sucesso!");

                currentVent = randomVent;
                _lastTimeChangedPosition = Time.time;
                _nextVentChangeTime = 0.0f;
            }
        }

        private void ImpostorVentLogic()
        {
            // not implemented yet
        }

        private bool CheckIfItsOutsideVent()
        {
            if (currentVent == null) return true;
            return !Utils.IsCloseToLocation(Vector2Position, currentVent.transform.position, 1.2f);
        }
    }
}
