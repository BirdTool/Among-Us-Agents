using AMG.Utilities;
using HarmonyLib;
using UnityEngine;

namespace AMG.Patches.RoundPatches
{
    [HarmonyPatch]
    public static class KillPatches
    {
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcMurderPlayer))]
        [HarmonyPostfix]
        private static void MurderPlayer_Postfix(PlayerControl __instance, PlayerControl target)
        {
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
