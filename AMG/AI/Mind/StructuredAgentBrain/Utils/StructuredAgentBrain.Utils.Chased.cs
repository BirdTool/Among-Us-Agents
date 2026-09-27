using System.Collections.Generic;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        public List<AgentChasedData> ChasedData { get; set; } = [];
    }

    public enum FollowReason
    {
        None,
        MovingTogether,
        IdleNearby
    }

    public class AgentChasedData(PlayerControl chasedBy)
    {
        public PlayerControl ChasedBy { get; } = chasedBy;
        public float TimeChased { get; private set; } = 0f;
        public FollowReason Reason { get; private set; } = FollowReason.None;

        public float LastUpdateTime { get; private set; } = 0f;

        public void Update(float deltaTime, FollowReason reason, float currentTime)
        {
            TimeChased += deltaTime;
            Reason = reason;
            LastUpdateTime = currentTime;
        }
    }
}