using System.Collections.Generic;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        public List<AgentChasedData> ChasedData { get; set; } = [];
    }

    public class AgentChasedData(PlayerControl chasedBy)
    {
        public PlayerControl ChasedBy { get; } = chasedBy;
        public float TimeChased { get; set; } = 0f;

        public void Update(float time)
        {
            TimeChased += time;
        }
    }
}