using AMG.Utilities;
using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace AMG.Patches.RoundPatches
{
    [HarmonyPatch]
    public static class RoundPatches
    {
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
        [HarmonyPostfix]
        public static void ShipStatus_Start_Postfix()
        {
            Utils.Round.ClearRounds();
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
        [HarmonyPostfix]
        public static void MeetingHud_Start_Postfix()
        {
            Utils.Round.AddRound();
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
        [HarmonyPostfix]
        public static void PlayerControl_ReportDeadBody_Postfix(PlayerControl __instance, NetworkedPlayerInfo target)
        {
            var body = Utils.Round.CurrentRoundDeadBodies.FirstOrDefault(x => x.PlayerId == target.PlayerId);
            body?.TimeOfReport = Time.time;
        }
    }
}
