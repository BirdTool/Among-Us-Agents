using System;
using System.Collections.Concurrent;

namespace AMG.Utilities
{
    public static class MainThreadDispatcher
    {
        private static readonly ConcurrentQueue<Action> _queue = new();

        public static void Enqueue(Action action) => _queue.Enqueue(action);

        public static void Drain(int maxPerFrame = 32)
        {
            int processed = 0;
            while (processed < maxPerFrame && _queue.TryDequeue(out var action))
            {
                try { action(); }
                catch (Exception e) { LogManager.LogError($"[MainThreadDispatcher] {e}"); }
                processed++;
            }
        }
    }
}