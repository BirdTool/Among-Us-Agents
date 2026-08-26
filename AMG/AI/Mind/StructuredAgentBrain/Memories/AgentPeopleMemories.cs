using System.Collections.Generic;
using AMG.Models;
using AmongUs.GameOptions;

namespace AMG.AI.Mind.Memories
{
    public class AgentPeopleMemories(byte playerId)
    {
        public byte PlayerId { get; private set; } = playerId;
        public float SuspiciusPercentage { get; private set; } = 0;
        public Dictionary<RoleTypes, float> ProbablyRole = [];

        public bool SawKilling = false;
        public bool SawVenting = false;

        public void IncreaseSuspiciusPercentage(float value)
        {
            SuspiciusPercentage += value;
            if (SuspiciusPercentage > 100f) SuspiciusPercentage = 100f;
            if (SuspiciusPercentage < 0f) SuspiciusPercentage = 0f;
        }

        public void DecreaseSuspiciusPercentage(float value)
        {
            SuspiciusPercentage -= value;
            if (SuspiciusPercentage < 0f) SuspiciusPercentage = 0f;
            if (SuspiciusPercentage > 100f) SuspiciusPercentage = 100f;
        }
    }
}