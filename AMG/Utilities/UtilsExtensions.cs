using AMG.AI.Navigation;
using UnityEngine;

namespace AMG.Utilities
{
    public static class UtilsExtensions
    {
        public static Waypoint GetClosestNode(this Vector2 position)
        {
            return Pathfinder.GetClosestNode(position);
        }
    }
}
