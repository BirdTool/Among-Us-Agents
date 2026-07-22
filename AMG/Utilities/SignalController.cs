using AMG.AI.Mind;
using AMG.AI.Navigation;
using UnityEngine;

namespace AMG.Utilities
{
    public static class SignalController
    {
        // Everyone
        public static void SendSignal(Enums.SignalsEnum signal)
        {
            var brains = Utils.GetAllBrains();
            foreach (var brain in brains)
            {
                brain.SignalReceive(signal);
            }
        }

        // Specific player
        public static void SendSignal(PlayerControl player, Enums.SignalsEnum signal)
        {
            player.GetComponent<AgentBrain>()?.SignalReceive(signal);
        }

        // All players nearby
        public static void SendSignal(Vector2 waypoint, float radius, Enums.SignalsEnum signal)
        {
            var brains = Utils.GetAllBrains();
            foreach (var brain in brains)
            {
                var position = brain.Vector2Position;

                var straightDistance = Pathfinder.GetStraightDistance(waypoint, position);

                if (straightDistance <= radius)
                {
                    brain.SignalReceive(signal);
                }
            }
        }
    }
}