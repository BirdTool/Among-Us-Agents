using System;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.Interfaces;

namespace AMG.Models.Plans
{
    public class CheckPlan(Func<bool> check, Func<bool> actionAfterCheckIsFalse = null) : IPlan
    {
        public string Name { get; set; } = "CheckPlan";
        public bool IsDone { get; set; } = false;

        private readonly Func<bool> _check = check;
        public readonly Func<bool> _actionAfterCheckIsFalse = actionAfterCheckIsFalse;

        public void Execute(StructuredAgentBrain brain)
        {
            return;
        }

        public bool CheckIfPassed()
        {
            return _check();
        }
    }
}