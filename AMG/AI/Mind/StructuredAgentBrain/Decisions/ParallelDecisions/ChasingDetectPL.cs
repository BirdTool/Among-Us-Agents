using System.Collections.Generic;
using System.Linq;
using AMG.AI.Navigation;
using AMG.AI.Tools;
using AMG.Enums.AgentEnums;
using AMG.Interfaces;
using AMG.Utilities;
using AMG.Utilities.MapUtils.TasksUtils;
using AmongUs.GameOptions;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain.Decisions.ParallelDecisions
{
    internal class ChasingDetectPL : IParallelDecision
    {
        private const float CloseDistanceThreshold = 3.3f;
        private const float MovementSpeedThreshold = 0.35f;
        private const float IdleNearbyDistanceThreshold = 4f;

        private const float MovingTogetherConfirmTime = 6f;
        private const float IdleNearbyConfirmTime = 5.5f;
        private const float DecayMultiplier = 2f;

        private const float ForgetTrackingAfterSeconds = 3f;
        private const float ForgetChasedDataAfterSeconds = 8f;

        private class TrackingData
        {
            public Vector2 LastPosition;
            public float LastUpdateTime;
            public float MovingTogetherTime;
            public float IdleNearbyTime;
        }

        private readonly Dictionary<(byte observerId, byte targetId), TrackingData> _tracking = [];

        public void Evaluate(StructuredAgentBrain brain)
        {
            float now = Utils.SecondsSinceShipStart.Value;
            var nearby = brain.NearbyPlayersInVision;
            byte observerId = brain.AgentId;
            var seenThisFrame = new HashSet<byte>();

            Vector2 selfPos = brain.transform.position;
            bool selfMoving = brain.DesiredVelocity.magnitude > MovementSpeedThreshold;
            bool selfDoingTask = brain.currentState == AgentState.DoingTask;

            foreach (var pl in nearby)
            {
                if (pl.PlayerId == observerId) continue;
                seenThisFrame.Add(pl.PlayerId);

                var key = (observerId, pl.PlayerId);

                Vector2 targetPos = pl.transform.position;
                float distance = Vector2.Distance(selfPos, targetPos);

                if (!_tracking.TryGetValue(key, out var data))
                {
                    _tracking[key] = new TrackingData { LastPosition = targetPos, LastUpdateTime = now };
                    continue;
                }

                float deltaTime = Mathf.Clamp(now - data.LastUpdateTime, 0f, 1f);
                if (deltaTime <= 0f) continue;

                float targetSpeed = Vector2.Distance(targetPos, data.LastPosition) / deltaTime;
                bool targetMoving = targetSpeed > MovementSpeedThreshold;

                if (IsTrappedTogether(brain, pl))
                {
                    data.MovingTogetherTime = 0f;
                    data.IdleNearbyTime = 0f;
                }
                else
                {
                    if (selfMoving && targetMoving && distance <= CloseDistanceThreshold)
                        data.MovingTogetherTime += deltaTime;
                    else
                        data.MovingTogetherTime = Mathf.Max(0f, data.MovingTogetherTime - deltaTime * DecayMultiplier);

                    if (selfDoingTask && !targetMoving && distance <= IdleNearbyDistanceThreshold && !TaskConsoleRegistry.IsNearAnyTask(targetPos))
                        data.IdleNearbyTime += deltaTime;
                    else
                        data.IdleNearbyTime = Mathf.Max(0f, data.IdleNearbyTime - deltaTime * DecayMultiplier);
                }

                data.LastPosition = targetPos;
                data.LastUpdateTime = now;

                if (data.MovingTogetherTime >= MovingTogetherConfirmTime)
                    RegisterChasing(brain, pl, deltaTime, FollowReason.MovingTogether, now);
                else if (data.IdleNearbyTime >= IdleNearbyConfirmTime)
                    RegisterChasing(brain, pl, deltaTime, FollowReason.IdleNearby, now);
            }

            CleanupStaleTracking(observerId, seenThisFrame, now);
            brain.ChasedData.RemoveAll(c => now - c.LastUpdateTime > ForgetChasedDataAfterSeconds);
        }

        private void RegisterChasing(StructuredAgentBrain brain, PlayerControl chaser, float deltaTime, FollowReason reason, float now)
        {
            var existing = brain.ChasedData.Find(c => c.ChasedBy.PlayerId == chaser.PlayerId);
            if (existing == null)
            {
                existing = new AgentChasedData(chaser);
                brain.ChasedData.Add(existing);
            }
            existing.Update(deltaTime, reason, now);

            brain.GetOrCreateMemory(chaser.PlayerId).IncreaseSuspiciusPercentage(deltaTime * 1.5f);
            brain.AddNameTag(DefaultTags.Thoughts.AmBeingChased);
        }

        private void CleanupStaleTracking(byte observerId, HashSet<byte> seenThisFrame, float now)
        {
            List<(byte, byte)> toRemove = null;
            foreach (var kvp in _tracking)
            {
                if (kvp.Key.observerId != observerId) continue;
                if (seenThisFrame.Contains(kvp.Key.targetId)) continue;
                if (now - kvp.Value.LastUpdateTime > ForgetTrackingAfterSeconds)
                    (toRemove ??= []).Add(kvp.Key);
            }
            if (toRemove != null)
                foreach (var key in toRemove) _tracking.Remove(key);
        }

        private bool IsTrappedTogether(StructuredAgentBrain brain, PlayerControl other)
        {
            var selfRoom = Utils.GetPlayerRoom(brain.Agent);
            var otherRoom = Utils.GetPlayerRoom(other);
            if (selfRoom != otherRoom) return false;

            if (selfRoom == SystemTypes.Hallway)
            {
                var waypoint = Pathfinder.GetClosestNode(brain.transform.position);
                if (waypoint?.NeighborRooms == null || waypoint.NeighborRooms.Count == 0)
                    return false;

                foreach (var room in waypoint.NeighborRooms)
                    if (!Utils.IsRoomClosed(room))
                        return false;
                return true;
            }

            return Utils.IsRoomClosed(selfRoom);
        }
    }
}