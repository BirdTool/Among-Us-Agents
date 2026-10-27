using AMG.AI.Navigation;
using AMG.AI.Services.ChatService;
using AMG.AI.Services.ChatService.Llm;
using AMG.AI.Services.ChatService.Local;
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
            AMG.Patches.Gameplay.Signals.VentSignals.Reset();
            AMG.AI.Navigation.WaypointManager.LoadWaypoints();
            Pathfinder.Initialize();

            // roster of everybody who started this match (kept after they die / get ejected / leave)
            AMGPlayers.SetAllPlayers();

            // new match: forget the previous one's players, then remember everybody who started this one
            if (!ChatMode.IsLlm)
            {
                ChatRoster.Reset();
                ChatParser.RememberPlayers();
            }
        }

        // developer directives only live for the match they were given in
        [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
        [HarmonyPostfix]
        public static void EndGameManager_Start_Postfix()
        {
            foreach (var brain in Utils.GetAllStructuredAgentBrain())
            {
                brain.LlmMeeting?.ClearDirectives();
                brain.LlmMeeting?.ClearMemory();
            }
            LlmMeetingTranscript.NewMatch();
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
        [HarmonyPostfix]
        public static void MeetingHud_Start_Postfix()
        {
            AMG.Patches.Gameplay.Signals.VentSignals.Reset();
            Utils.Round.AddRound();
            AMGPlayers.BeginMeeting();
            var allStructuredBrains = Utils.GetAllStructuredAgentBrain();
            foreach (var brain in allStructuredBrains)
            {
                brain.sawABody = false;
                brain.ResetPath();
                brain.SetState(Enums.AgentEnums.AgentState.OnMeeting);
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
        [HarmonyPostfix]
        public static void PlayerControl_ReportDeadBody_Postfix(PlayerControl __instance, NetworkedPlayerInfo target)
        {
            AMG.Patches.Gameplay.Signals.VentSignals.Reset();
            LlmMeetingContext.Capture(__instance, target);
            if (target == null) return;   // emergency button: there is no body

            var body = Utils.Round.CurrentRoundDeadBodies.FirstOrDefault(x => x.PlayerId == target.PlayerId);
            body?.TimeOfReport = Time.time;
        }
    }
}
