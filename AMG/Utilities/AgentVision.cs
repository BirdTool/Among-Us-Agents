using System.Collections.Generic;
using AMG.AI.Navigation;
using AMG.Enums.GameEnums;
using AmongUs.GameOptions;
using UnityEngine;

namespace AMG.Utilities
{
    public static class AgentVision
    {
        private const float BaseVisionRadius = 2.5f;
        private const float LightsOffVisionRadius = 0.25f;

        private static readonly int VisionMask = (1 << (int)LayersEnum.Shadow) | (1 << (int)LayersEnum.IlluminatedBlocking);

        public static List<PlayerControl> GetNearbyPlayers(
            Vector2 origin,
            float radius,
            PlayerControl exclude = null,
            bool aliveOnly = true,
            bool checkObstruction = true)
        {
            var result = new List<PlayerControl>();

            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p == exclude) continue;
                if (aliveOnly && (p.Data == null || p.Data.IsDead || p.Data.Disconnected)) continue;

                Vector2 pos = p.GetTruePosition();
                if (Vector2.Distance(origin, pos) > radius) continue;

                if (checkObstruction && IsObstructed(origin, pos)) continue;

                result.Add(p);
            }

            SortByDistance(result, origin);
            return result;
        }

        public static List<RoundDeadBody> GetNearbyDeadBodies(
            Vector2 origin,
            float radius,
            bool checkObstruction = true)
        {
            var result = new List<RoundDeadBody>();
            var bodies = Utils.Round.CurrentRoundDeadBodies;
            if (bodies == null) return result;

            foreach (var body in bodies)
            {
                if (body.ReportedByPlayerId.HasValue) continue;

                Vector2 pos = body.Position;
                if (Vector2.Distance(origin, pos) > radius) continue;

                if (checkObstruction && IsObstructed(origin, pos)) continue;

                result.Add(body);
            }

            return result;
        }

        public static List<PlayerControl> GetNearbyPlayersInVision(
            Vector2 pointOfVision,
            bool considerLightsOff = true,
            float incrementDistance = 0f)
        {
            float visionRadius = BaseVisionRadius + incrementDistance;
            if (considerLightsOff && IsLightsSabotaged())
                visionRadius = Mathf.Min(visionRadius, LightsOffVisionRadius + incrementDistance);

            return GetPlayersWithinVision(pointOfVision, visionRadius, exclude: null);
        }

        public static List<PlayerControl> GetNearbyPlayersInVision(
            PlayerControl player,
            float incrementDistance = 0f)
        {
            float visionRadius = GetLocalVisionRadius(player) + incrementDistance;
            return GetPlayersWithinVision(player.GetTruePosition(), visionRadius, exclude: player);
        }

        public static List<RoundDeadBody> GetNearbyBodiesInVision(
            Vector2 pointOfVision,
            bool considerLightsOff = true,
            float incrementDistance = 0f)
        {
            float visionRadius = BaseVisionRadius + incrementDistance;
            if (considerLightsOff && IsLightsSabotaged())
                visionRadius = Mathf.Min(visionRadius, LightsOffVisionRadius + incrementDistance);

            return GetBodiesWithinVision(pointOfVision, visionRadius);
        }

        public static List<RoundDeadBody> GetNearbyBodiesInVision(
            PlayerControl player,
            float incrementDistance = 0f)
        {
            float visionRadius = GetLocalVisionRadius(player) + incrementDistance;
            return GetBodiesWithinVision(player.GetTruePosition(), visionRadius);
        }

        public readonly struct NearbyPlayerDistance(PlayerControl player, float distance, bool isVisible)
        {
            public readonly PlayerControl Player = player;
            public readonly float Distance = distance;
            public readonly bool IsVisible = isVisible;
        }

        public static List<NearbyPlayerDistance> GetNearbyPlayersByDistance(Vector2 pointOfVision, float distance)
        {
            var result = new List<NearbyPlayerDistance>();

            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p.Data == null || p.Data.IsDead || p.Data.Disconnected) continue;

                Vector2 pos = p.GetTruePosition();
                float straightDist = Vector2.Distance(pointOfVision, pos);
                if (straightDist > distance) continue; // fora do alcance já em linha reta, descarta sem gastar A*

                bool visible = !IsObstructed(pointOfVision, pos) && !IsBlockedByClosedDoor(pointOfVision, pos);

                if (visible)
                {
                    result.Add(new NearbyPlayerDistance(p, straightDist, true));
                    continue;
                }

                // bloqueado: pega a distância real de caminho em vez da linha reta
                var startNode = Pathfinder.GetClosestNode(pointOfVision);
                var endNode = Pathfinder.GetClosestNode(pos);
                var path = Pathfinder.FindPath(startNode, endNode, out float pathDistance);

                if (path != null)
                    result.Add(new NearbyPlayerDistance(p, pathDistance, false));
            }

            result.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            return result;
        }

        private static List<PlayerControl> GetPlayersWithinVision(Vector2 origin, float visionRadius, PlayerControl exclude)
        {
            var result = new List<PlayerControl>();

            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p == exclude) continue;
                if (p.Data == null || p.Data.IsDead || p.Data.Disconnected) continue;

                Vector2 pos = p.GetTruePosition();
                if (Vector2.Distance(origin, pos) > visionRadius) continue;

                if (!IsWithinScreenBounds(origin, pos)) continue;
                if (IsBlockedByClosedDoor(origin, pos)) continue;
                if (IsObstructed(origin, pos)) continue;

                result.Add(p);
            }

            SortByDistance(result, origin);
            return result;
        }

        private static List<RoundDeadBody> GetBodiesWithinVision(Vector2 origin, float visionRadius)
        {
            var result = new List<RoundDeadBody>();
            var bodies = Utils.Round.CurrentRoundDeadBodies;
            if (bodies == null) return result;

            foreach (var body in bodies)
            {
                if (body.ReportedByPlayerId.HasValue) continue;

                Vector2 pos = body.Position;
                if (Vector2.Distance(origin, pos) > visionRadius) continue;

                if (!IsWithinScreenBounds(origin, pos)) continue;
                if (IsBlockedByClosedDoor(origin, pos)) continue;
                if (IsObstructed(origin, pos)) continue;

                result.Add(body);
            }

            return result;
        }

        private static float GetLocalVisionRadius(PlayerControl player)
        {
            if (player.lightSource != null)
                return player.lightSource.viewDistance;

            return BaseVisionRadius;
        }

        private static bool IsLightsSabotaged()
        {
            if (ShipStatus.Instance == null) return false;
            if (ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Electrical, out var sys))
            {
                var switchSys = sys.TryCast<SwitchSystem>();
                return switchSys != null && switchSys.IsActive;
            }
            return false;
        }

        internal static bool IsWithinScreenBounds(Vector2 viewerPos, Vector2 targetPos)
        {
            if (Camera.main == null) return true;

            float halfHeight = Camera.main.orthographicSize;
            float halfWidth = halfHeight * Camera.main.aspect;

            Vector2 delta = targetPos - viewerPos;
            return Mathf.Abs(delta.x) <= halfWidth && Mathf.Abs(delta.y) <= halfHeight;
        }

        private static SystemTypes GetRoomAtPosition(Vector2 pos)
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllRooms == null) return SystemTypes.Hallway;

            foreach (var room in ShipStatus.Instance.AllRooms)
            {
                if (room.roomArea != null && room.roomArea.OverlapPoint(pos))
                    return room.RoomId;
            }

            return SystemTypes.Hallway;
        }

        internal static bool IsBlockedByClosedDoor(Vector2 origin, Vector2 target)
        {
            var originRoom = GetRoomAtPosition(origin);
            var targetRoom = GetRoomAtPosition(target);

            if (originRoom == targetRoom) return false;

            return Utils.IsRoomClosed(originRoom) || Utils.IsRoomClosed(targetRoom);
        }

        internal static bool IsObstructed(Vector2 from, Vector2 to)
        {
            Vector2 dir = (to - from).normalized;
            float dist = Vector2.Distance(from, to);

            var hits = Physics2D.RaycastAll(from, dir, dist, VisionMask);
            foreach (var h in hits)
            {
                if (h.collider != null && !h.collider.isTrigger) return true;
            }
            return false;
        }

        private static void SortByDistance(List<PlayerControl> list, Vector2 origin)
        {
            list.Sort((a, b) =>
                Vector2.Distance(origin, a.GetTruePosition())
                .CompareTo(Vector2.Distance(origin, b.GetTruePosition())));
        }
    }
}