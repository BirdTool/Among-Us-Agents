using System;
using AMG.AI.Control;
using AMG.AI.Control.AgentController;
using AMG.AI.Mind;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.Utilities;
using InnerNet;
using UnityEngine;

namespace AMG.Modules.AutoPlay
{
    public static class PersistentAgent
    {
        private const float CheckInterval = 0.5f;
        private const float RetryAfterError = 5f;

        private static bool FirstCalculate = false;
        private const float CalculatingAfter = 20f;
        private static float NextCalculate;

        public static bool Enabled { get; private set; }

        private static float _nextCheck;
        private static StructuredAgentBrain _mappedBrain;
        private static float _mappedShip = -1f;

        public static void Toggle()
        {
            if (AmongUsClient.Instance == null || AmongUsClient.Instance.PlayerPrefab == null) return;

            var me = PlayerControl.LocalPlayer;

            if (Enabled || (me != null && me.GetComponent<AgentController>() != null))
            {
                Disable(me);
                return;
            }

            Enabled = true;
            LogManager.LogInfo("[Persistent] Brain ON: it will come back by itself after every match. Press N again to turn it off.");

            if (me != null && me.Data != null)
                Attach(me);
        }

        private static void Disable(PlayerControl me)
        {
            Enabled = false;
            AutoReturnToLobby.Cancel();

            if (me != null)
            {
                var controller = me.GetComponent<AgentController>();
                if (controller != null) UnityEngine.Object.Destroy(controller);

                var brain = me.gameObject.GetComponent<StructuredAgentBrain>();
                if (brain != null) UnityEngine.Object.Destroy(brain);
            }

            AgentController.AgentControlsRealPlayer = false;
            _mappedBrain = null;
            FirstCalculate = false;
            LogManager.LogInfo("[Persistent] Brain OFF.");
        }

        // Call every frame from the Unity main thread (MenuUI.Update).
        public static void Tick()
        {
            if (!Enabled) return;
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + CheckInterval;

            var client = AmongUsClient.Instance;
            var me = PlayerControl.LocalPlayer;
            if (client == null || me == null || me.Data == null) return;

            // Only in a lobby or a match: not on the end screen and not in the menus.
            var state = client.GameState;
            bool inMatch = state == InnerNetClient.GameStates.Started;
            if (!inMatch && state != InnerNetClient.GameStates.Joined) return;

            var brain = me.GetComponent<StructuredAgentBrain>();
            if (brain == null)
            {
                brain = Attach(me);
                if (brain == null) return;
            }

            if (!FirstCalculate)
            {
                NextCalculate = Time.unscaledTime + CalculatingAfter;
                FirstCalculate = true;
            }
            else if (NextCalculate > 0f && Time.unscaledTime >= NextCalculate)
            {
                brain.SetState(Enums.AgentEnums.AgentState.Calculating);
                NextCalculate = 0f;
            }

            // Tasks only exist during a match and are handed out a moment after it starts.
            if (inMatch && (_mappedBrain != brain || _mappedShip != AMG.Patches.RoundPatches.RoundPatches.ShipStartedAt))
                MapTasks(me, brain);
        }

        private static StructuredAgentBrain Attach(PlayerControl me)
        {
            try
            {
                // Players of the previous match no longer exist.
                AgentManager.Agents.RemoveAll(a => a.Control == null);
                AgentManager.AddAgent(me, new AgentData { Name = me.Data.PlayerName });

                me.gameObject.AddComponent<StructuredAgentBrain>();
                var brain = me.gameObject.GetComponent<StructuredAgentBrain>();

                AgentController.AgentControlsRealPlayer = true;
                MapTasks(me, brain);

                LogManager.LogInfo($"[Persistent] Brain attached to {me.Data.PlayerName}.");
                return brain;
            }
            catch (Exception e)
            {
                LogManager.LogError($"[Persistent] Could not attach the brain: {e}");
                _nextCheck = Time.unscaledTime + RetryAfterError;
                return null;
            }
        }

        private static void MapTasks(PlayerControl me, StructuredAgentBrain brain)
        {
            if (me.myTasks == null || me.myTasks.Count == 0) return;

            brain.MapArtificialTasks();
            brain.MapGameTasksToAILogic();

            _mappedBrain = brain;
            _mappedShip = AMG.Patches.RoundPatches.RoundPatches.ShipStartedAt;
        }
    }
}
