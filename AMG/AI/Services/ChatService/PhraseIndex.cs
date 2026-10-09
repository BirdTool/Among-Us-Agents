using System;
using System.Collections.Generic;
using AMG.Enums.ChatServiceEnums;
using AMG.Models.ChatService;

namespace AMG.AI.Services.ChatService
{
    public sealed class LanguageIndex
    {
        private static readonly IReadOnlyList<PhraseGroup> None = [];

        private readonly Dictionary<SystemTypes, string> _rooms;
        private readonly Dictionary<string, List<PhraseGroup>> _byIdIntent = [];
        private readonly Dictionary<string, List<PhraseGroup>> _byReplyTo = [];
        private readonly Dictionary<string, List<PhraseGroup>> _byTag = [];
        private readonly List<PhraseGroup> _withEvidence = [];

        public IReadOnlyList<PhraseGroup> Groups { get; }

        public LanguageIndex(Dictionary<SystemTypes, string> rooms, List<PhraseGroup> groups)
        {
            _rooms = new Dictionary<SystemTypes, string>(rooms);
            Groups = groups;

            foreach (var g in groups)
            {
                Add(_byIdIntent, Key(g.Id, g.Intent), g);
                if (!string.IsNullOrEmpty(g.ReplyTo)) Add(_byReplyTo, g.ReplyTo!, g);
                if (g.Tags != null)
                    foreach (var tag in g.Tags) Add(_byTag, tag, g);
                if (g.Evidence != null) _withEvidence.Add(g);
            }
        }

        public bool TryGetRoom(SystemTypes room, out string name) => _rooms.TryGetValue(room, out name!);

        public PhraseGroup? Find(string id, ChatIntentEnum intent, ChatContextEnum? context)
        {
            if (!_byIdIntent.TryGetValue(Key(id, intent), out var candidates)) return null;

            if (context != null)
                foreach (var g in candidates)
                    if (g.Context == context) return g;

            foreach (var g in candidates)
                if (g.Context == null) return g;

            return null;
        }

        public List<PhraseGroup> FindByCondition(ChatIntentEnum intent, ChatContextEnum? context, EvidenceEnum evidence)
        {
            var result = new List<PhraseGroup>();
            foreach (var g in _withEvidence)
                if (g.Intent == intent && g.Evidence == evidence && ContextFits(g, context))
                    result.Add(g);
            return result;
        }

        public IReadOnlyList<PhraseGroup> FindReplies(string replyToId) =>
            _byReplyTo.TryGetValue(replyToId, out var list) ? list : None;

        public List<PhraseGroup> FindByTag(string tag, ChatContextEnum? context)
        {
            var result = new List<PhraseGroup>();
            if (_byTag.TryGetValue(tag, out var list))
                foreach (var g in list)
                    if (ContextFits(g, context)) result.Add(g);
            return result;
        }

        private static bool ContextFits(PhraseGroup g, ChatContextEnum? context) =>
            g.Context == null || g.Context == context;

        private static string Key(string id, ChatIntentEnum intent) => id + "|" + intent;

        private static void Add(Dictionary<string, List<PhraseGroup>> map, string key, PhraseGroup g)
        {
            if (!map.TryGetValue(key, out var list)) map[key] = list = [];
            list.Add(g);
        }
    }

    public sealed class PhraseIndex(Dictionary<string, LanguageIndex> languages)
    {
        public static readonly PhraseIndex Empty = new([]);

        private readonly Dictionary<string, LanguageIndex> _languages = languages;

        public IEnumerable<string> Languages => _languages.Keys;

        public bool TryGetLanguage(string language, out LanguageIndex index) =>
            _languages.TryGetValue(language, out index!);
    }
}