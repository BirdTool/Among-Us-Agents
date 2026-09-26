using System.Collections.Generic;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        public AgentIntention Intentions { get; set; } = new();
    }

    public class AgentIntention
    {
        public List<byte> AvoidPlayers { get; set; } = [];
        public List<SystemTypes> AvoidRooms { get; set; } = [];
        public List<byte> FollowPlayers { get; set; } = [];
        public bool AvoidAllPlayers { get; set; } = false;
        public bool AvoidBeAlone { get; set; } = false;
        public float CompleteTaskAttention { get; set; } = 0f; // Negative: Avoid do task, Positive: Give more attention to do tasks
        public List<byte> PriorityKillTargets { get; set; } = [];
        public float FixSabotageDrive { get; set; } = 0f;

        public void Clear()
        {
            AvoidPlayers.Clear();
            AvoidRooms.Clear();
            FollowPlayers.Clear();
            AvoidAllPlayers = false;
            AvoidBeAlone = false;
            CompleteTaskAttention = 0f;
            PriorityKillTargets.Clear();
            FixSabotageDrive = 0f;
        }
    }
}