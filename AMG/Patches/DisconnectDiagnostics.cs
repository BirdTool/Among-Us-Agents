using AMG.Utilities;
using HarmonyLib;
using InnerNet;

namespace AMG.Patches
{
    [HarmonyPatch]
    public static class DisconnectDiagnostics
    {
        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleDisconnect), typeof(DisconnectReasons), typeof(string))]
        [HarmonyPrefix]
        public static void HandleDisconnect_Prefix(DisconnectReasons reason, string stringReason)
        {
            LogManager.LogWarning($"[Disconnect] This client was disconnected: reason={reason}, text=\"{stringReason}\", "
                + $"inMeeting={Utils.IsMeeting}, voting={Utils.IsMeetingVoting}.");
        }

        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
        [HarmonyPrefix]
        public static void OnPlayerLeft_Prefix(ClientData data, DisconnectReasons reason)
        {
            LogManager.LogWarning($"[Disconnect] Player left: {data?.PlayerName} (client {data?.Id}), reason={reason}, "
                + $"I am host={AmongUsClient.Instance.AmHost}.");
        }

        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.KickPlayer))]
        [HarmonyPrefix]
        public static void KickPlayer_Prefix(int clientId, bool ban)
        {
            LogManager.LogWarning($"[Disconnect] KickPlayer(clientId={clientId}, ban={ban}) was called.\n{new System.Diagnostics.StackTrace()}");
        }
    }
}
