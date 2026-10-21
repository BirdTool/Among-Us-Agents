using System;
using System.Globalization;
using System.Text;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.Enums.SafeRpcEnums;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Services.ChatService.Llm
{
    public sealed partial class LlmMeetingSession
    {
        private static string NormalizeCommandName(string name) => name switch
        {
            "change_suspicion" => "change_suspicious",
            "set_suspicion"    => "set_suspicious",
            _ => name
        };

        private string Execute(LlmCommand cmd, out bool terminal, out bool failed)
        {
            terminal = false;
            failed   = false;

            string cmdName = NormalizeCommandName(cmd?.Name);

            if (cmd == null || string.IsNullOrEmpty(cmdName) || Array.IndexOf(Profiles[Tier].Commands, cmdName) < 0)
            {
                failed = true;
                return $"error: unknown command '{cmd?.Name}'.";
            }

            switch (cmdName)
            {
                case "send_message":
                {
                    if (string.IsNullOrWhiteSpace(cmd.Text)) { failed = true; return "error: text is empty."; }
                    var check = CheckSay(cmd.Text);
                    if (check != ChatRpcEnums.SUCCESS) { failed = true; return $"error: {check}"; }
                    _outbox.Enqueue(cmd.Text);
                    return "ok: queued, it will appear in the chat in a few seconds.";
                }

                case "vote":
                {
                    if (cmd.Target == null) { failed = true; return "error: vote needs a target (player id, or 253 to skip)."; }
                    var r = Vote(cmd.Target);
                    if (r != VoteRpcEnums.SUCCESS) { failed = true; return $"error: {r}"; }
                    terminal = true;
                    return "ok: vote cast.";
                }

                case "wait":
                    terminal = true;
                    return "ok: waiting for new messages.";

                case "get_role_info":
                    return GetRoleDescription(cmd.Text);

                case "get_player_info":
                    return GetPlayerInfo(cmd.Target, out failed);

                case "get_meeting_observations":
                    return GetMeetingObservations();

                case "add_notes":
                {
                    if (string.IsNullOrWhiteSpace(cmd.Text)) { failed = true; return "error: text is empty."; }
                    var note = new MeetingNote { Id = _nextMeetingNoteId++, Text = cmd.Text.Trim(), CreatedAt = Time.time };
                    _meetingNotes.Add(note);
                    return $"ok: note #{note.Id} added.";
                }

                case "remove_notes":
                {
                    if (!TryParseNoteId(cmd, out int id)) { failed = true; return "error: remove_notes needs an id (e.g. text: \"1\" or target: 1)."; }
                    var note = _meetingNotes.Find(n => n.Id == id);
                    if (note == null) { failed = true; return $"error: note #{id} not found."; }
                    _meetingNotes.Remove(note);
                    return $"ok: note #{id} removed.";
                }

                case "get_notes":
                {
                    if (_meetingNotes.Count == 0) return "No notes for this meeting.";
                    var sb = new StringBuilder("Meeting Notes:\n");
                    foreach (var n in _meetingNotes) sb.AppendLine($"  - [#{n.Id}] {n.Text}");
                    return sb.ToString();
                }

                case "add_persistent_note":
                {
                    if (string.IsNullOrWhiteSpace(cmd.Text)) { failed = true; return "error: text is empty."; }
                    var note = new PersistentNote { Id = _nextPersistentNoteId++, TargetPlayerId = cmd.Target, Text = cmd.Text.Trim(), CreatedAt = Time.time };
                    _persistentNotes.Add(note);
                    if (cmd.Target.HasValue)
                    {
                        var pl = Utils.Players.GetPlayerByPlayerId(cmd.Target.Value);
                        string name = pl != null ? $"{pl.name} (ID {cmd.Target.Value})" : $"Player {cmd.Target.Value}";
                        return $"ok: persistent note #{note.Id} added for {name}.";
                    }
                    return $"ok: persistent note #{note.Id} added.";
                }

                case "remove_persistent_note":
                {
                    if (!TryParseNoteId(cmd, out int id)) { failed = true; return "error: remove_persistent_note needs an id (e.g. text: \"1\" or target: 1)."; }
                    var note = _persistentNotes.Find(n => n.Id == id);
                    if (note == null) { failed = true; return $"error: persistent note #{id} not found."; }
                    _persistentNotes.Remove(note);
                    return $"ok: persistent note #{id} removed.";
                }

                case "get_persistent_note":
                {
                    var list = cmd.Target.HasValue
                        ? _persistentNotes.FindAll(n => n.TargetPlayerId == cmd.Target.Value)
                        : _persistentNotes;

                    if (list.Count == 0)
                        return cmd.Target.HasValue ? $"No persistent notes for player {cmd.Target.Value}." : "No persistent notes.";

                    var sb = new StringBuilder("Persistent Notes:\n");
                    foreach (var note in list)
                    {
                        string tag = note.TargetPlayerId.HasValue
                            ? $" (Player {note.TargetPlayerId.Value}{(Utils.Players.GetPlayerByPlayerId(note.TargetPlayerId.Value) is { } pl ? $" - {pl.name}" : "")})"
                            : " (General)";
                        sb.AppendLine($"  - [#{note.Id}]{tag} {note.Text}");
                    }
                    return sb.ToString();
                }

                case "change_suspicious":
                {
                    if (cmd.Target == null) { failed = true; return "error: change_suspicious needs a target (player id)."; }
                    var player = Utils.Players.GetPlayerByPlayerId(cmd.Target.Value);
                    if (player == null) { failed = true; return "error: player not found."; }
                    if (!TryParseAmount(cmd, out float delta, out string err)) { failed = true; return $"error: {err}"; }

                    if (_brain is StructuredAgentBrain brain)
                    {
                        var mem = brain.GetOrCreateMemory(player.PlayerId);
                        if (delta >= 0) mem.IncreaseSuspiciusPercentage(delta);
                        else            mem.DecreaseSuspiciusPercentage(-delta);
                        return $"ok: {player.name} suspiciousness changed by {delta:+0.#;-0.#}% → now {mem.SuspiciusPercentage:0.#}%.";
                    }
                    failed = true; return "error: brain not available.";
                }

                case "set_suspicious":
                {
                    if (cmd.Target == null) { failed = true; return "error: set_suspicious needs a target (player id)."; }
                    var player = Utils.Players.GetPlayerByPlayerId(cmd.Target.Value);
                    if (player == null) { failed = true; return "error: player not found."; }
                    if (!TryParseAmount(cmd, out float val, out string err)) { failed = true; return $"error: {err}"; }

                    if (_brain is StructuredAgentBrain brain)
                    {
                        var mem = brain.GetOrCreateMemory(player.PlayerId);
                        mem.SetSuspiciusPercentage(val);
                        return $"ok: {player.name} suspiciousness set to {mem.SuspiciusPercentage:0.#}%.";
                    }
                    failed = true; return "error: brain not available.";
                }

                case "change_trust":
                {
                    if (cmd.Target == null) { failed = true; return "error: change_trust needs a target (player id)."; }
                    var player = Utils.Players.GetPlayerByPlayerId(cmd.Target.Value);
                    if (player == null) { failed = true; return "error: player not found."; }
                    if (!TryParseAmount(cmd, out float delta, out string err)) { failed = true; return $"error: {err}"; }

                    if (_brain is StructuredAgentBrain brain)
                    {
                        var mem = brain.GetOrCreateMemory(player.PlayerId);
                        if (delta >= 0) mem.IncreaseTrust(delta);
                        else            mem.DecreaseTrust(-delta);
                        return $"ok: {player.name} trust changed by {delta:+0.#;-0.#} → now {mem.Trust:+0.#;-0.#;0} (0=neutral).";
                    }
                    failed = true; return "error: brain not available.";
                }

                case "set_trust":
                {
                    if (cmd.Target == null) { failed = true; return "error: set_trust needs a target (player id)."; }
                    var player = Utils.Players.GetPlayerByPlayerId(cmd.Target.Value);
                    if (player == null) { failed = true; return "error: player not found."; }
                    if (!TryParseAmount(cmd, out float val, out string err)) { failed = true; return $"error: {err}"; }

                    if (_brain is StructuredAgentBrain brain)
                    {
                        var mem = brain.GetOrCreateMemory(player.PlayerId);
                        mem.SetTrust(val);
                        return $"ok: {player.name} trust set to {mem.Trust:+0.#;-0.#;0} (0=neutral).";
                    }
                    failed = true; return "error: brain not available.";
                }

                default:
                    failed = true;
                    return "error: not implemented.";
            }
        }

        private static bool TryParseAmount(LlmCommand cmd, out float amount, out string error)
        {
            amount = 0f;
            error  = null;

            if (cmd.Value.HasValue)
            {
                amount = cmd.Value.Value;
                return true;
            }

            if (string.IsNullOrWhiteSpace(cmd.Text))
            {
                error = "missing value/text amount (e.g. 20, +15, -10).";
                return false;
            }

            string raw = cmd.Text.Trim();
            if (raw.StartsWith('+')) raw = raw[1..].Trim();

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out amount))
                return true;

            error = $"unable to parse number from '{cmd.Text}'.";
            return false;
        }

        private static bool TryParseNoteId(LlmCommand cmd, out int id)
        {
            if (cmd.Target.HasValue) { id = cmd.Target.Value; return id > 0; }
            return int.TryParse(cmd.Text?.Trim(), out id) && id > 0;
        }
    }
}
