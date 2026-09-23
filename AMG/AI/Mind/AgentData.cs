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
