using System.Linq;
using HarmonyLib;

namespace AMG.Patches.RoundPatches
{
    [HarmonyPatch]
    public static class EjectionTrackerPatch
    {
        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
        [HarmonyPostfix]
        public static void CheckForEndVoting_Postfix(MeetingHud __instance)
        {
            var max = __instance.CalculateVotes().MaxPair(out var tie);
            var exiled = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(v => !tie && v.PlayerId == max.Key);

            if (exiled != null)
            {
                AMG.Utilities.AMGPlayers.SetPlayerAsEjected(exiled.PlayerId);
                AMG.Utilities.LogManager.LogDebug($"[EjectionTracker] The player {exiled.PlayerName} (ID: {exiled.PlayerId}) was ejected.");
            }
        }
    }
}