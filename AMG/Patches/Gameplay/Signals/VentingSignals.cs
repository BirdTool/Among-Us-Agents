using System.Collections.Generic;
using AMG.Models.Signals;
using AMG.Utilities;
using HarmonyLib;

namespace AMG.Patches.Gameplay.Signals
{
    [HarmonyPatch]
    public static class VentSignals
    {
        private static readonly List<byte> _playersInVent = [];
        
        // Personalized RPC
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        [HarmonyPostfix]
        public static void VentPersonalizedCall(PlayerControl __instance)
        {
            if (__instance.Data.IsDead)
                return;

            var inVent = __instance.inVent;
            var wasInVent = _playersInVent.Contains(__instance.PlayerId);

            if (inVent && !wasInVent)
            {
                var ventSignal = new VentSignal(__instance, false);
                SignalController.SendSignalRadiusCanSee(ventSignal, __instance.transform.position);
                _playersInVent.Add(__instance.PlayerId);
            }
            else if (!inVent && wasInVent)
            {
                var ventSignal = new VentSignal(__instance, true);
                SignalController.SendSignalRadiusCanSee(ventSignal, __instance.transform.position);
                _playersInVent.Remove(__instance.PlayerId);
            }
        }
    }
}
