using AMG.AI.Control;
using HarmonyLib;
using System.Linq;

namespace AMG.Patches
{
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
    public static class PlayerPhysics_FixedUpdate_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerPhysics __instance)
        {
            var pc = __instance.GetComponent<PlayerControl>();
            if (pc != null && AgentManager.Agents.Any(a => a.Control == pc))
                return false;
            return true;
        }
    }
}
