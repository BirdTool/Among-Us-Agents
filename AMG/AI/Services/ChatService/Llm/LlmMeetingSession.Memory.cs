using System;
using System.Collections.Generic;
using System.Text;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Navigation;
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
