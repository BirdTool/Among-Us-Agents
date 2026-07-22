using HarmonyLib;

namespace AMG.Patches
{
    [HarmonyPatch(typeof(PlayerPurchasesData), nameof(PlayerPurchasesData.GetPurchase))]
    public static class PlayerPurchasesData_GetPurchase
    {
        // Postfix patch of PlayerPurchasesData.GetPurchase to unlock all cosmetics
        public static void Postfix(ref bool __result)
        {
            __result = true;
        }
    }

}
