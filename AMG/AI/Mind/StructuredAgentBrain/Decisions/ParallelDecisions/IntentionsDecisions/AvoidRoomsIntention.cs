using System.Collections.Generic;
using System.Linq;
using AMG.Interfaces;
using AMG.Utilities;
using Rewired.Utils.Classes.Data;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain.Decisions.ParallelDecisions.IntentionsDecisions
{
    public class AvoidRoomsIntention : IParallelDecision
    {
        private const float CheckTimer = 3f;
        private readonly Dictionary<byte, float> _lastCheckTimer = [];
        private const float AvoidWindow = 15f;

        public void Evaluate(StructuredAgentBrain brain)
        {
            if (brain.IsDead || !brain.Intentions.AvoidRooms.Any()) return;
            if (brain.CurrentPath == null || !brain.CurrentPath.Any()) return;

            if (_lastCheckTimer.ContainsKey(brain.AgentId) && Time.time - _lastCheckTimer[brain.AgentId] < CheckTimer) return;
            _lastCheckTimer[brain.AgentId] = Time.time;

            var path = brain.CurrentPath;
            var rooms = brain.Intentions.AvoidRooms;

            var waypointsInTheRoom = path.Where(w =>
            {
                foreach (var room in rooms)
                {
                    if (w.Room == room) return true;
                }
                return false;
            }).ToList();

            if (!waypointsInTheRoom.Any()) return;

            float willAvoid = 1f;

            var localTask = brain.currentLocalTask;
            if (localTask != null)
            {
                if (brain.RemainingTasks < 2) willAvoid -= 22f;
                var taskLocation = localTask.Position.GetClosestNode();
                if (taskLocation != null)
                {
                    foreach (var room in rooms)
                    {
                        if (taskLocation.Room == room)
                        {
                            willAvoid += 30f;
                            break;
                        }
                    }
                }
            }

            if (brain.Intentions.CompleteTaskAttention > 10) willAvoid -= 15f;
            if (brain.currentSabotageStep != null) willAvoid -= 50f;

            foreach (var room in rooms)
            {
                var playersInTheRoom = Utils.Players.GetAllAlivePlayersInARoom(room);
                foreach (var player in playersInTheRoom)
                {
                    var memories = brain.GetOrCreateMemory(player.PlayerId);
                    if (memories.SuspiciusPercentage > 10) willAvoid += 25;

                    if (memories.Trust > 70) willAvoid -= 15;
                }
            }

            var isGoingToAvoid = Utils.ExecuteProbabilityAs100(willAvoid);
            if (isGoingToAvoid)
            {
                brain.AvoidPathsUntil = Time.time + AvoidWindow;
                brain.SetState(Enums.AgentEnums.AgentState.Calculating);
            }
        }
    }
}