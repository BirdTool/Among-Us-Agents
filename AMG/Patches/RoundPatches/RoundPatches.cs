using AMG.AI.Control;
using AMG.AI.Mind;
using AMG.AI.Navigation;
using AMG.AI.Tools;
using AMG.Utilities;
using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace AMG.Patches.RoundPatches
{
    [HarmonyPatch]
    public static class RoundPatches
    {
        public static float ShipStartedAt { get; private set; }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
        [HarmonyPostfix]
        public static void ShipStatus_Start_Postfix()
        {
            Utils.Round.ClearRounds();
            Utils.Round.AddRound();
            TaskAssignment.SetCommonTask();
            ShipStartedAt = Time.time;
            Pathfinder.Initialize();
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
        [HarmonyPostfix]
        public static void MeetingHud_Start_Postfix()
        {
            Utils.Round.AddRound();
            foreach (AgentListData agent in AgentManager.Agents)
            {
                var brain = agent.Control.gameObject.GetComponent<AgentBrain>();
                brain.sawABody = false;
                brain.ResetPath();
                brain.SetState(Enums.AgentEnums.AgentState.OnMeeting);
            }
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
