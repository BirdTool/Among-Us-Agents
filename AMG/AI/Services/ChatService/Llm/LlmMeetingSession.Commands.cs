using System;
using System.Globalization;
using System.Linq;
using System.Text;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Services.MessagesService;
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

            if (cmd == null || string.IsNullOrEmpty(cmdName) || Array.IndexOf(Profiles[Active].Commands, cmdName) < 0)
            {
                failed = true;
                return $"error: unknown command '{cmd?.Name}'.";
            }

            switch (cmdName)
            {
                case "private_message":
                {
                    if (string.IsNullOrWhiteSpace(cmd.Text)) { failed = true; return "error: text is empty."; }
                    MessageServiceClass.WriteMessage(cmd.Text, PrivateMessageSenderEnum.LLM);
                    return "ok: message sent to developer.";
                }

                case "send_message":
                {
                    if (string.IsNullOrWhiteSpace(cmd.Text)) { failed = true; return "error: text is empty."; }

                    var profile = Profiles[Active];
                    if (!profile.Lobby && !profile.TrustChat)
                    {
                        if (_outbox.Count > 0)
                            return "skipped: you already have a message waiting to be sent. One at a time; use wait.";
                        if (_sentThisMeeting >= MaxMessagesPerMeeting)
                            return $"skipped: you already used your {MaxMessagesPerMeeting} messages for this meeting. Use wait, or vote when voting opens.";
                        if (IsRepeat(cmd.Text))
                            return "skipped: that is too similar to something you already said. Say something new or use wait.";
                    }

                    var check = CheckSay(cmd.Text);
                    // cooldown is not an error here: PumpOutbox keeps the message queued until it can go out
                    if (check != ChatRpcEnums.SUCCESS && check != ChatRpcEnums.ERROR_InCooldown) { failed = true; return $"error: {check}"; }
                    _outbox.Enqueue(new OutboxItem(cmd.Text, Time.time));
                    return "ok: queued, it will appear in the chat in a few seconds.";
                }

                case "vote":
                {
                    if (cmd.Target == null) { failed = true; return "error: vote needs a target (player id, or 253 to skip)."; }
                    var r = Vote(cmd.Target);
                    if (r == VoteRpcEnums.ERROR_IsNotInVoteTime)
                    {
                        _pendingVote = cmd.Target;   // cast automatically when voting opens
                        terminal = true;
                        return "ok: voting is not open yet; your vote is scheduled and will be cast automatically when voting opens. (/vote again to change the target if you change your mind)";
                    }
                    if (r != VoteRpcEnums.SUCCESS) { failed = true; return $"error: {r}"; }
                    terminal = true;
                    return "ok: vote cast.";
                }

                case "wait":
                    terminal = true;
                    return "ok: waiting for new messages.";

                case "get_player_names":
                    return GetPlayerNames();

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
                        string name = pl != null ? $"{Colour(pl)} (ID {cmd.Target.Value})" : $"Player {cmd.Target.Value}";
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
                            ? $" (Player {note.TargetPlayerId.Value}{(Utils.Players.GetPlayerByPlayerId(note.TargetPlayerId.Value) is { } pl ? $" - {Colour(pl)}" : "")})"
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
                        return $"ok: {Colour(player)} suspiciousness changed by {delta:+0.#;-0.#}% → now {mem.SuspiciusPercentage:0.#}%.";
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
                        return $"ok: {Colour(player)} suspiciousness set to {mem.SuspiciusPercentage:0.#}%.";
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
                        return $"ok: {Colour(player)} trust changed by {delta:+0.#;-0.#} → now {mem.Trust:+0.#;-0.#;0} (0=neutral).";
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
                        return $"ok: {Colour(player)} trust set to {mem.Trust:+0.#;-0.#;0} (0=neutral).";
                    }
                    failed = true; return "error: brain not available.";
                }

                case "intention_avoid_players":
                {
                    if (cmd.Target == null && string.IsNullOrEmpty(cmd.Text))
                        return "error: intention_avoid_players needs a target (player id) or text action.";

                    if (cmd.Target.HasValue)
                    {
                        var player = Utils.Players.GetPlayerByPlayerId(cmd.Target.Value);
                        if (player == null) { failed = true; return "error: player not found."; }

                        if (_brain is StructuredAgentBrain brain)
                        {
                            brain.Intentions.AvoidPlayers.Add(player.PlayerId);
                            return "ok: added to avoid list.";
                        }
                    }
                    if (!string.IsNullOrEmpty(cmd.Text))
                    {
                        var txt = cmd.Text.Trim().ToLowerInvariant();
                        if (_brain is StructuredAgentBrain brain)
                        {
                            if (txt == "clear")
                            {
                                brain.Intentions.AvoidPlayers.Clear();
                                return "ok: cleared avoid list.";
                            }
                            else if (txt == "all")
                            {
                                brain.Intentions.AvoidAllPlayers = true;
                                return "ok: avoid all players.";
                            }
                            else if (txt.StartsWith("remove "))
                            {
                                var idStr = txt["remove ".Length..].Trim();
                                if (string.IsNullOrEmpty(idStr))
                                    return "error: remove needs a player id.";

                                if (byte.TryParse(idStr, out byte playerId))
                                {
                                    if (brain.Intentions.AvoidPlayers.Contains(playerId))
                                    {
                                        brain.Intentions.AvoidPlayers.Remove(playerId);
                                        return $"ok: removed player {playerId} from avoid list.";
                                    }
                                    else
                                        return "error: player not found in avoid list.";
                                }
                                else
                                    return "error: invalid player id.";
                            }
                            failed = true;
                            return "error: invalid command.";
                        }
                    }

                    failed = true;
                    return "error: missing parameter or invalid parameter.";
                }

                case "intention_avoid_be_alone":
                {
                    if (string.IsNullOrWhiteSpace(cmd.Text)) { failed = true; return "error: missing parameter (on/off)."; }
                    
                    if (_brain is StructuredAgentBrain brain)
                    {
                        var txt = cmd.Text.Trim().ToLowerInvariant();
                        if (txt == "on")
                        {
                            brain.Intentions.AvoidBeAlone = true;
                            return "ok: avoid_be_alone is now ON.";
                        }
                        else if (txt == "off")
                        {
                            brain.Intentions.AvoidBeAlone = false;
                            return "ok: avoid_be_alone is now OFF.";
                        }
                        failed = true;
                        return "error: text must be exactly 'on' or 'off'.";
                    }
                    failed = true; return "error: brain not available.";
                }

                case "intention_follow_player":
                {
                    if (cmd.Target == null) { failed = true; return "error: intention_follow_player needs a target (player id)."; }
                    
                    var player = Utils.Players.GetPlayerByPlayerId(cmd.Target.Value);
                    if (player == null) { failed = true; return "error: player not found."; }

                    if (_brain is StructuredAgentBrain brain)
                    {
                        float priority = 50f; // Default priority if none provided
                        if (TryParseAmount(cmd, out float parsedVal, out _)) 
                        {
                            priority = parsedVal;
                        }

                        // Remove existing follow intent for this player to prevent duplicates
                        brain.Intentions.FollowPlayers.RemoveAll(x => x.playerId == player.PlayerId);

                        if (priority <= 0)
                        {
                            return $"ok: stopped following player {player.PlayerId}.";
                        }
                        else
                        {
                            brain.Intentions.FollowPlayers.Add((player.PlayerId, priority));
                            return $"ok: following player {player.PlayerId} with priority {priority:0.#}.";
                        }
                    }
                    failed = true; return "error: brain not available.";
                }

                case "intention_priority_kill":
                {
                    if (cmd.Target == null && string.IsNullOrEmpty(cmd.Text))
                        return "error: intention_priority_kill needs a target (player id) or text action.";
                    if (!_brain.IsImpostor) { failed = true; return "error: only impostors can use this command."; }
                    if (_brain is StructuredAgentBrain brain)
                    {
                        if (!string.IsNullOrEmpty(cmd.Text))
                        {
                            var txt = cmd.Text.Trim().ToLowerInvariant();
                            if (txt == "clear")
                            {
                                brain.Intentions.PriorityKillTargets.Clear();
                                return "ok: cleared priority kill list.";
                            }
                            else if (txt.StartsWith("remove "))
                            {
                                var idStr = txt["remove ".Length..].Trim();
                                if (byte.TryParse(idStr, out byte playerId))
                                {
                                    if (brain.Intentions.PriorityKillTargets.Contains(playerId))
                                    {
                                        brain.Intentions.PriorityKillTargets.Remove(playerId);
                                        return $"ok: removed player {playerId} from priority kill list.";
                                    }
                                    return "error: player not found in priority kill list.";
                                }
                                return "error: invalid player id.";
                            }
                        }

                        if (cmd.Target.HasValue)
                        {
                            var player = Utils.Players.GetPlayerByPlayerId(cmd.Target.Value);
                            if (player == null) { failed = true; return "error: player not found."; }

                            if (!brain.Intentions.PriorityKillTargets.Contains(player.PlayerId))
                            {
                                brain.Intentions.PriorityKillTargets.Add(player.PlayerId);
                            }
                            return $"ok: added player {player.PlayerId} to priority kill list.";
                        }

                        failed = true; return "error: invalid command.";
                    }
                    failed = true; return "error: brain not available.";
                }

                case "intention_clear":
                {
                    if (_brain is StructuredAgentBrain brain)
                    {
                        brain.Intentions.Clear();
                        return "ok: all active intentions (avoid, follow, kill targets) have been cleared.";
                    }
                    failed = true; return "error: brain not available.";
                }

                case "intentions_get":
                {
                    if (_brain is StructuredAgentBrain brain)
                    {
                        var sb = new StringBuilder("Active Intentions:\n");
                        var i = brain.Intentions;

                        if (i.AvoidAllPlayers) 
                            sb.AppendLine("- Avoid Players: ALL");
                        else if (i.AvoidPlayers.Count > 0) 
                            sb.AppendLine($"- Avoid Players: {string.Join(", ", i.AvoidPlayers)}");
                        
                        if (i.FollowPlayers.Count > 0)
                        {
                            var followList = string.Join(", ", i.FollowPlayers.Select(f => $"{f.playerId} (pri:{f.priority})"));
                            sb.AppendLine($"- Follow Players: {followList}");
                        }
                        
                        if (i.PriorityKillTargets.Count > 0) 
                            sb.AppendLine($"- Priority Kills: {string.Join(", ", i.PriorityKillTargets)}");
                        
                        sb.AppendLine($"- Avoid Being Alone: {(i.AvoidBeAlone ? "ON" : "OFF")}");

                        return sb.ToString().TrimEnd();
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
