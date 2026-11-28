using System.Collections.Generic;
using AMG.AI.Control.AgentController;
using AMG.AI.Mind;
using AMG.AI.Mind.LLMAgentBrain;
using AMG.AI.Mind.StructuredAgentBrain;
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
            var controllers = Utils.GetAllAgentController();
            foreach (var controller in controllers)
            {
                switch (controller)
                {
                    case StructuredAgentBrain brain:
                        brain.SignalReceive(signal);
                        break;
                    case LLMAgentBrain brain:
                        brain.SignalReceive(signal);
                        break;
                }
            }
        }

        // Specific player
        public static void SendSignalToAgent(ISignalClass signal, PlayerControl player)
        {

            var controller = player.GetComponent<AgentController>();
            switch (controller)
            {
                case StructuredAgentBrain brain:
                    brain.SignalReceive(signal);
                    break;
                case LLMAgentBrain brain:
                    brain.SignalReceive(signal);
                    break;
            }
        }

        // All players nearby
        public static void SendSignalRadiusVector2(ISignalClass signal, Vector2 waypoint, float radius, PlayerControl excludePlayer = null)
        {
            var controllers = Utils.GetAllAgentController();
            foreach (var controller in controllers)
            {
                var position = controller.Vector2Position;

                if(controller.AgentId == excludePlayer?.PlayerId) continue;

                var straightDistance = Pathfinder.GetStraightDistance(waypoint, position);

                if (straightDistance <= radius)
                {
                    switch (controller)
                    {
                        case StructuredAgentBrain brain:
                            brain.SignalReceive(signal);
                            break;
                        case LLMAgentBrain brain:
                            brain.SignalReceive(signal);
                            break;
                    }
                }
            }
        }

        // All players that can see the target position
        public static void SendSignalRadiusCanSee(ISignalClass signal, Vector2 position, PlayerControl excludePlayer = null)
        {
            var controllers = Utils.GetAllAgentController();
            foreach (var controller in controllers)
            {
                var brainPosition = controller.Vector2Position;

                if(controller.AgentId == excludePlayer?.PlayerId) continue;

                var canSee = Utils.CanSeeTheTarget(brainPosition, position, 6f);

                if (canSee)
                {
                    switch (controller)
                    {
                        case StructuredAgentBrain brain:
                            brain.SignalReceive(signal);
                            break;
                        case LLMAgentBrain brain:
                            brain.SignalReceive(signal);
                            break;
                    }
                }
            }
        }
    }
}