using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AMG.Enums.ChatServiceEnums;
using AMG.Models.ChatService;

namespace AMG.AI.Services.ChatService
{
    // Understands free sentences by comparing them with the PhraseBank itself: every variant of every group is a template,
    // so phrases and groups added later (e.g. by the external AI) are understood without touching C#.
    public static partial class ChatParser
    {
        private const float TemplateThreshold = 0.72f;

        // Per meaning word the template has and the sentence lacks.
        private const float MissingMeaningPenalty = 0.12f;

        private const string RoomSlot = "zzroomzz";
        private const string PlayerSlot = "zzplayerzz";

        private static readonly Regex PossessiveSlot = new(@"(\{(?:target|subject|witness|room)\})'s", RegexOptions.Compiled);

        private sealed class Template
        {
            public PhraseGroup Group;
            public string Text;
            public HashSet<string> Grams;
            public string[] Tokens;
            public HashSet<string> Words;
            public readonly List<string> PlayerRoles = []; // "target" / "subject" / "witness", in order of appearance
            public bool HasRoom;
            public bool Negated;
            public string[] MeaningWords;   // words that carry meaning (not slots, fillers, soft words, question words)
        }

        private static IReadOnlyList<PhraseGroup> _templatesSource;
        private static int _templatesCount;
        private static List<Template> _templates = [];

        private static List<Template> GetTemplates(IReadOnlyList<PhraseGroup> groups)
        {
            if (ReferenceEquals(groups, _templatesSource) && groups.Count == _templatesCount) return _templates;

            var list = new List<Template>();
            foreach (var g in groups)
            {
                foreach (var v in g.Phrases)
                {
                    var t = BuildTemplate(g, v.Text);
                    if (t != null) list.Add(t);
                }
            }

            _templatesSource = groups;
            _templatesCount = groups.Count;
            _templates = list;
            return list;
        }

        private static Template BuildTemplate(PhraseGroup group, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            var t = new Template { Group = group, Text = text };

            var prepared = PossessiveSlot.Replace(text, "$1")
                .Replace("{target}", " zztargetzz ").Replace("{subject}", " zzsubjectzz ")
                .Replace("{witness}", " zzwitnesszz ").Replace("{room}", " " + RoomSlot + " ");

            var tokens = new List<string>();
            foreach (var tok in ChatLexicon.Normalize(prepared).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                switch (tok)
                {
                    case "zztargetzz": t.PlayerRoles.Add("target"); tokens.Add(PlayerSlot); break;
                    case "zzsubjectzz": t.PlayerRoles.Add("subject"); tokens.Add(PlayerSlot); break;
                    case "zzwitnesszz": t.PlayerRoles.Add("witness"); tokens.Add(PlayerSlot); break;
                    case RoomSlot: t.HasRoom = true; tokens.Add(RoomSlot); break;
                    default: tokens.Add(tok); break;
                }
            }

            if (tokens.Count == 0) return null;

            t.Negated = tokens.Any(ChatLexicon.Negators.Contains);
            t.Tokens = [.. tokens];
            t.MeaningWords = [.. tokens.Where(w => w != PlayerSlot && w != RoomSlot && w != "?" && !ChatLexicon.SoftWords.Contains(w)
                && !ChatLexicon.Fillers.Contains(w) && !ChatLexicon.Interrogatives.Contains(w)).Distinct()];
            t.Words = [.. tokens];
            t.Grams = Grams(tokens);
            return t;
        }

