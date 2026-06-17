using HarmonyLib;
using AMG.AI.Control;

namespace AMG.Patches
{
    [HarmonyPatch]
    public static class ConnectionPatches
    {
        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.ExitGame))]
        [HarmonyPostfix]
        public static void AmongUsClient_ExitGame_Postfix()
        {
            AgentManager.ClearAllAgents();
        }
    }
}