using System;
using System.Linq;
using System.Reflection;
using System.Text;
using AMG.AI.Control.AgentController;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.Utilities;
using HarmonyLib;
using UnityEngine;

namespace AMG.Patches.Diagnostics
{
    internal static class KillDiagnostics
    {
        public static bool Enabled = true;

        // Same range SignalController.SendSignalRadiusCanSee uses.
        private const float SignalRange = 6f;

        private static int _rpcHits;
        private static int _murderHits;

        internal static void Report(string source, PlayerControl killer, PlayerControl target, string extra = null)
        {
            if (!Enabled || killer == null || target == null)
                return;

            try
            {
                if (source == "RpcMurderPlayer") _rpcHits++;
                else _murderHits++;

                var sb = new StringBuilder();
                sb.AppendLine($"[KillDiag] ===== kill seen via {source} (RpcMurderPlayer hits: {_rpcHits}, MurderPlayer hits: {_murderHits}) =====");

                var local = PlayerControl.LocalPlayer;
                bool host = AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;
                sb.AppendLine($"host={host} | local player={Describe(local)} | meeting={Utils.IsMeeting}{(extra == null ? "" : " | " + extra)}");

                Vector2 killerPos = killer.transform.position;     // what MurderSignals sends to SignalController
                Vector2 killerTruePos = killer.GetTruePosition();
                Vector2 targetPos = target.GetTruePosition();

                sb.AppendLine($"killer={Describe(killer)} role={killer.Data?.Role?.Role} pos={Pos(killerPos)} truePos={Pos(killerTruePos)} room={AgentVision.GetRoomAtPosition(killerTruePos)}");
                sb.AppendLine($"target={Describe(target)} pos={Pos(targetPos)} room={AgentVision.GetRoomAtPosition(targetPos)} dead={target.Data?.IsDead}");
                sb.AppendLine($"killer -> target distance = {Vector2.Distance(killerTruePos, targetPos):0.00}");

                AppendBodies(sb, target);
                AppendCamera(sb);

                var agents = Utils.GetAllAgentController();
                sb.AppendLine($"agents in the match: {agents.Count}");

                foreach (var agent in agents)
                {
                    if (agent == null || agent.Agent == null)
                        continue;

                    AppendAgent(sb, agent, killer, target, killerPos, targetPos);
                }

                LogManager.LogInfo(sb.ToString().TrimEnd());
            }
            catch (Exception e)
            {
                LogManager.LogWarning($"[KillDiag] report failed: {e.Message}");
            }
        }

        private static void AppendBodies(StringBuilder sb, PlayerControl target)
        {
            var roundBodies = Utils.Round.CurrentRoundDeadBodies;
            int registered = roundBodies?.Count ?? -1;
            bool hasTarget = roundBodies != null && roundBodies.Any(b => b.PlayerId == target.PlayerId);

            int world = 0;
            bool worldHasTarget = false;

            foreach (var body in UnityEngine.Object.FindObjectsOfType<DeadBody>())
            {
                world++;
                if (body.ParentId == target.PlayerId) worldHasTarget = true;
            }

            sb.AppendLine($"bodies: registered in Utils.Round = {(registered < 0 ? "none (no round started)" : registered.ToString())} (includes the victim: {hasTarget}) | DeadBody objects in the world = {world} (include the victim: {worldHasTarget})");
        }

        private static void AppendCamera(StringBuilder sb)
        {
            var cam = Camera.main;

            if (cam == null)
            {
                sb.AppendLine("camera: Camera.main is null (IsWithinScreenBounds always passes)");
                return;
            }

            float halfH = cam.orthographicSize;
            sb.AppendLine($"camera: half size = {halfH * cam.aspect:0.0} x {halfH:0.0} (IsWithinScreenBounds uses it as the vision box)");
        }

