using System.Collections.Generic;
using System.Linq;
using AMG.AI.Navigation;
using AMG.AI.Perception;
using AMG.AI.Tools;
using AMG.Enums.AgentEnums;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        public float FollowStartedAt { get; private set; }
        public float FollowTargetLostFor => _hasSnapshot ? Time.time - _lastSeenTime : float.MaxValue;

        public PlayerControl PlayerToFollow = null;

        private const float RecheckInterval = 0.2f;
        private const float FollowDistance = 1.5f;
        private const float ResumeDistance = 2.0f;
        private const float MaxPathDrift = 6f;
        private const float LostGiveUpTime = 20f;
        private const float ArriveRadius = 1.5f;
        private const float LostGraceTime = 1.5f;

        private float _lastCheckTime = 0f;
        private bool _IsLookingForPlayer = false;

        private PlayerControl _snapshotOwner = null;
        private bool _hasSnapshot = false;
        private Vector2 _lastSeenPos;
        private Vector2 _lastSeenDir;
        private float _lastSeenTime;

        private Vector2 _pathGoal;
        private Waypoint _predictedTarget = null;
        private readonly HashSet<SystemTypes> _checkedRooms = [];

        private bool HasActivePath => CurrentPath != null && CurrentPathIndex < CurrentPath.Count;

        private void UpdateFollowingPlayer()
        {
            if (PlayerToFollow == null || PlayerToFollow.Data.IsDead || PlayerToFollow.Data.Disconnected)
            {
                StopFollowing();
                return;
            }

            if (_snapshotOwner != PlayerToFollow) SeedSnapshot();

            bool checkTick = Time.time - _lastCheckTime >= RecheckInterval;
            if (!checkTick)
            {
                AdvancePath();
                return;
            }
            _lastCheckTime = Time.time;

            bool visible = CanSee(PlayerToFollow); 
            Vector2 myPos = Agent.transform.position;
            Vector2 playerPos = PlayerToFollow.transform.position;

            if (visible)
            {
                RegisterSighting(playerPos);

                if (_IsLookingForPlayer)
                {
                    RemoveNameTag(DefaultTags.Thoughts.LookingForPlayer);
                    _IsLookingForPlayer = false;
                    ResetPath();
                }

                FollowVisiblePlayer(myPos, playerPos);
                return;
            }

            if (_IsLookingForPlayer)
            {
                if (!HasActivePath) LookForPlayer();
                else AdvancePath();
                return;
            }

            if (_hasSnapshot && Time.time - _lastSeenTime <= LostGraceTime)
            {
                FollowVisiblePlayer(myPos, playerPos);
                return;
            }

            FollowLostPlayer(myPos);
        }

        private void FollowVisiblePlayer(Vector2 myPos, Vector2 playerPos)
        {
            if (Utils.CanWalkToTarget(myPos, playerPos, 35))
            {
                float dist = Vector2.Distance(myPos, playerPos);
                float stopAt = HasActivePath ? FollowDistance : ResumeDistance;

                if (dist <= stopAt)
                {
                    ResetPath();
                    return;
                }

                Vector2 target = playerPos - (playerPos - myPos).normalized * FollowDistance;
                var straight = Pathfinder.FindStraightPath(myPos, target, out _, true);

                if (straight != null && straight.Count > 0)
                {
                    CurrentPath = straight;
                    CurrentPathIndex = 0;
                    _pathGoal = playerPos;
                    ProcessPathMovement();
                    return;
                }
            }

            MoveTo(playerPos, trimEnd: true);
        }

        private void FollowLostPlayer(Vector2 myPos)
        {
            float lost = Time.time - _lastSeenTime;

            if (!_hasSnapshot || lost > LostGiveUpTime)
            {
                LookForPlayer();
                return;
            }

            Vector2 goal = _predictedTarget != null ? _predictedTarget.Position : _lastSeenPos;

            if (Vector2.Distance(myPos, goal) <= ArriveRadius)
            {
                if (_predictedTarget != null && _predictedTarget.Room != SystemTypes.Hallway)
                    _checkedRooms.Add(_predictedTarget.Room);

                var next = ExtrapolateAlongDirection(lost);
                if (next == null || Vector2.Distance(myPos, next.Position) <= ArriveRadius)
                    next = PickPredictedTarget(lost);

                if (next == null)
                {
                    ResetPath();
                    return;
                }

                _predictedTarget = next;
                ResetPath();
                goal = next.Position;
            }

            MoveTo(goal, trimEnd: false);
        }

        private Waypoint ExtrapolateAlongDirection(float lostTime)
        {
            if (_lastSeenDir == Vector2.zero) return null;

            float speed = PlayerToFollow.MyPhysics.Speed * 1.75f;
            float maxTravel = Mathf.Clamp(speed * lostTime, 2f, 30f);

            var start = _lastSeenPos.GetClosestNode();
            if (start == null) return null;

            var current = start;
            Vector2 heading = _lastSeenDir;
            float travelled = 0f;
            var visited = new HashSet<Waypoint> { current };

            while (travelled < maxTravel)
            {
                Waypoint best = null;
                float bestDot = 0.3f;
                float bestLen = 0f;
                Vector2 bestDir = Vector2.zero;

                foreach (var n in current.Neighbors)
                {
                    if (n == null || visited.Contains(n)) continue;
                    if (n.Room != current.Room && (Utils.IsRoomClosed(n.Room) || Utils.IsRoomClosed(current.Room))) continue;

                    Vector2 edge = n.Position - current.Position;
                    float len = edge.magnitude;
                    if (len < 0.0001f) continue;

                    Vector2 dir = edge / len;
                    float dot = Vector2.Dot(heading, dir);
                    if (dot > bestDot)
                    {
                        bestDot = dot; best = n; bestLen = len; bestDir = dir;
                    }
                }

                if (best == null) break;

                visited.Add(best);
                travelled += bestLen;
                heading = bestDir;
                current = best;
            }

            return current == start ? null : current;
        }

        private Waypoint PickPredictedTarget(float elapsed)
        {
            var lastWaypoint = _lastSeenPos.GetClosestNode();
            float speed = PlayerToFollow.MyPhysics.Speed * 1.75f;

            var predictions = PlayerRoutePredictor.Predict(lastWaypoint, _lastSeenDir, speed, elapsed);
            foreach (var p in predictions)
            {
                if (!_checkedRooms.Contains(p.Room)) return p.ClosestWaypoint;
            }
            return null;
        }

        private void RegisterSighting(Vector2 pos)
        {
            if (_hasSnapshot)
            {
                Vector2 delta = pos - _lastSeenPos;
                if (delta.sqrMagnitude > 0.05f * 0.05f) _lastSeenDir = delta.normalized;
            }

            _lastSeenPos = pos;
            _lastSeenTime = Time.time;
            _hasSnapshot = true;

            _predictedTarget = null;
            _checkedRooms.Clear();
        }

        private void SeedSnapshot()
        {
            _snapshotOwner = PlayerToFollow;
            _hasSnapshot = false;
            _lastSeenDir = Vector2.zero;
            _predictedTarget = null;
            _checkedRooms.Clear();

            var memory = GetOrCreateMemory(PlayerToFollow.PlayerId);
            if (memory.LastKnownPosition == Vector2.zero) return;

            float age = Mathf.Max(0f, (Utils.SecondsSinceShipStart ?? 0f) - memory.LastSeenTime);
            _lastSeenPos = memory.LastKnownPosition;
            _lastSeenTime = Time.time - age;
            _hasSnapshot = true;
        }

        private void AdvancePath()
        {
            if (HasActivePath && ProcessPathMovement() != false) ResetPath();
        }

        private void MoveTo(Vector2 goal, bool trimEnd)
        {
            if (HasActivePath && Vector2.Distance(goal, _pathGoal) <= MaxPathDrift)
            {
                AdvancePath();
                return;
            }

            var path = Pathfinder.FindPath(WaypointPosition, goal.GetClosestNode(), out _);
            if (path == null || path.Count == 0)
            {
                LookForPlayer();
                return;
            }

            if (trimEnd && path.Count > 4) path.RemoveRange(path.Count - 4, 4);

            _pathGoal = goal;
            base.CommandGoToPath(path);
        }

        private void LookForPlayer()
        {
            _IsLookingForPlayer = true;
            AddNameTag(DefaultTags.Thoughts.LookingForPlayer);

            var randomPosition = WaypointManager.AllWaypoints.GetRandomItemSecureOrDefault();
            var path = Pathfinder.FindPath(WaypointPosition, randomPosition, out float _);

            if (path != null && path.Count > 0) base.CommandGoToPath(path);
        }

        public void StartFollowing(PlayerControl target)
        {
            PlayerToFollow = target;
            FollowStartedAt = Time.time;
            _IsLookingForPlayer = false;
            _snapshotOwner = target;
            _hasSnapshot = false;
            _lastSeenDir = Vector2.zero;
            _predictedTarget = null;
            _checkedRooms.Clear();
            RegisterSighting(target.transform.position);
            _lastCheckTime = 0f;
            ResetPath();
            SetState(AgentState.FollowingPlayer);
        }

        public void StopFollowing(bool changeState = true)
        {
            if (_IsLookingForPlayer)
            {
                RemoveNameTag(DefaultTags.Thoughts.LookingForPlayer);
                _IsLookingForPlayer = false;
            }

            PlayerToFollow = null;
            _snapshotOwner = null;
            _hasSnapshot = false;
            _predictedTarget = null;
            _checkedRooms.Clear();

            if (!changeState) return;
            ResetPath();
            SetState(AgentState.Calculating);
        }
    }
}