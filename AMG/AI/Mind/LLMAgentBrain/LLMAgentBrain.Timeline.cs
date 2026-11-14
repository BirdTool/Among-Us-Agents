using System;
using System.Collections.Generic;
using System.Linq;
using AMG.Models.LLMAgentBrainModels;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Mind.LLMAgentBrain
{
    public partial class LLMAgentBrain
    {
        private readonly List<(int round, LlmTimeline timeline)> _timeline = [];

        public IReadOnlyList<(int round, LlmTimeline timeline)> Timeline => _timeline;

        public string AllTranscript
            => string.Join("\n", _timeline.Select(t => $"Round {t.round}: {t.timeline.Transcript}"));

        public string GetCurrentRoundTranscript()
        {
            int currentRound = Utils.Round.CurrentRoundNumber;
            return string.Join("\n",
                _timeline
                    .Where(t => t.round == currentRound)
                    .Select(t => t.timeline.Transcript));
        }

        public List<LlmTimeline> GetCurrentTimelineRound()
        {
            int currentRound = Utils.Round.CurrentRoundNumber;
            return [.. _timeline
                .Where(t => t.round == currentRound)
                .Select(t => t.timeline)];
        }

        public void ClearTimeline() => _timeline.Clear();

        public void AddSawKiling(AMGPlayerData killer, AMGPlayerData victim)
        {
            if (killer == null) throw new ArgumentNullException(nameof(killer));
            if (victim == null) throw new ArgumentNullException(nameof(victim));

            string killerName = killer.PlayerName;
            string victimName = victim.PlayerName;
            string killerColour = killer.PlayerColour;
            string victimColour = victim.PlayerColour;
            RegisterTimelineEvent(
                $"Saw {killerName} ({killerColour}) killing {victimName} ({victimColour})",
                victim.Player.GetTruePosition());
        }

        public void AddSawPlayerVenting(bool isLeaving, AMGPlayerData player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));

            string playerName = player.PlayerName;
            string playerColour = player.PlayerColour;
            RegisterTimelineEvent(
                $"Saw {playerName} ({playerColour}) {(isLeaving ? "leaving" : "entering")} the vent",
                player.Player.GetTruePosition());
        }

        private void RegisterTimelineEvent(string txt, Vector2 position)
            => _timeline.Add((Utils.Round.CurrentRoundNumber, new LlmTimeline(txt, position, Vector2Position)));
    }
}