using System;
using System.Collections.Generic;
using AMG.Models.ChatService;
using UnityEngine;

namespace AMG.AI.Services.ChatService
{
    public static class ChatOutbox
    {
        private const float Ttl = 8f;
        private static readonly List<(byte sender, string text, ChatIntent intent, float time)> _items = [];

        public static void Register(byte senderId, string text, ChatIntent intent)
        {
            if (intent == null || string.IsNullOrWhiteSpace(text)) return;
            Prune();
            _items.Add((senderId, text.Trim(), intent, Time.time));
        }

        public static bool TryTake(byte senderId, string text, out ChatIntent intent)
        {
            Prune();
            string wanted = (text ?? "").Trim();

            for (int i = 0; i < _items.Count; i++)
            {
                var it = _items[i];
                if (it.sender != senderId) continue;
                if (!string.Equals(it.text, wanted, StringComparison.OrdinalIgnoreCase)) continue;

                intent = it.intent;
                _items.RemoveAt(i);
                return true;
            }

            intent = null;
            return false;
        }

        public static void Clear() => _items.Clear();

        private static void Prune() => _items.RemoveAll(i => Time.time - i.time > Ttl);
    }
}
