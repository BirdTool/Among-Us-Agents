using System.Collections.Generic;
using AMG.Utilities;
using AmongUs.GameOptions;
using UnityEngine;

namespace AMG.Utilities
{
    public static class AgentPerception
    {
        private const int WallMask = 1 << 17;

        public static List<PlayerControl> GetNearbyPlayers(
            Vector2 origin,
            float radius,
            PlayerControl exclude = null,
            bool aliveOnly = true,
            bool checkObstruction = true)
        {
            float sqrRadius = radius * radius;
            var result = new List<PlayerControl>();

            var all = PlayerControl.AllPlayerControls;
            for (int i = 0; i < all.Count; i++)
            {
                var p = all[i];
                if (p == null || p == exclude) continue;
                if (aliveOnly && (p.Data == null || p.Data.IsDead || p.Data.Disconnected)) continue;

                Vector2 pos = p.GetTruePosition();
                float sqrDist = (pos - origin).sqrMagnitude;
                if (sqrDist > sqrRadius) continue;

                if (checkObstruction && IsObstructed(origin, pos)) continue;

                result.Add(p);
            }

            result.Sort((a, b) =>
                ((Vector2)a.GetTruePosition() - origin).sqrMagnitude
                .CompareTo(((Vector2)b.GetTruePosition() - origin).sqrMagnitude));

            return result;
        }

        public static List<RoundDeadBody> GetNearbyDeadBodies(
            Vector2 origin,
            float radius,
            bool checkObstruction = true)
        {
            float sqrRadius = radius * radius;
            var result = new List<RoundDeadBody>();

            var bodies = Utils.Round.CurrentRoundDeadBodies;
            if (bodies == null) return result;

            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (body.ReportedByPlayerId.HasValue) continue; // already reported, not on the map anymore

                Vector2 pos = body.Position;
                float sqrDist = (pos - origin).sqrMagnitude;
                if (sqrDist > sqrRadius) continue;

                if (checkObstruction && IsObstructed(origin, pos)) continue;

                result.Add(body);
            }

            return result;
        }

        public static List<PlayerControl> GetNearbyPlayersInVision(
            PlayerControl viewer,
            PlayerControl exclude = null,
            bool aliveOnly = true)
        {
            float visionRadius = GetEffectiveVisionRadius(viewer);
            return GetNearbyPlayers(
                viewer.GetTruePosition(),
                visionRadius,
                exclude ?? viewer,
                aliveOnly,
                checkObstruction: true);
        }

        public static List<RoundDeadBody> GetNearbyDeadBodiesInVision(PlayerControl viewer)
        {
            float visionRadius = GetEffectiveVisionRadius(viewer);
            return GetNearbyDeadBodies(viewer.GetTruePosition(), visionRadius, checkObstruction: true);
        }
        
        public static float GetEffectiveVisionRadius(PlayerControl viewer)
        {
            bool isImpostor = viewer.Data.Role != null && viewer.Data.Role.IsImpostor;

            float multiplier = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(
                isImpostor ? FloatOptionNames.ImpostorLightMod : FloatOptionNames.CrewLightMod);

            if (!isImpostor && IsLightsSabotaged())
            {
                multiplier = Mathf.Min(multiplier, 0.25f);
            }
            const float baseVisionRadius = 2.5f;
            return baseVisionRadius * multiplier;
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

        private static bool IsObstructed(Vector2 from, Vector2 to)
        {
            return PhysicsHelpers.AnythingBetween(from, to, WallMask, false);
        }
    }
}