using AMG.Patches.Gameplay.Signals;
using AMG.Utilities;
using HarmonyLib;
using UnityEngine;

namespace AMG.Patches.RoundPatches
{
    [HarmonyPatch]
    public static class KillPatches
    {
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        [HarmonyPrefix]
        private static void MurderPlayer_Prefix(PlayerControl __instance, PlayerControl target)
        {
            if (!MurderSignals.IsKillGoingThrough(target)) return;

            var isViper = __instance.Data.Role.Role == AmongUs.GameOptions.RoleTypes.Viper;
            Utils.Round.AddDeadBody(new RoundDeadBody
            {
                PlayerId = target.PlayerId,
                TimeOfDeath = Time.time,
                Position = target.transform.position,
                IsViper = isViper
            });
        }
    }
}
