using System;
using AMG.Enums;
using UnityEngine;

namespace AMG.Models
{
    public class AgentTempParallelDecision(TempParallelDecisionsIDsEnum id, Func<bool> Execute)
    {
        public TempParallelDecisionsIDsEnum ID { get; } = id;
        public bool DeleteOnMeeting = false;
        public float SecondsTimeLimit = float.PositiveInfinity;
        public float StartedAt { get; private set; } = Time.time;

        public bool IsTimeLimitExpired => Time.time - StartedAt > SecondsTimeLimit;
        public Func<bool> Execute { get; } = Execute;
    }
}