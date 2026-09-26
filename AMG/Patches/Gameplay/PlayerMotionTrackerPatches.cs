using HarmonyLib;
using AMG.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.Patches.Gameplay
{
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
    public static class PlayerMotionTrackerPatches
    {
        private static readonly Dictionary<int, PlayerMotionTracker> _players = [];

        private const float CleanupIntervalSeconds = 2f;
        private static float _lastCleanupTime;

        public static void Postfix(PlayerPhysics __instance)
        {
            var clientId = __instance.myPlayer.GetClientId();

            if (!_players.TryGetValue(clientId, out var tracker) || tracker.Target == null)
            {
                tracker = new PlayerMotionTracker(__instance.myPlayer);
                _players[clientId] = tracker;
            }

            tracker.Tick(Time.fixedDeltaTime);

            TryCleanupDisconnectedPlayers();
        }

        private static void TryCleanupDisconnectedPlayers()
        {
            if (Time.time - _lastCleanupTime < CleanupIntervalSeconds) return;
            _lastCleanupTime = Time.time;

            if (AmongUsClient.Instance == null || _players.Count == 0) return;

            var connectedClientIds = new HashSet<int>();
            foreach (var client in AmongUsClient.Instance.allClients)
            {
                connectedClientIds.Add(client.Id);
            }

            List<int> staleKeys = null;
            foreach (var kvp in _players)
            {
                if (!connectedClientIds.Contains(kvp.Key) || kvp.Value.Target == null)
                {
                    (staleKeys ??= []).Add(kvp.Key);
                }
            }

            if (staleKeys == null) return;

            foreach (var key in staleKeys)
            {
                _players.Remove(key);
            }
        }
    }
}