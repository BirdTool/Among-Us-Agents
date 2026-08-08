using HarmonyLib;
using AMG.Utilities;

namespace AMG.Patches.Gameplay
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
    public static class DebugVoteLogger
    {
        public static void Prefix(byte srcPlayerId, byte suspectPlayerId)
        {
            /*
            var isMe = Utils.Players.LocalPlayer.PlayerId == srcPlayerId;

            LogManager.LogDebug($"[DEBUG VOTE] src={srcPlayerId} suspect={suspectPlayerId} [isItMe: {isMe}]");
            */
        }
    }
}