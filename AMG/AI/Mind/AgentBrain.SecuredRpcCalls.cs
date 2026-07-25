using System.Linq;
using AMG.Enums.SafeRpcEnums;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
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

            AgentControl.CmdReportDeadBody(GameData.Instance.GetPlayerById(playerInfoToReport));

            return result;
        }

        public ReportDeadBodyRpcEnums SafeReportBody(RoundDeadBody body)
        {
            var result = SafeReportBodyNotExecute(body);
            if (result != ReportDeadBodyRpcEnums.SUCCESS) return result;

            AgentControl.CmdReportDeadBody(GameData.Instance.GetPlayerById(body.PlayerId));

            return result;
        }
        
        public SafeKillRpcEnums SafeKillNotExecute(byte targetId)
        {
            if (IsDead) return SafeKillRpcEnums.ERROR_AgentIsDead;
            if (IsCrewmate) return SafeKillRpcEnums.ERROR_AgentIsNotImpostor;
            
            if (myAgent.killTimer > 0f) return SafeKillRpcEnums.ERROR_CooldownNotReady;

            var target = Utils.Players.GetPlayerByPlayerId(targetId);
            if (target == null) return SafeKillRpcEnums.ERROR_TargetDoesNotExist;
            if (targetId == myAgent.PlayerId) return SafeKillRpcEnums.ERROR_TargetIsItSelf;
            if (target.Data.IsDead) return SafeKillRpcEnums.ERROR_TargetIsDead;
            if (target.Data.Role.IsImpostor) return SafeKillRpcEnums.ERROR_TargetIsImpostor;

            float[] nativeKillDistances = [1.0f, 1.8f, 2.5f];
            int killDistIndex = 1; 
            
            if (GameOptionsManager.Instance != null && GameOptionsManager.Instance.currentNormalGameOptions != null)
            {
                killDistIndex = GameOptionsManager.Instance.currentNormalGameOptions.KillDistance;
            }
            
            killDistIndex = Mathf.Clamp(killDistIndex, 0, 2); 
            float currentLobbyMaxDistance = nativeKillDistances[killDistIndex];

            float dist = Vector2.Distance(Vector2Position, target.transform.position);
            if (dist > currentLobbyMaxDistance) return SafeKillRpcEnums.ERROR_TargetTooFar;

            bool isProtected = target.protectedByGuardianId != -1 && target.protectedByGuardianId != 255;
            
            if (isProtected) return SafeKillRpcEnums.FAILED_AngelProtected;

            return SafeKillRpcEnums.SUCCESS;
        }

        public SafeKillRpcEnums SafeKill(byte targetId)
        {
            var result = SafeKillNotExecute(targetId);
            
            if (result != SafeKillRpcEnums.SUCCESS && result != SafeKillRpcEnums.FAILED_AngelProtected) 
            {
                return result;
            }

            bool didKillSucceed = result == SafeKillRpcEnums.SUCCESS;
            
            AgentControl.RpcMurderPlayer(Utils.Players.GetPlayerByPlayerId(targetId), didKillSucceed);

            return result;
        }
    }
}