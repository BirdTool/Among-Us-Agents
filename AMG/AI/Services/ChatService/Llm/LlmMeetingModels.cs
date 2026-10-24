using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AMG.AI.Services.ChatService.Llm
{
    public enum LlmTier { Lobby, Low, Medium, High, Laboratory }

    public sealed partial class LlmMeetingSession
    {
        private const int MaxCommandsPerStep = 5;

        private sealed record TierProfile(
            int MaxSteps, float DeadlineSeconds, string[] Commands,
            int MaxTokens = 1024,     // output cap per request
            float MinInterval = 2f,   // minimum pause between chains
            int MaxChatLines = 0,     // 0 = whole chat; otherwise only the last N lines
            bool TrustChat = false,   // chat = orders from the tester
            bool Lobby = false);      // no role, no meeting

        private static readonly string[] IntentionCommandsMedium = [
            "intention_avoid_players"
        ];

        private static readonly string[] IntentionCommandsHighLab = [
            "intention_avoid_players", "intention_avoid_be_alone",
            "intention_follow_player", "intention_priority_kill", "intention_clear"
        ];

        private static readonly string[] AllCommands = [
            "private_message", "send_message", "vote", "wait", "get_player_names",
            "get_role_info", "get_player_info", "get_meeting_observations",
            "get_notes", "add_notes", "remove_notes",
            "get_persistent_note", "add_persistent_note", "remove_persistent_note",
            "change_suspicious", "set_suspicious", "change_trust", "set_trust",
            "intention_avoid_players", "intention_avoid_be_alone",
            "intention_follow_player", "intention_priority_kill", "intention_clear"
        ];

        private static readonly System.Collections.Generic.Dictionary<LlmTier, TierProfile> Profiles = new()
        {
            [LlmTier.Lobby] = new(2, 15f, ["send_message", "wait", "private_message", "get_player_names"], MaxTokens: 200, MinInterval: 12f, MaxChatLines: 15, Lobby: true),
            [LlmTier.Low] = new(3, 20f, ["send_message", "vote", "wait", "private_message", "get_player_names"], MinInterval: 8f),
            [LlmTier.Medium] = new(6, 40f, [
                "send_message", "vote", "wait", "private_message", "get_player_names",
                "get_role_info", "get_player_info", "get_meeting_observations",
                "get_persistent_note", "add_persistent_note", "remove_persistent_note",
                "change_suspicious", "set_suspicious", "change_trust", "set_trust",
                "intention_avoid_players"
            ], MinInterval: 8f),
            [LlmTier.High] = new(12, 90f, AllCommands, MinInterval: 8f),
            [LlmTier.Laboratory] = new(15, 120f, AllCommands, MaxTokens: 2048, MinInterval: 1f, TrustChat: true),
        };

        private static readonly Dictionary<string, string> CommandDocs = new()
        {
            ["private_message"] = "private_message {text}: send a private message to the developer (they wont receive if they're not playing the same match).",

            ["send_message"] = "send_message {text}: say something in the meeting chat (max 100 characters).",
            ["vote"] = "vote {target}: vote for a player id, or 253 to skip. Not allowed during discussion; one vote only.",
            ["wait"] = "wait: do nothing until new chat messages arrive. Use it when you have nothing to add.",
            ["get_player_names"] = "get_player_names: the real name of every player next to their colour and id. Use it when someone writes a name in the chat and you do not know which colour it is.",
            ["get_role_info"] = "get_role_info {text}: rules and abilities of a role, e.g. \"Judge\", \"Scientist\", or empty for your own role.",
            ["get_player_info"] = "get_player_info {target}: all known data on a player (memories, position, crimes, trust, suspicion).",
            ["get_meeting_observations"] = "get_meeting_observations: what you personally witnessed this round (bodies, crimes, reporter, etc.).",
            ["add_notes"] = "add_notes {text}: add a temporary note to this meeting.",
            ["remove_notes"] = "remove_notes {text/target}: remove a meeting note by its id.",
            ["get_notes"] = "get_notes: list all temporary notes for this meeting.",
            ["add_persistent_note"] = "add_persistent_note {text, target?}: add a note (optionally tied to a player) that persists across meetings.",
            ["remove_persistent_note"] = "remove_persistent_note {text/target}: remove a persistent note by its id.",
            ["get_persistent_note"] = "get_persistent_note {target?}: list persistent notes, optionally filtered by player id.",
            ["change_suspicious"] = "change_suspicious {target, text/value}: adjust suspiciousness by +/- delta (e.g. \"+20\", \"-10\"). Range 0-100.",
            ["set_suspicious"] = "set_suspicious {target, text/value}: set suspiciousness to an exact value. Range 0-100.",
            ["change_trust"] = "change_trust {target, text/value}: adjust trust by +/- delta (e.g. \"+15\", \"-20\"). Range -100..+100, 0=neutral.",
            ["set_trust"] = "set_trust {target, text/value}: set trust to an exact value. Range -100..+100, 0=neutral, negative=distrust, positive=trusted.",

            // --- Intention commands ---
            ["intention_avoid_players"] = "intention_avoid_players {target?, text?}: manage the avoid-players list. Use target=player-id to add a player. text=\"remove {id}\" to remove one. text=\"all\" to avoid every player. text=\"clear\" to clear the whole list.",
            ["intention_avoid_be_alone"] = "intention_avoid_be_alone {text}: text=\"on\" makes the agent wander to find company when alone; text=\"off\" disables it.",
            ["intention_follow_player"] = "intention_follow_player {target, value?}: add a player to follow with priority 1-100 (value field). The follow is not permanent — it fires only when the target is visible and stops naturally after a while. Use value=0 to remove the player from the list.",
            ["intention_priority_kill"] = "intention_priority_kill {target?, text?}: (impostors only) add a player as a priority kill target (target=player-id). text=\"clear\" to clear all priority targets. Text=\"remove {id}\" to remove one.",
            ["intention_clear"] = "intention_clear: reset ALL active intentions (avoid lists, follow list, priority kills, avoid-be-alone).",
            ["intentions_get"] = "intentions_get: get all active intentions."
        };

        private static readonly System.Collections.Generic.HashSet<string> InfoCommands =
        [
            "get_observations", "get_player_names", "get_role_info", "get_player_info", 
            "get_meeting_observations", "get_notes", "add_notes", "remove_notes",
            "get_persistent_note", "add_persistent_note", "remove_persistent_note",
            "change_suspicious", "set_suspicious", "change_trust", "set_trust",
            "change_suspicion", "set_suspicion",
            "intention_avoid_players", "intention_avoid_be_alone",
            "intention_follow_player", "intention_priority_kill", "intention_clear",
        ];
    }

    public sealed class MeetingNote
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public float CreatedAt { get; set; }
    }

    public sealed class PersistentNote
    {
        public int Id { get; set; }
        public byte? TargetPlayerId { get; set; }
        public string Text { get; set; }
        public float CreatedAt { get; set; }
        public bool Auto { get; set; }   // written by code from what the agent witnessed, not by the model
    }

    public sealed class LlmTurn
    {
        public string Reasoning { get; set; }
        public List<LlmCommand> Commands { get; set; } = [];

        // Allows the LLM to respond with a single "command" key instead of "commands" list.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LlmCommand Command
        {
            get => Commands.Count > 0 ? Commands[0] : null;
            set
            {
                if (value != null && (Commands == null || Commands.Count == 0))
                    Commands = [value];
            }
        }
    }

    public sealed class LlmCommand
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public byte? Target { get; set; }
        public float? Value { get; set; }
    }
}
