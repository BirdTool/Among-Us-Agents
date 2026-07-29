using AMG.Models.Signals;
using AMG.Utilities;
using HarmonyLib;

namespace AMG.Patches.Gameplay.Signals
{
    [HarmonyPatch]
    public static class MurderSignals
    {
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        [HarmonyPostfix]
        public static void RpcMurderPlayer_Postfix(PlayerControl __instance, PlayerControl target)
        {
            var killSignal = new KillSignal(__instance, target);
            SignalController.SendSignalRadiusCanSee(killSignal, __instance.transform.position);
        }
    }
}
