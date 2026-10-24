using System.Collections.Generic;
using System.Linq;
using System.Text;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Navigation;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Services.ChatService.Llm
{
    public sealed partial class LlmMeetingSession
    {
        private string GetPlayerInfo(byte? target, out bool failed)
        {
            failed = false;

            if (target == null)
            {
                failed = true;
                return "error: get_player_info needs a target (player id).";
            }

            var player = Utils.Players.GetPlayerByPlayerId(target.Value);
            if (player == null)
            {
                failed = true;
                return "error: player not found.";
            }

            if (_brain is not StructuredAgentBrain brain)
            {
                failed = true;
                return "Error: brain not available. (Something went wrong, do not try again)";
            }

            var p = brain.GetOrCreateMemory(player.PlayerId);

            var sb = new StringBuilder();
            sb.AppendLine($"=== Player Info: {Colour(player)} (ID: {player.PlayerId}) ===");
            sb.AppendLine($"Colour: {player.Data?.ColorName ?? "Unknown"}");
            sb.AppendLine($"Alive: {!(player.Data?.IsDead ?? false)}");

            // trust: -100..+100, 0=neutral
            sb.AppendLine($"Trust: {p.Trust:+0.#;-0.#;0} (-100=full distrust, 0=neutral, +100=full trust)");

            // suspicion: 0..100
            sb.AppendLine($"Suspicion: {p.SuspiciusPercentage:0.#}% (0=looks innocent, 100=certain impostor)");

            // role
            if (_brain.Agent.Data?.Role != null && _brain.Agent.Data.Role.IsImpostor && (player.Data?.Role?.IsImpostor ?? false))
            {
                sb.AppendLine("Role: Impostor (Fellow Impostor — confirmed)");
            }
            else if (p.ProbablyRole.Count > 0)
            {
                var roleStrs = p.ProbablyRole.Select(kv => $"{kv.Key}: {kv.Value * 100f:0.#}%");
                sb.AppendLine($"Role Probabilities: {string.Join(", ", roleStrs)}");
            }

            // position
            if (p.LastKnownPosition != Vector2.zero && p.LastSeenTime > 0)
            {
                float timeAgo = Utils.SecondsSinceShipStart.HasValue
                    ? Utils.SecondsSinceShipStart.Value - p.LastSeenTime
                    : 0f;
                sb.AppendLine($"LastSeenTime: {p.LastSeenTime:0.#}s ({timeAgo:0.#}s ago)");
                sb.AppendLine($"LastKnownPosition: ({p.LastKnownPosition.x:0.#}, {p.LastKnownPosition.y:0.#})");
                sb.AppendLine($"LastKnownRoom: {AgentVision.GetRoomAtPosition(p.LastKnownPosition)}");
                sb.AppendLine($"PositionUncertainty: {p.PositionUncertainty:0.#}");
            }
            else
            {
                sb.AppendLine("LastKnownPosition: Never seen / forgotten");
            }

            // witnessed crimes
            sb.AppendLine($"SawKilling: {p.SawKilling}");
            if (p.SawKilling)
            {
                string victimName = p.LastVictimSeenKilled.HasValue
                    ? ColourOf(p.LastVictimSeenKilled.Value)
                    : "Unknown";
                sb.AppendLine($"  - Victim: {victimName}");
                sb.AppendLine($"  - Room: {p.KillRoom?.ToString() ?? "Unknown"}");
                sb.AppendLine($"  - Time: {p.TimeOfKillWitnessed:0.#}s");
            }

            sb.AppendLine($"SawVenting: {p.SawVenting}");
            if (p.SawVenting)
                sb.AppendLine($"  - VentRoom: {p.VentRoom?.ToString() ?? "Unknown"}");

            // tasks
            sb.AppendLine($"SeenVerifiedTask: {p.SeenVerifiedTask}" +
                (p.SeenVerifiedTask && p.LastVerifiedTaskTime >= 0 ? $" (at {p.LastVerifiedTaskTime:0.#}s)" : ""));
            sb.AppendLine($"SeenFakeTask: {p.SeenFakeTask}");

            if (p.ClaimedTaskLocation.HasValue)
                sb.AppendLine($"ClaimedTaskLocation: {p.ClaimedTaskLocation} (at {p.ClaimedTaskTime:0.#}s)");

            if (p.SuspectedOfLongTaskDwell)
                sb.AppendLine($"SuspectedOfLongTaskDwell: True ({p.LongDwellTaskType} for {p.LongDwellDuration:0.#}s)");

            // social / voting
            sb.AppendLine($"SelfReportedBody: {p.SelfReportedBody}");

            if (p.LastVoteTarget.HasValue)
            {
                string voteTarget = p.LastVoteTarget.Value == Utils.SkipVotePlayerId
                    ? "Skip"
                    : ColourOf(p.LastVoteTarget.Value);
                sb.AppendLine($"LastVoteTarget: {voteTarget}");
            }

            sb.AppendLine($"VotedAgainstConsensusLastMeeting: {p.VotedAgainstConsensusLastMeeting}");

            if (p.SeenTogetherWith.Count > 0)
            {
                IEnumerable<string> sightings = p.SeenTogetherWith.Select(s =>
                {
                    string otherName = ColourOf(s.playerId);
                    return $"{otherName} (at {s.time:0.#}s)";
                });
                sb.AppendLine($"SeenTogetherWith: {string.Join(", ", sightings)}");
            }

            // player-specific persistent notes
            var playerNotes = _persistentNotes.FindAll(n => n.TargetPlayerId == player.PlayerId);
            if (playerNotes.Count > 0)
            {
                sb.AppendLine("Persistent Notes on this player:");
                foreach (var n in playerNotes)
                    sb.AppendLine($"  - [#{n.Id}] {n.Text}");
            }

            return sb.ToString();
        }
    }
}
