using System.Collections.Generic;
using UnityEngine;

namespace AMG.AI.Services.ChatService.Llm
{
    public sealed class LlmTranscriptEntry
    {
        public int Index;          // 1 = first message of the meeting
        public byte SenderId;
        public string SenderName;
        public string Text;        // exactly what was typed
        public float At;           // Time.time when it arrived
    }

    public sealed record LlmMeetingLog(int Meeting, List<LlmTranscriptEntry> Entries);

    public static class LlmMeetingTranscript
    {
        private const int MaxMeetingsKept = 6;

        private static readonly List<LlmTranscriptEntry> _entries = [];
        private static readonly List<LlmMeetingLog> _history = [];

        // Chat of the meetings that already ended in this match, oldest first.
        public static IReadOnlyList<LlmMeetingLog> History => _history;

        public static IReadOnlyList<LlmTranscriptEntry> All => _entries;

        // 1 = first meeting of the match. Lets directives say in which meeting they were received.
        public static int MeetingNumber { get; private set; }

        public static void Add(PlayerControl sender, string text)
        {
            if (sender == null || sender.Data == null || string.IsNullOrWhiteSpace(text)) return;

            _entries.Add(new LlmTranscriptEntry
            {
                Index = _entries.Count + 1,
                SenderId = sender.PlayerId,
                SenderName = sender.Data.PlayerName,
                Text = text,
                At = Time.time,
            });
        }

        // Entries after `lastSeenIndex` (0 = everything): lets an LLM loop ask only for what is new.
        public static List<LlmTranscriptEntry> Since(int lastSeenIndex) =>
            _entries.FindAll(e => e.Index > lastSeenIndex);

        public static void Reset()
        {
            if (_entries.Count > 0)
                _history.Add(new LlmMeetingLog(MeetingNumber, [.. _entries]));
            while (_history.Count > MaxMeetingsKept) _history.RemoveAt(0);

            _entries.Clear();
            MeetingNumber++;
            LlmMeetingContext.MeetingStarted();
        }

        public static void NewMatch()
        {
            _entries.Clear();
            _history.Clear();
            MeetingNumber = 0;
            LlmMeetingContext.Clear();
        }
    }
}
