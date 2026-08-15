using System.Collections.Generic;
using Epic.OnlineServices.Achievements;
using UnityEngine;

namespace AMG.Utilities
{
    public static class KillCooldownManager
    {
        public static readonly Dictionary<byte, float> _killCooldowns = [];
        public static float? KillCooldown
        {
            get
            {
                if (GameOptionsManager.Instance == null || GameOptionsManager.Instance.currentNormalGameOptions == null) return null;

                return GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown;
            }
        }

        public static void StartCooldown(byte playerId)
        {
            if (KillCooldown == null) return;
            _killCooldowns[playerId] = Time.time + KillCooldown.Value;
        }

        public static void StartCooldownAsHalf(byte playerId)
        {
            if (KillCooldown == null) return;
            _killCooldowns[playerId] = Time.time + KillCooldown.Value / 2;
        }

        public static void StartCooldownOfEveryImpostor()
        {
            foreach (var player in Utils.Players.AllImpostors)
            {
                StartCooldown(player.Data.PlayerId);
            }
        }

        public static bool CanKill(byte playerId)
        {
            if (KillCooldown == null) return true;

            if (!_killCooldowns.ContainsKey(playerId)) return true;

            var timeSinceLastKill = Time.time - _killCooldowns[playerId];
            var isReady = timeSinceLastKill >= KillCooldown.Value;
            
            if (isReady)
            {
                _killCooldowns.Remove(playerId);
            }

            return isReady;
        }
    }
}