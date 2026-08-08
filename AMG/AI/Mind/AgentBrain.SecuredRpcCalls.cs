using System;
using System.Linq;
using AMG.Enums.SafeRpcEnums;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private static RoundDeadBody GetDeadBodyByPlayerId(byte playerId) => Utils.Round.CurrentRoundDeadBodies.FirstOrDefault(b => b.PlayerId == playerId);
        private float _lastMessage = 0;
        private const byte SkipVotePlayerId = 253;

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

        public VoteRpcEnums SafeVoteNotExecute(byte playerId)
        {
            if (IsDead) return VoteRpcEnums.ERROR_AgentIsDead;
            if (!Utils.IsMeeting) return VoteRpcEnums.ERROR_IsNotInMeeting;
            if (!Utils.IsMeetingVoting) return VoteRpcEnums.ERROR_IsNotInVoteTime;
            
            if (playerId != unchecked((byte)-1) && playerId <= 250)
            {
                var target = Utils.Players.GetPlayerByPlayerId(playerId);
                if (target == null) return VoteRpcEnums.ERROR_TargetDoesNotExist;
                if (target.Data.IsDead) return VoteRpcEnums.ERROR_TargetIsDead;
            }
            if (MeetingHud.Instance.DidVote(myAgent.PlayerId)) return VoteRpcEnums.ERROR_AlreadyVoted;

            return VoteRpcEnums.SUCCESS;
        }

        public VoteRpcEnums SafeVote(byte playerId)
        {
            bool isSkipVote = playerId == unchecked((byte)-1)
                || playerId >= 250;
            if (isSkipVote) playerId = SkipVotePlayerId;
            var result = SafeVoteNotExecute(playerId);
            
            if (result != VoteRpcEnums.SUCCESS) 
            {
                return result;
            }
            
            IsAuthorizedToVote = true;
            MeetingHud.Instance.CmdCastVote(myAgent.PlayerId, playerId);
            try {
                MeetingHud.Instance.CastVote(myAgent.PlayerId, playerId);
            } catch (Exception ex) {
                LogManager.LogError($"[Agente {AgentControl.PlayerId}] Exceção ao votar (playerId={playerId}): {ex}");
                IsAuthorizedToVote = false;
                return VoteRpcEnums.FAILED_UnknownError;
            }
            IsAuthorizedToVote = false;

            return result;
        }

        public ChatRpcEnums SafeSendChatNotExecute(string content)
        {
            if (!Utils.IsMeeting || IsDead) return ChatRpcEnums.ERROR_IsNotInMeeting;
            if (content.Length > 100) return ChatRpcEnums.ERROR_ContentIsBiggerThan100;
            if (string.IsNullOrEmpty(content)) return ChatRpcEnums.ERROR_ContentIsEmpty;
            if (Time.time - _lastMessage < 3f) return ChatRpcEnums.ERROR_InCooldown;

            return ChatRpcEnums.SUCCESS;
        }

        public ChatRpcEnums SafeSendChat(string content)
        {
            var result = SafeSendChatNotExecute(content);
            
            if (result != ChatRpcEnums.SUCCESS) 
            {
                return result;
            }
            
            AgentControl.RpcSendChat(content);
            _lastMessage = Time.time;

            return result;
        }
    }
}