        private static void AppendAgent(StringBuilder sb, AgentController agent, PlayerControl killer, PlayerControl target, Vector2 killerPos, Vector2 targetPos)
        {
            Vector2 pos = agent.Vector2Position;
            bool alive = agent.Agent.Data != null && !agent.Agent.Data.IsDead;
            bool isKiller = agent.AgentId == killer.PlayerId;

            sb.AppendLine($"  agent {Describe(agent.Agent)} [{agent.GetType().Name}] alive={alive}{(isKiller ? " (THE KILLER)" : "")} pos={Pos(pos)} room={AgentVision.GetRoomAtPosition(pos)} | dist to killer={Vector2.Distance(pos, killerPos):0.0}, to victim={Vector2.Distance(pos, targetPos):0.0}");
            sb.AppendLine($"      sees killer : {Vision(pos, killerPos)}");
            sb.AppendLine($"      sees victim : {Vision(pos, targetPos)}");

            bool canSeePlayerKiller = !isKiller && AgentVision.CanSeePlayer(agent.Agent, killer);
            bool canSeePlayerTarget = AgentVision.CanSeePlayer(agent.Agent, target);
            int nearbyBodies = AgentVision.GetNearbyDeadBodies(pos, SignalRange).Count;
            sb.AppendLine($"      AgentVision.CanSeePlayer: killer={canSeePlayerKiller}, victim={canSeePlayerTarget} | bodies within {SignalRange:0}: {nearbyBodies}");

            // Mirrors MurderSignals -> SignalController.SendSignalRadiusCanSee (only StructuredAgentBrain receives signals).
            bool structured = agent is StructuredAgentBrain;
            bool wouldSee = !isKiller && Utils.CanSeeTheTarget(pos, killerPos, SignalRange);
            string why = isKiller ? "it is the killer"
                : !structured ? "not a StructuredAgentBrain: SignalController never sends it anything"
                : !wouldSee ? "cannot see the killer"
                : "yes";
            sb.AppendLine($"      kill signal would be delivered (if the hook fired): {why}");
        }

        // Same checks as Utils.CanSeeTheTarget, one by one, so the log says which one blocks the vision.
        private static string Vision(Vector2 from, Vector2 to)
        {
            Vector2 a = new(from.x, from.y + 0.5f);
            Vector2 b = new(to.x, to.y + 0.5f);

            float dist = Vector2.Distance(a, b);
            bool range = dist <= SignalRange;
            bool screen = AgentVision.IsWithinScreenBounds(a, b);
            bool door = !AgentVision.IsBlockedByClosedDoor(a, b);
            bool clear = !AgentVision.IsObstructed(a, b);
            bool final = Utils.CanSeeTheTarget(from, to, SignalRange);

            return $"dist={dist:0.0} range={Ok(range)} screen={Ok(screen)} door={Ok(door)} clear={Ok(clear)} => {(final ? "CAN SEE" : "cannot see")}";
        }

        private static string Ok(bool value) => value ? "ok" : "FAIL";
        private static string Pos(Vector2 p) => $"({p.x:0.0},{p.y:0.0})";

        private static string Describe(PlayerControl player)
            => player == null ? "none" : $"#{player.PlayerId} {player.Data?.PlayerName}";
    }

    [HarmonyPatch]
    public static class KillDiagnosticsRpcPatch
    {
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcMurderPlayer), [typeof(PlayerControl), typeof(bool)])]
        [HarmonyPostfix]
        private static void RpcMurderPlayer_Postfix(PlayerControl __instance, PlayerControl target, bool didSucceed)
        {
            KillDiagnostics.Report("RpcMurderPlayer", __instance, target, $"didSucceed={didSucceed}");
        }

        [HarmonyPatch]
        public static class KillDiagnosticsMurderPatch
        {
            [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
            [HarmonyPostfix]
            private static void MurderPlayer_Postfix(PlayerControl __instance, PlayerControl target)
            {
                KillDiagnostics.Report("MurderPlayer", __instance, target);
            }
        }
    }
}
