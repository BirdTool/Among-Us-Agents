using System.Collections.Generic;

namespace AMG.AI.Services.ChatService.Local
{
    // Everybody who was in the match, even after leaving or dying: players keep being mentioned ("white left"),
    // so their names and colours must still be recognised (and masked) in later messages.
    // Reset when a new match starts.
    public static class ChatRoster
    {
        private static readonly Dictionary<byte, ChatPlayer> _seen = [];

        public static void Reset() => _seen.Clear();

        // Adds/updates the players currently in the match and returns everybody seen so far.
        public static List<ChatPlayer> Merge(IEnumerable<ChatPlayer> current)
        {
            foreach (var p in current) _seen[p.Id] = p;
            return [.. _seen.Values];
        }
    }
}
