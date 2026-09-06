using System.Collections.Generic;
using AMG.AI.Control.AgentController;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.Enums.AgentEnums;
using AMG.Utilities;
using HarmonyLib;

namespace AMG.Patches.Gameplay
{
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
    public static class RealPlayerMovementPatch
    {
        public static void Postfix(PlayerPhysics __instance)
        {
            if (!AgentController.AgentControlsRealPlayer) return;
            if (PlayerControl.LocalPlayer == null) return;
            if (__instance != PlayerControl.LocalPlayer.MyPhysics) return;

            var brain = PlayerControl.LocalPlayer.GetComponent<AgentController>();
            if (brain == null) return;
            if (brain is StructuredAgentBrain structuredBrain)
            {
                if (structuredBrain.currentState == AgentState.InVent) return;
            }

            __instance.body.velocity = brain.DesiredVelocity;
        }
    }
}