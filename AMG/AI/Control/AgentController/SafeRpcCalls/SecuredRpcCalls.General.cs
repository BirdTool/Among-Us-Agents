using System.Linq;
using AMG.Enums.SafeRpcEnums;
using AMG.Utilities;
using AmongUs.GameOptions;
using UnityEngine;

namespace AMG.AI.Control.AgentController
{
    public partial class AgentController
    {
        private static RoundDeadBody GetDeadBodyByPlayerId(byte playerId) => Utils.Round.CurrentRoundDeadBodies.FirstOrDefault(b => b.PlayerId == playerId);

        public ReportDeadBodyRpcEnums SafeReportBodyNotExecute(byte playerId)
        {
            var playerInfoToReport = GameData.Instance.GetPlayerById(playerId);
            if (playerInfoToReport == null) return ReportDeadBodyRpcEnums.ERROR_BodyDoesNotExist;

            var body = GetDeadBodyByPlayerId(playerId);
            if (body == null) return ReportDeadBodyRpcEnums.ERROR_BodyDoesNotExist;

            float dist = Vector2.Distance(Vector2Position, body.Position);
            if (dist > 3.4f) return ReportDeadBodyRpcEnums.ERROR_BodyTooFar;

            if (IsDead) return ReportDeadBodyRpcEnums.ERROR_AgentIsDead;

            return ReportDeadBodyRpcEnums.SUCCESS;
        }

        public ReportDeadBodyRpcEnums SafeReportBodyNotExecute(RoundDeadBody body)
        {
            var playerInfoToReport = GameData.Instance.GetPlayerById(body.PlayerId);
            if (playerInfoToReport == null) return ReportDeadBodyRpcEnums.ERROR_BodyDoesNotExist;

            float dist = Vector2.Distance(Vector2Position, body.Position);
            if (dist > 3.4f) return ReportDeadBodyRpcEnums.ERROR_BodyTooFar;

            if (IsDead) return ReportDeadBodyRpcEnums.ERROR_AgentIsDead;

            return ReportDeadBodyRpcEnums.SUCCESS;
        }

        public ReportDeadBodyRpcEnums SafeReportBody(byte playerInfoToReport)
        {
            var result = SafeReportBodyNotExecute(playerInfoToReport);
            if (result != ReportDeadBodyRpcEnums.SUCCESS) return result;

            Agent.CmdReportDeadBody(GameData.Instance.GetPlayerById(playerInfoToReport));

            return result;
        }

        public ReportDeadBodyRpcEnums SafeReportBody(RoundDeadBody body)
        {
            var result = SafeReportBodyNotExecute(body);
            if (result != ReportDeadBodyRpcEnums.SUCCESS) return result;

            Agent.CmdReportDeadBody(GameData.Instance.GetPlayerById(body.PlayerId));

            return result;
        }

        public UseVentRpcEnums SafeUseVentNotExecute(Vent vent)
        {
            if (IsDead) return UseVentRpcEnums.ERROR_AgentIsDead;
            
            var isEngineer = Agent.Data.Role.Role == RoleTypes.Engineer;
            
            if (!IsImpostor && !isEngineer) return UseVentRpcEnums.ERROR_AgentIsNotImpostorOrEngineer;
            
            if (vent == null) return UseVentRpcEnums.ERROR_VentDoesNotExist;
            if (Vector2.Distance(Vector2Position, vent.transform.position) > 3f) return UseVentRpcEnums.ERROR_AgentIsTooFarFromVent;

            if (isEngineer)
            {
                var engineerRole = Agent.Data.Role.Cast<EngineerRole>();

                if (engineerRole.cooldownSecondsRemaining > 0f)
                {
                    return UseVentRpcEnums.ERROR_AgentIsInCooldown; 
                }
            }

            return UseVentRpcEnums.SUCCESS;
        }

        public UseVentRpcEnums SafeUseVent(Vent vent)
        {
            var result = SafeUseVentNotExecute(vent);
            
            if (result != UseVentRpcEnums.SUCCESS) 
            {
                return result;
            }
            
            Agent.MyPhysics.RpcEnterVent(vent.Id);

            return result;
        }
    }
}