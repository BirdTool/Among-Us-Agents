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

        private const string ContinueButtonKeyword = "Continue";
        private const float ContinueRetryAfter = 2f;
        private const float DumpButtonsAfter = 5f;

        private static float _nextContinueTry;
        private static bool _dumpedButtons;

        [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
        [HarmonyPostfix]
        public static void EndGameManager_Start_Postfix()
        {
            if (!PersistentAgent.Enabled || Utils.IsFreePlay) return;

            _pending = true;
            _nextTry = 0f;
            _giveUpAt = Time.unscaledTime + GiveUpAfter;
            _nextContinueTry = 0f;
            _dumpedButtons = false;
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

            if (TryAdvance())
                _pending = false;
        }

        private static bool TryAdvance()
        {
            try
            {
                PassiveButton playAgain = null, next = null;

                foreach (var button in UnityEngine.Object.FindObjectsOfType<PassiveButton>())
                {
                    if (button == null || !button.isActiveAndEnabled) continue;

                    string buttonName = button.gameObject.name;
                    if (buttonName == PlayAgainButtonName)
                        playAgain = button;
                    else if (next == null && buttonName.IndexOf(ContinueButtonKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        next = button;
                }

                if (playAgain != null)
                {
                    playAgain.OnClick.Invoke();
                    LogManager.LogInfo("[AutoReturn] Play Again pressed.");
                    return true;
                }

                float now = Time.unscaledTime;

                if (next != null)
                {
                    if (now >= _nextContinueTry)
                    {
                        _nextContinueTry = now + ContinueRetryAfter;
                        next.OnClick.Invoke();
                        LogManager.LogInfo($"[AutoReturn] Continue pressed ({next.gameObject.name}).");
                    }
                    return false;
                }

                if (!_dumpedButtons && now > _giveUpAt - GiveUpAfter + DumpButtonsAfter)
                {
                    _dumpedButtons = true;
                    var names = new System.Collections.Generic.List<string>();
                    foreach (var button in UnityEngine.Object.FindObjectsOfType<PassiveButton>())
                        if (button != null && button.isActiveAndEnabled)
                            names.Add(button.gameObject.name);

                    LogManager.LogWarning($"[AutoReturn] No Continue/Play Again yet. Active buttons: {string.Join(", ", names)}");
                }
            }
            catch (System.Exception e)
            {
                LogManager.LogError($"[AutoReturn] Could not advance to the lobby: {e.Message}");
            }

            return false;
        }
    }
}
