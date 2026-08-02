using System.Collections.Generic;
using System.Linq;
using AMG.AI.Mind.Memories;
using AMG.Utilities;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private readonly Dictionary<byte, AgentPeopleMemories> PeopleMemories = [];
        public readonly List<RoundDeadBody> bodiesSeenDead = [];

        public AgentPeopleMemories GetOrCreateMemory(byte playerId)
        {
            var memory = PeopleMemories.GetValueOrDefault(playerId);
            if (memory != null) return memory;
            memory = new AgentPeopleMemories(playerId);
            PeopleMemories.Add(playerId, memory);
            return memory;
        }

        public List<AgentPeopleMemories> GetMemories() => [.. PeopleMemories.Values];

        public void IncreaseSuspiciusPercentage(byte playerId, float value)
        {
            var memory = GetOrCreateMemory(playerId);
            memory.IncreaseSuspiciusPercentage(value);
        }

        public void DecreaseSuspiciusPercentage(byte playerId, float value)
        {
            var memory = GetOrCreateMemory(playerId);
            memory.DecreaseSuspiciusPercentage(value);
        }
    }
}
