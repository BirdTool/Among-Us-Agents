using HarmonyLib;
using AMG.AI.Control;
using AMG.Utilities;

namespace AMG.Patches
{
    [HarmonyPatch]
    public static class PlayerPhysicsPatch
    {
        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.LateUpdate))]
        [HarmonyPostfix]
        public static void PlayerPhysics_LateUpdate_Postfix()
        {
            if (ShipStatus.Instance != null) 
            {
                Utils.TakeDoorsSnapshot();
            }
        }
    }
}