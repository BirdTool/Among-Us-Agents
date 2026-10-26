using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AMG.AI.Services.ChatService.Local.Corpus
{

    public sealed class CorpusEvent
    {
        public string Type { get; set; }
        public float SecBefore { get; set; }
    }

    public sealed class CorpusMeeting
    {
        public string Kind { get; set; } = "meeting";
        public string Session { get; set; }
        public int Meeting { get; set; }
        public string Utc { get; set; }
        public int Alive { get; set; }
        public List<CorpusEvent> Events { get; set; } = [];
    }

    public sealed class CorpusParse
    {
        public string Id { get; set; }
        public string Intent { get; set; }
        public string Room { get; set; }
        public float Confidence { get; set; }
    }

    public sealed class CorpusPrev
    {
        public string Speaker { get; set; }
        public string Text { get; set; }
    }

    // LOCAL ONLY (ChatRaw.jsonl): what was actually typed, joined to the shareable records by Id.
    public sealed class CorpusRaw
    {
        public string Kind { get; set; } = "raw";
        public string Id { get; set; }
        public string Raw { get; set; }
    }

    public sealed class CorpusRosterPlayer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
    }

    // LOCAL ONLY (ChatRaw.jsonl): who was in the match, so old messages can be re-masked when the parser improves.
    public sealed class CorpusRoster
    {
        public string Kind { get; set; } = "roster";
        public string Session { get; set; }
        public int Meeting { get; set; }
        public List<CorpusRosterPlayer> Players { get; set; } = [];
    }

    public sealed class CorpusMessage
    {
        public string Kind { get; set; } = "msg";
        public string Id { get; set; }                // session-meeting-seq: the key into ChatRaw.jsonl
        public string Session { get; set; }
        public int Meeting { get; set; }
        public float At { get; set; }                 // seconds since the meeting started
        public string Speaker { get; set; }
        public string Text { get; set; }              // null when filtered
        public string Bucket { get; set; }            // understood | low | unknown | filtered
        public string Category { get; set; }          // only for filtered / flagged
        public string Flag { get; set; }              // flagged but kept (profanity)
        public string Reason { get; set; }
        public CorpusParse Parse { get; set; }
        public List<CorpusPrev> Prev { get; set; }    // the last messages before this one (context: "nav" only makes sense after a question)
        public List<string> Open { get; set; }        // questions still open when it was said
    }

    public static class CorpusJson
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static string Line(object record) => JsonSerializer.Serialize(record, record.GetType(), Options);
    }
}
