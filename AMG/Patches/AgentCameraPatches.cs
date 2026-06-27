using AMG.AI.Control;
using HarmonyLib;
using System.Linq;

namespace AMG.Patches
{
    [HarmonyPatch(typeof(FollowerCamera), nameof(FollowerCamera.SetTarget))]
    public static class FollowerCamera_SetTarget_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerControl target)
        {
            if (target != null && AgentManager.Agents.Any(a => a.Control == target))
                return false;
            return true;
        }
    }
}