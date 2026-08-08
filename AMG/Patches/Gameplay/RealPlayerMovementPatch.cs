using HarmonyLib;
using AMG.AI.Mind;

namespace AMG.Patches.Gameplay
{
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
    public static class RealPlayerMovementPatch
    {
        public static void Postfix(PlayerPhysics __instance)
        {
            if (!AgentBrain.AgentControlsRealPlayer) return;
            if (PlayerControl.LocalPlayer == null) return;
            if (__instance != PlayerControl.LocalPlayer.MyPhysics) return;

            var brain = PlayerControl.LocalPlayer.GetComponent<AgentBrain>();
            if (brain == null) return;

            __instance.body.velocity = brain.DesiredVelocity;
        }
    }
}