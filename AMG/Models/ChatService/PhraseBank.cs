using System.Collections.Generic;
using AMG.Enums.ChatServiceEnums;

namespace AMG.Models.ChatService
{
    public class PhraseBankData
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public Dictionary<string, LanguagePack> Languages { get; set; } = []; // "pt", "en"
    }

    public class LanguagePack
    {
        public Dictionary<SystemTypes, string> Rooms { get; set; } = []; // Security -> "segurança"
        public List<PhraseGroup> Phrases { get; set; } = [];
    }

    public class PhraseGroup
    {
        public string Id { get; set; } = "";
        public ChatIntentEnum Intent { get; set; }
        public ChatContextEnum? Context { get; set; }

        public float Weight { get; set; } = 1f;

        public string? ReplyTo { get; set; }

        public EvidenceEnum? Evidence { get; set; }

        public List<string>? Tags { get; set; }

        public PhraseSourceEnum? Source { get; set; }

        public List<PhraseVariant> Phrases { get; set; } = [];
    }

    public class PhraseVariant
    {
        public List<string> Tone { get; set; } = [];
        public float Weight { get; set; } = 1f;
        public string Text { get; set; } = "";
        public float MinConfidence { get; set; } = 0f;
        public float MaxConfidence { get; set; } = 1f;

        public PhraseSourceEnum? Source { get; set; }
    }
}