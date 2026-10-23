using AMG.Utilities;
using HarmonyLib;
using UnityEngine;

namespace AMG.Modules.AutoPlay
{
    // The AI client must press "Play Again" by itself the moment the match ends: if a human does it,
    // the delay can cost the agent its lobby colour. Only runs while PersistentAgent is on (N key).
    [HarmonyPatch]
    public static class AutoReturnToLobby
    {
        private const string PlayAgainButtonName = "PlayAgainButton";
        private const float TryInterval = 0.25f;
        private const float GiveUpAfter = 60f;

        private static bool _pending;
        private static float _nextTry;
        private static float _giveUpAt;

        [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
        [HarmonyPostfix]
        public static void EndGameManager_Start_Postfix()
        {
            if (!PersistentAgent.Enabled || Utils.IsFreePlay) return;

            _pending = true;
            _nextTry = 0f;
            _giveUpAt = Time.unscaledTime + GiveUpAfter;
            LogManager.LogInfo("[AutoReturn] Match ended: waiting for the Play Again button.");
        }

        [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
        [HarmonyPostfix]
        public static void LobbyBehaviour_Start_Postfix() => Cancel();

        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.ExitGame))]
        [HarmonyPrefix]
        public static void AmongUsClient_ExitGame_Prefix() => Cancel();

        public static void Cancel() => _pending = false;

        // Call every frame from the Unity main thread (MenuUI.Update). Does nothing unless a match just ended.
        public static void Tick()
        {
            if (!_pending) return;

            float now = Time.unscaledTime;
            if (now < _nextTry) return;
            _nextTry = now + TryInterval;

            if (now > _giveUpAt)
            {
                _pending = false;
                LogManager.LogWarning("[AutoReturn] Play Again button not found: gave up.");
                return;
            }

            if (TryClickPlayAgain())
                _pending = false;
        }

        private static bool TryClickPlayAgain()
        {
            try
            {
                foreach (var button in UnityEngine.Object.FindObjectsOfType<PassiveButton>())
                {
                    if (button == null || button.gameObject.name != PlayAgainButtonName || !button.isActiveAndEnabled)
                        continue;

                    button.OnClick.Invoke();
                    LogManager.LogInfo("[AutoReturn] Play Again pressed.");
                    return true;
                }
            }
            catch (System.Exception e)
            {
                LogManager.LogError($"[AutoReturn] Could not press Play Again: {e.Message}");
            }

            return false;
        }
    }
}
