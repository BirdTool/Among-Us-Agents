using AMG.Utilities;
using HarmonyLib;

namespace AMG.Patches.Gameplay
{
    [HarmonyPatch]
    public static class DoorPatches
    {
        [HarmonyPatch(typeof(PlainDoor), nameof(PlainDoor.SetDoorway))]
        [HarmonyPostfix]
        public static void PlainDoor_SetDoorway_Postfix()
        {
            Utils.NotifyDoorStateChanged();
        }
    }
}
