using AMG.Enums.AgentEnums;
using AmongUs.GameOptions;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.AI.Mind
{
    public class AgentData
    {
        public string Name { get; set; }
        public AgentEmotion Emotion { get; set; } = AgentEmotion.Neutral;
        public AgentPersonality Personality { get; set; }
        public Dictionary<byte, AgentPlayerReminders> PlayersReminders { get; set; }
    }

    public class AgentPlayerReminders
    {
        public double Affinity { get; set; } = 0;
        public double Trust { get; set; } = 0;
        public double Suspicions { get; set;  } = 0;
        public Vector2? LastSeenPosition { get; set; } = null;
        public Time LastSeenTime { get; set; } = null;
        public RoleTypes? PossibleRole { get; set; } = null;
        public double RoleConfidence { get; set; } = 0;
    }

    public class AgentPersonality
    {
        public double Confident { get; set; } = 0;
        public double Curious { get; set; } = 0;
        public double Cautious { get; set; } = 0;
        public double Brave { get; set; } = 0;
        public double Cowardly { get; set; } = 0;
    }
}
