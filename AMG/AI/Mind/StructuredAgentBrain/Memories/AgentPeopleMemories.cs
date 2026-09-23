using System.Collections.Generic;
using AMG.AI.Navigation;
using AMG.Models;
using AmongUs.GameOptions;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain.Memories
{
    public class AgentPeopleMemories(byte playerId)
    {
        public byte PlayerId { get; private set; } = playerId;
        public float SuspiciusPercentage { get; private set; } = 0;
        public Dictionary<RoleTypes, float> ProbablyRole = [];

        public float LastSeenTime { get; private set; } = 0f; // Use SecondsSinceShipStart
        public Vector2 LastKnownPosition { get; private set; } = Vector2.zero;

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

        public void UpdateLastSeen(float time, Vector2 position)
        {
            LastSeenTime = time;
            LastKnownPosition = position;
        }

        public void UpdateLastSeen(float time, Waypoint waypoint)
        {
            LastSeenTime = time;
            LastKnownPosition = waypoint.Position;
        }

        public void UpdateLastSeen(float time, Vector3 position)
        {
            LastSeenTime = time;
            LastKnownPosition = position;
        }
    }
}