        private static ParseResult MatchTemplates(List<string> tokens, Entities ent, IReadOnlyList<PhraseGroup> groups, ChatContextEnum context)
        {
            var masked = new List<string>(tokens.Count);
            for (int i = 0; i < tokens.Count; i++)
            {
                switch (ent.Kind[i])
                {
                    case 0: masked.Add(tokens[i]); break;
                    case 1: masked.Add(RoomSlot); break;
                    case 2: masked.Add(PlayerSlot); break;
                }
            }

            // "shields", "medbay" are rooms AND tasks: templates that name them literally must see the word, not a room slot
            var plain = new List<string>(tokens.Count);
            for (int i = 0; i < tokens.Count; i++)
            {
                switch (ent.Kind[i])
                {
                    case 0: case 1: plain.Add(tokens[i]); break;
                    case 2: plain.Add(PlayerSlot); break;
                }
            }

            if (plain.All(t => t == PlayerSlot || t == "?")) return null;

            var message = Grams(masked);
            var messagePlain = Grams(plain);
            var wordsPlain = new HashSet<string>(plain);
            bool negated = masked.Any(ChatLexicon.Negators.Contains);
            bool hasContent = plain.Any(t => t != PlayerSlot && t != "?" && !ChatLexicon.SoftWords.Contains(t) && !ChatLexicon.Fillers.Contains(t)
                && !ChatLexicon.Interrogatives.Contains(t));

            bool messageAsks = masked.Contains("?");

            Template best = null;
            float bestScore = 0f;

            foreach (var t in GetTemplates(groups))
            {
                if (t.PlayerRoles.Count != ent.PlayerSlots.Count) continue; // the sentence must name exactly as many players as the template has slots
                if (t.HasRoom && ent.Room == null) continue;                // nothing to put in {room}
                if (t.Negated != negated) continue;                         // "didnt vent" is never "vented"

                bool templateAsks = t.Words.Contains("?");
                bool statementForQuestion = messageAsks && !templateAsks && t.Group.Intent != ChatIntentEnum.Question;

                float dice = Dice(message, t.Grams);
                if (!t.HasRoom) dice = Math.Max(dice, Dice(messagePlain, t.Grams));
                if (statementForQuestion) dice -= 0.25f;                    // "body in elec?" is not "Body in {room}"

                // the template says something the sentence does not ("did anyone see X" is not "did anyone see X scan")
                int missing = t.MeaningWords.Count(w => !wordsPlain.Contains(w));
                dice -= MissingMeaningPenalty * missing;

                float quality = dice;

                // short sentence ("red vented") for a longer template ("{target} vented, I saw it!"): every word of the
                // sentence is in the template and what the template adds is only filler, never meaning
                if (quality < TemplateThreshold && hasContent && !t.HasRoom && !statementForQuestion
                    && wordsPlain.All(w => t.Words.Contains(w))
                    && t.Tokens.Where(w => !wordsPlain.Contains(w)).All(w => w == PlayerSlot || w == "?" || ChatLexicon.SoftWords.Contains(w) || ChatLexicon.Fillers.Contains(w)))
                {
                    quality = TemplateThreshold + 0.2f * dice;
                }

                // the threshold is decided on quality alone; the context bonus only ranks candidates that already qualify
                if (quality < TemplateThreshold) continue;

                float rank = quality + (t.Group.Context == context ? 0.02f : 0f);
                if (rank > bestScore) { bestScore = rank; best = t; }
            }

            if (best == null) return null;

            if (ent.PlayerSlots.Any(s => s == null))
                return Unknown("matched a template but a player name is ambiguous");

            var g = best.Group;
            var intent = new ChatIntent
            {
                Type = g.Intent,
                PhraseId = g.Id,
                Context = context,
                Evidence = g.Evidence,
                Room = ent.Room,
            };

            for (int i = 0; i < best.PlayerRoles.Count; i++)
            {
                byte id = ent.PlayerSlots[i].Value;
                switch (best.PlayerRoles[i])
                {
                    case "target": intent.Target = id; break;
                    case "subject": intent.Subject = id; break;
                    case "witness": intent.Witness = id; break;
                }
            }

            float confidence = Math.Min(0.95f, bestScore);
            return Ok(intent, confidence, $"template \"{best.Text}\" ({g.Id}/{g.Intent}) {bestScore:0.00}");
        }

        private static HashSet<string> Grams(List<string> tokens)
        {
            var set = new HashSet<string>();
            for (int i = 0; i < tokens.Count; i++)
            {
                set.Add(tokens[i]);
                if (i + 1 < tokens.Count) set.Add(tokens[i] + " " + tokens[i + 1]);
            }
            return set;
        }

        private static float Dice(HashSet<string> a, HashSet<string> b)
        {
            if (a.Count == 0 || b.Count == 0) return 0f;
            int common = 0;
            foreach (var x in a) if (b.Contains(x)) common++;
            return 2f * common / (a.Count + b.Count);
        }
    }
}
