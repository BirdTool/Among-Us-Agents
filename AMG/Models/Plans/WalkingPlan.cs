using System.Collections.Generic;
using System.Linq;
using AMG.AI.Mind;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Navigation;
using AMG.Enums;
using AMG.Interfaces;
using AMG.Utilities;
using UnityEngine;

namespace AMG.Models.Plans
{
    public class WalkingPlan(List<Waypoint> path) : IPlan
    {
        public bool IsDone { get; set; } = false;
        public List<Waypoint> Path { get; private set; } = path;
        public bool IsRunning { get; set; } = false;

        public void Execute(StructuredAgentBrain brain)
        {
            if (Path.Count == 0)
            {
                IsDone = true;
                return;
            }

            if (IsDone || IsRunning) return;

            if (!brain.CurrentPath.SequenceEqual(Path))
            {
                IsRunning = true;
                brain.OnReachedTheCurrentPath.Add(() => 
                {
                    IsDone = true;
                    return true;
                });

                brain.CommandGoToPath(Path);
            }
        }
    }
}
