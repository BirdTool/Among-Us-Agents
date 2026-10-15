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

        public static List<ChatPlayer> SnapshotPlayers(out IReadOnlyDictionary<SystemTypes, string> rooms)
        {
            var players = new List<ChatPlayer>();
            foreach (var p in Utils.Players.AllPlayers)
            {
                if (p == null || p.Data == null) continue;
                players.Add(new ChatPlayer(p.PlayerId, p.Data.PlayerName ?? "", Utils.Colours.GetPlayerColor(p)?.ToString() ?? ""));
            }

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
