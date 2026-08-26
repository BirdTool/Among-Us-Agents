using AMG.AI.Mind.StructuredAgentBrain.Decisions.Sabotages;
using HarmonyLib;

namespace AMG.Patches.Gameplay
{
    [HarmonyPatch]
    public static class SabotagePatches
    {
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.RpcUpdateSystem), [typeof(SystemTypes), typeof(byte)])]
        [HarmonyPostfix]
        private static void RpcUpdateSystem_Postfix()
        {
            SabotageManager.CheckSabotageStateChange();
        }
    }
}