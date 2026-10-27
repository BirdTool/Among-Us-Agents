using HarmonyLib;

namespace AMG.Patches.RoundPatches
{
    [HarmonyPatch]
    public static class EjectionTrackerPatch
    {
        // VotingComplete is the RPC every client receives when the meeting's votes are counted
        // (exiled == null or tie == true: nobody was ejected). CheckForEndVoting only runs the real logic on the host
        // and its postfix also ran after every single vote, so it could mark someone as ejected too early.
        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
        [HarmonyPostfix]
        public static void VotingComplete_Postfix(NetworkedPlayerInfo exiled, bool tie, bool wasOverruled)
        {
            var result = AMG.Utilities.AMGPlayers.RecordMeetingResult(exiled, tie, wasOverruled);

            AMG.Utilities.LogManager.LogDebug(result.EjectedId is byte id
                ? $"[EjectionTracker] Meeting #{result.Number}: player {id} was ejected (overruled={wasOverruled}, confirmEjects={result.ConfirmEjects}, wasImpostor={(result.EjectedWasImpostor?.ToString() ?? "not revealed")})."
                : $"[EjectionTracker] Meeting #{result.Number}: nobody was ejected (tie={tie}).");
        }
    }
}
