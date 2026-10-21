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

        private static readonly string[] AllCommands = [
            "send_message", "vote", "wait",
            "get_role_info", "get_player_info", "get_meeting_observations",
            "get_notes", "add_notes", "remove_notes",
            "get_persistent_note", "add_persistent_note", "remove_persistent_note",
            "change_suspicious", "set_suspicious", "change_trust", "set_trust"
        ];

        private static readonly System.Collections.Generic.Dictionary<LlmTier, TierProfile> Profiles = new()
        {
            [LlmTier.Lobby] = new(2, 15f, ["send_message", "wait"], MaxTokens: 200, MinInterval: 12f, MaxChatLines: 15, Lobby: true),
            [LlmTier.Low] = new(3, 20f, ["send_message", "vote", "wait"]),
            [LlmTier.Medium] = new(6, 40f, [
                "send_message", "vote", "wait",
                "get_role_info", "get_player_info", "get_meeting_observations",
                "get_persistent_note", "add_persistent_note", "remove_persistent_note",
                "change_suspicious", "set_suspicious", "change_trust", "set_trust"
            ]),
            [LlmTier.High] = new(12, 90f, AllCommands),
            [LlmTier.Laboratory] = new(15, 120f, AllCommands, MaxTokens: 2048, MinInterval: 1f, TrustChat: true),
        };

        private static readonly Dictionary<string, string> CommandDocs = new()
        {
            ["send_message"] = "send_message {text}: say something in the meeting chat (max 100 characters).",
            ["vote"] = "vote {target}: vote for a player id, or 253 to skip. Not allowed during discussion; one vote only.",
            ["wait"] = "wait: do nothing until new chat messages arrive. Use it when you have nothing to add.",
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
        };

        private static readonly System.Collections.Generic.HashSet<string> InfoCommands =
        [
            "get_observations", "get_role_info", "get_player_info", "get_meeting_observations",
            "get_notes", "add_notes", "remove_notes",
            "get_persistent_note", "add_persistent_note", "remove_persistent_note",
            "change_suspicious", "set_suspicious", "change_trust", "set_trust",
            "change_suspicion", "set_suspicion",
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
