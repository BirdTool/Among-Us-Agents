using System;
using AMG.Utilities;
using HarmonyLib;
using UnityEngine;

namespace AMG.Patches.Diagnostics
{
    // DIAGNOSTICS ONLY: logs, never changes behaviour. Search the log for "[SabotageDiag]".
    //
    // Today SabotageManager.CheckSabotageStateChange only runs from a postfix on ShipStatus.RpcUpdateSystem,
    // which (like RpcMurderPlayer) only runs on the machine that originates the call. This watches the real
    // sabotage state every frame (it is synced to every client) and compares it with what the agents were told:
    // if a sabotage is active but Utils.CurrentSabotage stays null, the agents were never notified.
    internal static class SabotageDiagnostics
    {
        public static bool Enabled = true;

        // How long a sabotage may stay "unnoticed" before it is reported as a problem.
        private const float NotifiedWithin = 1.5f;

        private static int _lastMask = -1;
        private static float _activeSince = -1f;
        private static bool _alerted;
        private static int _rpcHits;
        private static float _lastRpcAt = float.NegativeInfinity;

        internal static void OnRpcUpdateSystem(object[] args)
        {
            if (!Enabled)
                return;

            _rpcHits++;
            _lastRpcAt = Time.time;

            try
            {
                bool host = AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;
                object system = args != null && args.Length > 0 ? args[0] : null;
                object amount = args != null && args.Length > 1 ? args[1] : null;
                LogManager.LogInfo($"[SabotageDiag] RpcUpdateSystem postfix hit #{_rpcHits}: system={system} amount={amount} host={host} state=[{State(ShipStatus.Instance)}]");
            }
            catch (Exception e)
            {
                LogManager.LogWarning($"[SabotageDiag] rpc report failed: {e.Message}");
            }
        }

        // Called once per frame.
        internal static void Poll()
        {
            if (!Enabled)
                return;

            try
            {
                var ship = ShipStatus.Instance;

                if (ship == null)
                {
                    _lastMask = -1;
                    _activeSince = -1f;
                    _alerted = false;
                    return;
                }

                bool reactor = Utils.Sabotages.IsReactorSabotaged(ship);
                bool o2 = Utils.Sabotages.IsOxygenSabotaged(ship);
                bool lights = Utils.Sabotages.IsElectricalSabotaged(ship);
                bool comms = Utils.Sabotages.IsCommsSabotaged(ship);
                bool active = reactor || o2 || lights || comms;

                // Cheap per-frame comparison; the text is only built when something changed.
                int mask = (reactor ? 1 : 0) | (o2 ? 2 : 0) | (lights ? 4 : 0) | (comms ? 8 : 0);

                if (mask == _lastMask && _lastMask >= 0)
                {
                    if (active && !_alerted && _activeSince >= 0f && Time.time - _activeSince >= NotifiedWithin && Utils.CurrentSabotage == null)
                    {
                        _alerted = true;
                        LogManager.LogWarning($"[SabotageDiag] PROBLEM: [{State(ship)}] has been active for {Time.time - _activeSince:0.0}s but Utils.CurrentSabotage is still null: CheckSabotageStateChange never ran, so the agents ignore it. (RpcUpdateSystem hits: {_rpcHits})");
                    }

                    return;
                }

                _lastMask = mask;
                _alerted = false;
                _activeSince = active ? Time.time : -1f;

                bool host = AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;
                string ago = float.IsNegativeInfinity(_lastRpcAt) ? "never" : $"{Time.time - _lastRpcAt:0.0}s ago";
                LogManager.LogInfo($"[SabotageDiag] state changed: [{State(ship)}] host={host} | RpcUpdateSystem postfix hits so far={_rpcHits}, last {ago} | agents' current sabotage = {Current()}");
            }
            catch (Exception e)
            {
                LogManager.LogWarning($"[SabotageDiag] poll failed: {e.Message}");
            }
        }

        private static string State(ShipStatus ship)
        {
            if (ship == null)
                return "no ship";

            return $"reactor={Utils.Sabotages.IsReactorSabotaged(ship)} o2={Utils.Sabotages.IsOxygenSabotaged(ship)} lights={Utils.Sabotages.IsElectricalSabotaged(ship)} comms={Utils.Sabotages.IsCommsSabotaged(ship)}";
        }

        private static string Current() => Utils.CurrentSabotage?.GetType().Name ?? "null";
    }

    [HarmonyPatch]
    public static class SabotageDiagnosticsPatches
    {
        // Same hook SabotagePatches uses; arguments read through __args.
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.RpcUpdateSystem), [typeof(SystemTypes), typeof(byte)])]
        [HarmonyPostfix]
        private static void RpcUpdateSystem_Postfix(object[] __args)
        {
            SabotageDiagnostics.OnRpcUpdateSystem(__args);
        }

        // Frame poll (same hook VentSignals uses), only once per frame: for the local player.
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        [HarmonyPostfix]
        private static void FixedUpdate_Postfix(PlayerControl __instance)
        {
            if (__instance != PlayerControl.LocalPlayer)
                return;

            SabotageDiagnostics.Poll();
        }
    }
}
