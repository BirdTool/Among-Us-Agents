using System.Collections.Generic;
using System.Linq;

namespace AMG.AI.Services.ChatService.Llm
{
    // Messages between the agents of one player (Communicator, Detective, Assistence). It lives as long as the player's
    // LlmChatService. Every recipient has its own read cursor, so an item reaches its recipient once.
    public sealed class AgentMailbox
    {
        public sealed record Item(int Id, string From, string To, string Kind, string Text, int Round);

        private const int MaxItems = 200;

        private readonly List<Item> _items = [];
        private readonly Dictionary<string, int> _readUpTo = [];
        private int _lastId;

        public void Clear()
        {
            _items.Clear();
            _readUpTo.Clear();
            _lastId = 0;
        }

        // The last "count" items sent to or by "name", oldest first. IsNew: "name" has not been handed it yet.
        public List<(Item Item, bool IsNew)> Recent(string name, int count)
        {
            int cursor = _readUpTo.TryGetValue(name, out var id) ? id : 0;
            return [.. _items
                .Where(i => i.To == name || i.From == name)
                .TakeLast(count)
                .Select(i => (i, i.To == name && i.Id > cursor))];
        }

        public Item Post(string from, string to, string kind, string text, int round)
        {
            var item = new Item(++_lastId, from, to, kind, text, round);
            _items.Add(item);
            if (_items.Count > MaxItems) _items.RemoveAt(0);
            return item;
        }

        public List<Item> Unread(string to)
        {
            int cursor = _readUpTo.TryGetValue(to, out var id) ? id : 0;
            return [.. _items.Where(i => i.To == to && i.Id > cursor)];
        }

        // Everything up to "lastDelivered" (inclusive) is read by "to".
        public void MarkRead(string to, Item lastDelivered) => _readUpTo[to] = lastDelivered.Id;
    }
}
