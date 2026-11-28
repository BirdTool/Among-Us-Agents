using System;
using System.Collections.Generic;
using System.Linq;
using AMG.Enums.SafeRpcEnums;
using AMG.Models.LLM;
using AMG.Utilities;
using AmongUs.GameOptions;
using UnityEngine;

namespace AMG.AI.Services.ChatService.Llm
{
    public sealed partial class LlmChatService
    {
        public const float EarlyVoteFraction = 0.6f;
        public const float EarlyVoteUnlimitedSeconds = 30f;     // when the game has no voting time limit
        public const float LastCallSeconds = 20f;
        public const float SecondaryVoteWindowSeconds = 10f;
        private const float SecondaryUnlimitedSeconds = 60f;    // when the game has no voting time limit

        private float _votingOpenedAt = -1f;

        public float? VotingElapsed => _votingOpenedAt < 0f ? null : Time.time - _votingOpenedAt;

        private static float? VotingTimeTotal
        {
            get
            {
                if (GameOptionsManager.Instance == null) return null;

                int seconds = GameOptionsManager.Instance.CurrentGameOptions.GetInt(Int32OptionNames.VotingTime);
                return seconds > 0 ? seconds : null;
            }
        }

        public float? VotingRemaining
            => VotingElapsed is float elapsed && VotingTimeTotal is float total ? Mathf.Max(0f, total - elapsed) : null;

        public float EarlyVoteUntil => VotingTimeTotal is float total ? total * EarlyVoteFraction : EarlyVoteUnlimitedSeconds;

        public LlmDefaultAgentClass MainVoterAgent => _runs.Select(r => r.Agent).FirstOrDefault(a => a.CanVote && a.MainVoter);

        public IEnumerable<LlmDefaultAgentClass> OtherAgents(LlmDefaultAgentClass self)
            => _runs.Select(r => r.Agent).Where(a => a != self && a.HasCommands);

        private bool MainVoterActive(LlmDefaultAgentClass except)
            => _runs.Any(r => r.Agent != except && r.Agent.CanVote && r.Agent.MainVoter);

        public (bool success, string message) TryVote(LlmDefaultAgentClass agent, byte target, bool certain)
        {
            if (_votedFor != null)
                return (false, "error: you already voted. A vote cannot be changed.");

            var check = CheckVote(target);
            if (check != VoteRpcEnums.SUCCESS)
                return (false, check == VoteRpcEnums.ERROR_IsNotInVoteTime ? "error: voting is not open yet. Wait." : $"error: {check}");

            float elapsed = VotingElapsed ?? 0f;
            bool skip = target >= 250;

            if (!agent.MainVoter && MainVoterActive(agent))
            {
                bool window = VotingRemaining is float left ? left <= SecondaryVoteWindowSeconds : elapsed >= SecondaryUnlimitedSeconds;
                if (!window)
                    return (false, $"error: {MainVoterAgent?.AgentName} is the main voter and decides the vote. Do not vote yourself; you may only do it if it still has not voted in the last {SecondaryVoteWindowSeconds:F0}s of voting.");
            }
            else if (elapsed < EarlyVoteUntil && !(certain && !skip))
            {
                return (false, $"error: too early to vote ({elapsed:F0}s into voting, {EarlyVoteUntil:F0}s must pass). Keep reading the chat. "
                    + (skip ? "" : "Only if you are 100% sure who the impostor is (someone was seen killing or practically confessed), send the command again with \"certain\" as the second arg."));
            }

            var result = Vote(target);
            return result == VoteRpcEnums.SUCCESS ? (true, "ok: vote cast.") : (false, $"error: {result}");
        }
    }
}
