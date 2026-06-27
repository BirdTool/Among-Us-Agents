using System.Collections.Generic;

namespace AMG.AI.Tools
{
    public static class TaskTracker
    {
        public static readonly Dictionary<uint, byte> AgentTaskIds = [];
        public static readonly HashSet<uint> PlayerOwnTaskIds = [];
        public static bool SuppressNextBanner = false;

        public static bool IsAgentTask(uint taskId) => AgentTaskIds.ContainsKey(taskId);

        public static void Clear()
        {
            AgentTaskIds.Clear();
            PlayerOwnTaskIds.Clear();
            SuppressNextBanner = false;
        }
    }
}
