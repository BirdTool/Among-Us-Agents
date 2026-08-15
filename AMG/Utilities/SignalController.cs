using System.Collections.Generic;
using AMG.AI.Mind;
using AMG.AI.Navigation;
using AMG.Interfaces;
using UnityEngine;

namespace AMG.Utilities
{
    public static class SignalController
    {
        // Everyone
        public static void SendSignalEveryone(ISignalClass signal)
        {
            var brains = Utils.GetAllBrains();
            foreach (var brain in brains)
            {
                brain.SignalReceive(signal);
            }
        }

        // Specific player
        public static void SendSignalToAgent(ISignalClass signal, PlayerControl player)
        {
            player.GetComponent<AgentBrain>()?.SignalReceive(signal);
        }

        // All players nearby
        public static void SendSignalRadiusVector2(ISignalClass signal, Vector2 waypoint, float radius)
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

        // All players that can see the target position
        public static void SendSignalRadiusCanSee(ISignalClass signal, Vector2 position)
        {
            var brains = Utils.GetAllBrains();
            foreach (var brain in brains)
            {
                var brainPosition = brain.Vector2Position;

                var canSee = Utils.CanSeeTheTarget(brainPosition, position, 6f);

                if (canSee)
                {
                    brain.SignalReceive(signal);
                }
            }
        }
    }
}