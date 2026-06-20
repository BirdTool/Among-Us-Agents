using AMG.Enums;
using AMG.Models;

namespace AMG.AI.Tools
{
    public static partial class DefaultTags
    {
        public static class States
        {
            public static AgentTag Wandering => new()
            {
                Tag = "Wandering",
                ColourHex = "#00FFFF",
                Size = "80%",
                Identifier = IdentifierEnum.State
            };

            public static AgentTag Stopped => new()
            {
                Tag = "Stopped",
                ColourHex = "#be0000",
                Size = "80%",
                Identifier = IdentifierEnum.State
            };

            public static AgentTag Navigating => new()
            {
                Tag = "Navigating",
                ColourHex = "#00FF00",
                Size = "80%",
                Identifier = IdentifierEnum.State
            };

            public static AgentTag SmartWandering => new()
            {
                Tag = "Wandering",
                ColourHex = "#ff57df",
                Size = "80%",
                Identifier = IdentifierEnum.State
            };

            public static AgentTag DoingTask => new()
            {
                Tag = "Doing Task",
                ColourHex = "#fd7200",
                Size = "80%",
                Identifier = IdentifierEnum.State
            };

            public static AgentTag Calculating => new()
            {
                Tag = "Calculating",
                ColourHex = "#ff3636",
                Size = "80%",
                Identifier = IdentifierEnum.Think
            };

        }

        public static class Emotions
        {
            public static AgentTag Neutral => new()
            {
                Tag = "Neutral",
                ColourHex = "#FFFFFF",
                Size = "50%",
                Identifier = IdentifierEnum.Emotion
            };

            public static AgentTag Happy => new()
            {
                Tag = "Happy",
                ColourHex = "#FFD700",
                Size = "50%",
                Identifier = IdentifierEnum.Emotion
            };

            public static AgentTag Sad => new()
            {
                Tag = "Sad",
                ColourHex = "#1E90FF",
                Size = "50%",
                Identifier = IdentifierEnum.Emotion
            };

            public static AgentTag Angry => new()
            {
                Tag = "Angry",
                ColourHex = "#FF0000",
                Size = "50%",
                Identifier = IdentifierEnum.Emotion
            };

            public static AgentTag Scared => new()
            {
                Tag = "Scared",
                ColourHex = "#8A2BE2",
                Size = "50%",
                Identifier = IdentifierEnum.Emotion
            };

            public static AgentTag Confused => new()
            {
                Tag = "Confused",
                ColourHex = "#FF69B4",
                Size = "50%",
                Identifier = IdentifierEnum.Emotion
            };

            public static AgentTag Confident => new()
            {
                Tag = "Confident",
                ColourHex = "#32CD32",
                Size = "50%",
                Identifier = IdentifierEnum.Emotion
            };
        }

        public static class Thoughts
        {
            public static AgentTag Calculating => new()
            {
                Tag = "Calculating route...",
                ColourHex = "#00BFFF",
                Size = "65%",
                Identifier = IdentifierEnum.Think
            };

            public static AgentTag Stuck => new()
            {
                Tag = "Stuck!",
                ColourHex = "#DC143C",
                Size = "65%",
                Identifier = IdentifierEnum.Think
            };
        }
    }
}