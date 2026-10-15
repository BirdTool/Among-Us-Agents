using System;
using System.Collections.Generic;
using AMG.AI.Services.ChatService.History;
using AMG.Enums.ChatServiceEnums;
using AMG.Models.ChatService;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Services.ChatService
{
    public static partial class ChatParser
    {
        private static object _languageSource;
        private static string _languageName;
        private static Dictionary<SystemTypes, string> _rooms = [];
        private static IReadOnlyList<PhraseGroup> _groups;

        // Adds the players currently in the match to the roster (call it early: players may leave later).
        public static List<ChatPlayer> RememberPlayers()
        {
            var current = new List<ChatPlayer>();
            foreach (var p in Utils.Players.AllPlayers)
            {
                if (p == null || p.Data == null) continue;

                // the colour id does not depend on the game's language; the displayed colour name does
                string color = p.Data.DefaultOutfit != null ? ChatLexicon.ColorName(p.Data.DefaultOutfit.ColorId) : "";
                if (color.Length == 0) color = Utils.Colours.GetPlayerColor(p)?.ToString() ?? "";

                current.Add(new ChatPlayer(p.PlayerId, p.Data.PlayerName ?? "", color, p.Data.ColorName));
            }

            return ChatRoster.Merge(current);
        }

        // Everybody seen in the match and the room names of the active language, for the parser and the corpus.
        public static List<ChatPlayer> SnapshotPlayers(out IReadOnlyDictionary<SystemTypes, string> rooms)
        {
            var players = RememberPlayers();
            LoadLanguage();
            rooms = _rooms;
            return players;
        }

        public static MessageParsedData Parse(PlayerControl sender, string text, ChatContextEnum context)
        {
            var players = SnapshotPlayers(out var rooms);
            var result = Parse(text, sender.PlayerId, context, players, rooms, _groups);

            return new MessageParsedData
            {
                Content = text,
                SenderId = sender.PlayerId,
                SenderName = sender.Data.PlayerName,
                TimeStamp = Time.time,
                Intent = result.Intent,
                Context = context,
                Confidence = result.Confidence,
                FromAgent = false,
                Rationale = result.Rationale,
            };
        }

        // Room names and phrase groups of the active language pack; rebuilt only when the bank or the language changes.
        private static void LoadLanguage()
        {
            var index = PhraseBank.Index;
            if (ReferenceEquals(index, _languageSource) && _languageName == Realizer.Language) return;

            var rooms = new Dictionary<SystemTypes, string>();
            IReadOnlyList<PhraseGroup> groups = null;

            if (index.TryGetLanguage(Realizer.Language, out var lang) || index.TryGetLanguage(Realizer.DefaultLanguage, out lang))
            {
                foreach (SystemTypes room in Enum.GetValues(typeof(SystemTypes)))
                    if (lang.TryGetRoom(room, out var name)) rooms[room] = name;

                groups = lang.Groups;
            }

            _languageSource = index;
            _languageName = Realizer.Language;
            _rooms = rooms;
            _groups = groups;
        }
    }
}
