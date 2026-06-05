using AMG.Utilities;
using HarmonyLib;
using UnityEngine;

namespace AMG.Patches.ROundPatches
{
    [HarmonyPatch]
    public static class KillPatches
    {
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcMurderPlayer))]
        [HarmonyPostfix]
        private static void MurderPlayer_Postfix(PlayerControl __instance, PlayerControl target)
        {
            Utils.Round.AddDeadBody(new RoundDeadBody
            {
                PlayerId = target.PlayerId,
                TimeOfDeath = Time.time,
                Position = target.transform.position
            });
        }
    }
}
