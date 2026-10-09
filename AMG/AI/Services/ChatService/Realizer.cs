using System;
using System.Collections.Generic;
using System.Linq;
using AMG.Models.ChatService;
using AMG.Utilities;

namespace AMG.AI.Services.ChatService
{
    public static class Realizer
    {
        public const string DefaultLanguage = "en";

        public static string Language { get; set; } = DefaultLanguage;

        public static string Realize(ChatIntent intent, string tone = null)
        {
            if (intent == null) return null;

            var index = PhraseBank.Index;

            if (!index.TryGetLanguage(Language, out var lang) &&
                !index.TryGetLanguage(DefaultLanguage, out lang))
                return null;

            var group = lang.Find(intent.PhraseId, intent.Type, intent.Context);
            if (group == null || group.Phrases.Count == 0) return null;

            var variant = PickVariant(group, intent.Confidence, tone);
            if (variant == null) return null;

            return Fill(variant.Text, intent, lang);
        }

        private static PhraseVariant PickVariant(PhraseGroup group, float confidence, string tone)
        {
            var fits = group.Phrases
                .Where(p => confidence >= p.MinConfidence && confidence <= p.MaxConfidence)
                .ToList();

            if (fits.Count == 0) return null;

            if (!string.IsNullOrEmpty(tone))
            {
                var withTone = fits
                    .Where(p => p.Tone.Any(t => string.Equals(t, tone, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (withTone.Count > 0) fits = withTone;
            }

            return fits.GetRandomWeighted(p => p.Weight);
        }

        private static string Fill(string text, ChatIntent intent, LanguageIndex lang)
        {
            var values = new Dictionary<string, string>
            {
                ["target"] = PlayerName(intent.Target),
                ["subject"] = PlayerName(intent.Subject),
                ["witness"] = PlayerName(intent.Witness),
                ["room"] = RoomName(intent.Room, lang)
            };

            foreach (var kv in values)
            {
                var token = "{" + kv.Key + "}";
                if (!text.Contains(token)) continue;

                // The intent didn't provide what the phrase needs: better to say nothing than to send "{target}".
                if (kv.Value == null)
                {
                    LogManager.LogWarning($"[Realizer] '{intent.PhraseId}' needs {token} but the intent has no value for it.");
                    return null;
                }

                text = text.Replace(token, kv.Value);
            }

            return text;
        }

        private static string PlayerName(byte? playerId)
        {
            if (playerId == null) return null;

            var player = Utils.Players.GetPlayerByPlayerId(playerId.Value);
            var name = player?.Data?.PlayerName;

            return string.IsNullOrWhiteSpace(name) ? null : name;
        }

        private static string RoomName(SystemTypes? room, LanguageIndex lang)
        {
            if (room == null) return null;
            return lang.TryGetRoom(room.Value, out var name) ? name : room.Value.ToString();
        }
    }
}