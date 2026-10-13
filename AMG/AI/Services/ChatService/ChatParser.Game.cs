using System;
using System.Collections.Generic;
using AMG.AI.Services.ChatService.History;
using AMG.Enums.ChatServiceEnums;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Services.ChatService
{
    public static partial class ChatParser
    {
        private static object _roomsSource;
        private static string _roomsLanguage;
        private static Dictionary<SystemTypes, string> _rooms = [];

        public static MessageParsedData Parse(PlayerControl sender, string text, ChatContextEnum context)
        {
            var players = new List<ChatPlayer>();
            foreach (var p in Utils.Players.AllPlayers)
            {
                if (p == null || p.Data == null) continue;
                players.Add(new ChatPlayer(p.PlayerId, p.Data.PlayerName ?? "", Utils.Colours.GetPlayerColor(p)?.ToString() ?? ""));
            }

            var result = Parse(text, sender.PlayerId, context, players, GetRooms());

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

        private static Dictionary<SystemTypes, string> GetRooms()
        {
            var index = PhraseBank.Index;
            if (ReferenceEquals(index, _roomsSource) && _roomsLanguage == Realizer.Language) return _rooms;

            var rooms = new Dictionary<SystemTypes, string>();
            if (index.TryGetLanguage(Realizer.Language, out var lang) || index.TryGetLanguage(Realizer.DefaultLanguage, out lang))
            {
                foreach (SystemTypes room in Enum.GetValues(typeof(SystemTypes)))
                    if (lang.TryGetRoom(room, out var name)) rooms[room] = name;
            }

            _roomsSource = index;
            _roomsLanguage = Realizer.Language;
            _rooms = rooms;
            return rooms;
        }
    }
}
