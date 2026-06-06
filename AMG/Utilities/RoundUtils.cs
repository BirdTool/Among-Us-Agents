using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AMG.Utilities
{
    public static partial class Utils
    {
        public static class Round
        {
            private static readonly List<GameRound> Rounds = new();

            public static GameRound CurrentRound => Rounds.Count > 0 ? Rounds.Last() : null;
            public static List<RoundDeadBody> CurrentRoundDeadBodies => CurrentRound?.Bodies;

            public static void AddRound()
            {
                int roundNumber = Rounds.Count + 1;
                Rounds.Add(new GameRound { Number = roundNumber });
            }

            public static void AddDeadBody(RoundDeadBody body)
            {
                if (CurrentRound == null) return;
                CurrentRound.Bodies.Add(body);
            }

            public static void ClearRounds()
            {
                Rounds.Clear();
            }
        }
    }

    public class GameRound
    {
        public int Number;
        public List<RoundDeadBody> Bodies = [];
    }

    public class RoundDeadBody
    {
        public byte PlayerId { get; set; }
        public byte? ReportedByPlayerId { get; set; } = null;
        public float TimeOfDeath { get; set; } = Time.time;
        public float? TimeOfReport { get; set; } = null;
        public Vector2 Position { get; set; }

        public float TimeBetweenDeathAndReport => TimeOfReport.HasValue ? TimeOfReport.Value - TimeOfDeath : -1f;
        public float TimeSinceDeath => Time.time - TimeOfDeath;
    }
}