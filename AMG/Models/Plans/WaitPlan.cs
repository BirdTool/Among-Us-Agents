using System;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.Interfaces;
using UnityEngine;

namespace AMG.Models.Plans
{
    public class WaitTimePlan(float seconds) : IPlan
    {
        private readonly float _seconds = seconds;
        public string Name { get; set; } = "WaitTimePlan";
        public bool IsDone { get; set; } = false;
        public float? StartedAt { get; private set; } = null;
        private float EndsAt { get; set; } = 0f;

        public void Execute(StructuredAgentBrain brain)
        {
            if (StartedAt == null)
            {
                StartedAt = Time.time;
                EndsAt = StartedAt.Value + _seconds;
            }
            IsDone = Time.time >= EndsAt;
        }
    }

    public class WaitConditionPlan(Func<bool> condition) : IPlan
    {
        private readonly Func<bool> _condition = condition;
        public string Name { get; set; } = "WaitConditionPlan";
        public bool IsDone { get; set; } = false;
        public float? StartedAt { get; private set; } = null;

        public void Execute(StructuredAgentBrain brain)
        {
            StartedAt ??= Time.time;
            IsDone = _condition();
        }
    }
}