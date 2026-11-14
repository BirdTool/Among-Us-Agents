using AMG.Models.Signals;
using AMG.Utilities;
using HarmonyLib;

namespace AMG.Patches.Gameplay.Signals
{
    [HarmonyPatch]
    public static class MurderSignals
    {
        // Testing only: also send who REALLY killed when the killer was disguised (KillSignal.RealKiller).
        // Turn it off for real play, so agents only know what they could see.
        public static bool SendRealKillerForTests = true;

        // MurderPlayer applies the kill on EVERY client. RpcMurderPlayer only runs where the kill is originated
        // (the host), so as a regular client in a public match no one ever got a kill signal through it.
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        [HarmonyPrefix]
        public static void MurderPlayer_Prefix(PlayerControl __instance, PlayerControl target)
        {
            if (!IsKillGoingThrough(target)) return;

            var shown = GetDisplayedPlayer(__instance, out bool isDisguised);

            if (shown == null)
            {
                // Disguised, but we could not tell as whom: all we have left is the real killer.
                LogManager.LogWarning($"[KillSignal] #{__instance.PlayerId} killed while shapeshifted as \"{__instance.CurrentOutfit.PlayerName}\", but no player has that name.");
                shown = __instance;
            }

            var killSignal = new KillSignal(shown, target, isDisguised, SendRealKillerForTests ? __instance : null);
            SignalController.SendSignalRadiusCanSee(killSignal, __instance.transform.position, __instance);
        }

        // MurderPlayer also runs for kills that fail: skip a target that is already dead or shielded by a guardian angel
        // (255 means "nobody", like in SafeKillNotExecute).
        internal static bool IsKillGoingThrough(PlayerControl target)
        {
            if (target == null || target.Data == null || target.Data.IsDead) return false;

            return target.protectedByGuardianId == -1 || target.protectedByGuardianId == 255;
        }

        // What witnesses SEE: a shapeshifted player looks like the one he copied. null = disguised as someone we cannot find.
        private static PlayerControl GetDisplayedPlayer(PlayerControl player, out bool isDisguised)
        {
            isDisguised = player.CurrentOutfitType == PlayerOutfitType.Shapeshifted;
            if (!isDisguised) return player;

            string shownName = player.CurrentOutfit.PlayerName;

            foreach (var other in PlayerControl.AllPlayerControls)
            {
                if (other == null || other == player || other.Data == null) continue;
                if (other.Data.PlayerName == shownName) return other;
            }

            return null;
        }
    }
}
