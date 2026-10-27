using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Services.ChatService.Llm
{
    // What the agent remembers between meetings of the same match: its own notes, plus the things it
    // personally saw, which are written down by code so they never depend on the model remembering to do it.
    public sealed partial class LlmMeetingSession
    {
        private const int MaxStoredNotes = 40;
        private const int MaxNotesInPrompt = 14;
        private const int MaxNoteLength = 160;

        private readonly HashSet<string> _autoNoteKeys = [];

        private static readonly Regex RoleClaim = new(
            @"\b(?:i['’]?m|im|i am)\s+(?:an?\s+|the\s+)?(?<role>eng(?:ineer)?|sci(?:entist)?|tracker|noisemaker|guardian angel|detective)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static string CanonicalRole(string role) => role.ToLowerInvariant() switch
        {
            "eng" or "engineer" => "Engineer",
            "sci" or "scientist" => "Scientist",
            "tracker" => "Tracker",
            "noisemaker" => "Noisemaker",
            "guardian angel" => "Guardian Angel",
            "detective" => "Detective",
            _ => role
        };

        private string SenderLabel(LlmTranscriptEntry e)
        {
            string sender = ColourOf(e.SenderId);
            if (sender.StartsWith("player ")) sender = Clean(e.SenderName);   // left the game: fall back to the name
            return e.SenderId == _brain.AgentId ? $"{sender} (you)" : sender;
        }

        // Role claims other players make in the chat ("im eng"): written down so they are not forgotten next meeting.
        private void RecordClaims()
        {
            int meeting = LlmMeetingTranscript.MeetingNumber;
            foreach (var e in LlmMeetingTranscript.All)
            {
                if (e.SenderId == _brain.AgentId) continue;

                var match = RoleClaim.Match(e.Text ?? string.Empty);
                if (!match.Success) continue;

                string role = CanonicalRole(match.Groups["role"].Value);
                AddAutoNote($"claim:{e.SenderId}:{role}", e.SenderId,
                    $"[claim] Meeting #{meeting}: {ColourOf(e.SenderId)} said they are {role} (not verified).");
            }
        }

        // The chat of the meetings that already ended: High remembers all of them, Medium only the last one.
        private void AppendHistory(StringBuilder sb, TierProfile profile)
        {
            if (profile.Lobby || profile.TrustChat) return;

            var past = LlmMeetingTranscript.History;
            if (past.Count == 0) return;

            bool high = Active == LlmTier.High;
            int meetings = high ? past.Count : 1;
            int maxLines = high ? 60 : 25;

            var lines = new List<string>();
            for (int i = past.Count - meetings; i < past.Count; i++)
                foreach (var e in past[i].Entries)
                    lines.Add($"#{past[i].Meeting} {SenderLabel(e)}: {Clean(Trunc(e.Text, 100))}");

            if (lines.Count > maxLines) lines = lines.GetRange(lines.Count - maxLines, maxLines);

            sb.AppendLine("Chat of the earlier meetings of this match, oldest first. Use it to remember who accused whom, who claimed a role and what you already said; do not repeat an accusation you already made unless you have something new:");
            foreach (var line in lines) sb.AppendLine(line);
            sb.AppendLine();
        }

        // High only: everything the agent has observed about every player, without having to ask for each one.
        private void AppendAllObservations(StringBuilder sb, TierProfile profile)
        {
            if (Active != LlmTier.High || _brain is not StructuredAgentBrain brain) return;

            var lines = new List<string>();
            foreach (var pl in GameData.Instance.AllPlayers)
            {
                if (pl.PlayerId == brain.AgentId) continue;

                var p = brain.GetOrCreateMemory(pl.PlayerId);
                var facts = new List<string>();

                if (p.SawKilling)
                {
                    string victim = p.LastVictimSeenKilled.HasValue ? ColourOf(p.LastVictimSeenKilled.Value) : "someone";
                    facts.Add($"you saw them kill {victim} in {p.KillRoom?.ToString() ?? "an unknown room"}");
                }
                if (p.SawVenting) facts.Add($"you saw them use a vent in {p.VentRoom?.ToString() ?? "an unknown room"}");
                if (p.SeenFakeTask) facts.Add("you saw them fake a task");
                if (p.SeenVerifiedTask) facts.Add("you saw them do a real task");
                if (p.SelfReportedBody) facts.Add("they reported a body themselves");
                if (p.LastVoteTarget.HasValue)
                    facts.Add("last vote: " + (p.LastVoteTarget.Value == Utils.SkipVotePlayerId ? "skip" : ColourOf(p.LastVoteTarget.Value)));

                lines.Add($"- {Colour(pl)} (ID {pl.PlayerId}, {(AMGPlayers.GetPlayer(pl.PlayerId)?.PublicStatus ?? (pl.Disconnected ? "left the game" : pl.IsDead ? "dead" : "alive"))}): suspicion {p.SuspiciusPercentage:0}%, trust {p.Trust:+0;-0;0}; "
                    + (facts.Count > 0 ? string.Join("; ", facts) : "nothing notable seen"));
            }

            if (lines.Count == 0) return;

            sb.AppendLine("Everything you have observed about each player so far (the same facts as get_player_info; only things you really saw):");
            foreach (var line in lines) sb.AppendLine(line);
            sb.AppendLine();
        }

        // A new match starts from scratch.
        public void ClearMemory()
        {
            _persistentNotes.Clear();
            _autoNoteKeys.Clear();
            _nextPersistentNoteId = 1;
        }

        private void TrimNotes()
        {
            while (_persistentNotes.Count > MaxStoredNotes)
            {
                int i = _persistentNotes.FindIndex(n => !n.Auto);   // the model's own notes go first
                _persistentNotes.RemoveAt(i >= 0 ? i : 0);
            }
        }

        private void AddAutoNote(string key, byte? target, string text)
        {
            if (!_autoNoteKeys.Add(key)) return;

            _persistentNotes.Add(new PersistentNote
            {
                Id = _nextPersistentNoteId++,
                TargetPlayerId = target,
                Text = text,
                CreatedAt = Time.time,
                Auto = true
            });
            TrimNotes();
        }

        // First-hand facts: bodies, kills, vents and fake tasks the agent witnessed. Safe to call every step: it never duplicates.
        private void RecordWitnessedFacts()
        {
            if (_brain is not StructuredAgentBrain brain) return;
            int meeting = LlmMeetingTranscript.MeetingNumber;

            foreach (var body in brain.bodiesSeenDead)
            {
                var room = AgentVision.GetRoomAtPosition(body.Position);
                AddAutoNote($"body:{body.PlayerId}", body.PlayerId,
                    $"[seen] Meeting #{meeting}: you saw the body of {ColourOf(body.PlayerId)} in {room}.");
            }

            foreach (var m in brain.GetMemories())
            {
                if (m.PlayerId == brain.AgentId) continue;
                string who = ColourOf(m.PlayerId);

                if (m.SawKilling)
                {
                    string victim = m.LastVictimSeenKilled.HasValue ? ColourOf(m.LastVictimSeenKilled.Value) : "someone";
                    AddAutoNote($"kill:{m.PlayerId}:{m.LastVictimSeenKilled}", m.PlayerId,
                        $"[seen] You saw {who} kill {victim} in {m.KillRoom?.ToString() ?? "an unknown room"}.");
                }
                if (m.SawVenting)
                    AddAutoNote($"vent:{m.PlayerId}:{m.VentRoom}", m.PlayerId,
                        $"[seen] You saw {who} use a vent in {m.VentRoom?.ToString() ?? "an unknown room"}.");
                if (m.SeenFakeTask)
                    AddAutoNote($"fake:{m.PlayerId}", m.PlayerId, $"[seen] You saw {who} fake a task.");
            }
        }

        private void AppendNotes(StringBuilder sb, TierProfile profile)
        {
            if (profile.Lobby) return;

            if (_persistentNotes.Count > 0)
            {
                sb.AppendLine("Your notes from earlier in this match (they are kept between meetings):");
                for (int i = Math.Max(0, _persistentNotes.Count - MaxNotesInPrompt); i < _persistentNotes.Count; i++)
                {
                    var n = _persistentNotes[i];
                    string about = n.TargetPlayerId.HasValue ? $" ({ColourOf(n.TargetPlayerId.Value)})" : string.Empty;
                    sb.AppendLine($"- [#{n.Id}]{about} {Clean(Trunc(n.Text, MaxNoteLength))}");
                }
                sb.AppendLine("Notes marked [seen] are things you personally saw: do not retract them unless you find clear evidence they were wrong.");
                sb.AppendLine();
            }

            if (!profile.TrustChat && System.Linq.Enumerable.Contains(profile.Commands, "add_persistent_note"))
            {
                sb.AppendLine("Use add_persistent_note for things worth remembering in later meetings: an alibi someone gave, who claimed what, a contradiction you caught, who you decided to trust. Keep each note short, use colours, and do not repeat a note you already have.");
                sb.AppendLine();
            }
        }
    }
}
