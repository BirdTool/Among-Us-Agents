using System.Linq;
using AMG.AI.Navigation;
using AMG.Interfaces;
using UnityEngine;

namespace AMG.Utilities
{
    public static class UtilsExtensions
    {
        public static Waypoint GetClosestNode(this Vector2 position)
        {
            return Pathfinder.GetClosestNode(position);
        }

        public static bool CompleteSabotage(this ISabotage sabotage, ShipStatus shipStatus)
        {
            if (!sabotage.IsAllStepsCompleted()) return false;
            
            Utils.Sabotages.RepairSabotages(shipStatus);

            return true;
        }

        public static bool IsAllStepsCompleted(this ISabotage sabotage)
        {
            var steps = sabotage.GetSteps();
            return steps.All(s => s.IsCompleted);
        }
    }
}
