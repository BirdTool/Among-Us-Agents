using System;
using System.Linq;
using AMG.AI.Tools;
using AMG.Enums.SafeRpcEnums;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Control.AgentController
{
    public partial class AgentController
    {
        protected float _lastMessage = 0;
        protected const byte SkipVotePlayerId = 253;

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
            if (MeetingHud.Instance.DidVote(AgentId)) return VoteRpcEnums.ERROR_AlreadyVoted;

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
            MeetingHud.Instance.CmdCastVote(AgentId, playerId);
            try {
                MeetingHud.Instance.CastVote(AgentId, playerId);
            } catch (Exception ex) {
                LogManager.LogError($"[Agente {AgentId}] Exceção ao votar (playerId={playerId}): {ex}");
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
            
            Agent.RpcSendChat(content);
            _lastMessage = Time.time;

            return result;
        }
    }
}