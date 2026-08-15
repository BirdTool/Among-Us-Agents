using HarmonyLib;
using AMG.AI.Mind;
using AMG.AI.Tools;

namespace AMG.Patches
{
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
    public static class UpdatePatches
    {
        public static void Postfix(/* PlayerPhysics __instance */)
        {
            DoorCooldownTracker.UpdateDoorsState();
        }
    }
}