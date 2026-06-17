using System;

namespace AMG.Models
{
    public class AgentUpdateAction(Func<bool> execute)
    {
        public bool ExecuteOnMeeting { get; set; }
        public bool DeleteOnMeeting { get; set; }
        public bool IsOnlyPredefinedAction { get; set; } = false;
        public Func<bool> Execute { get; set; } = execute;
    }
}
