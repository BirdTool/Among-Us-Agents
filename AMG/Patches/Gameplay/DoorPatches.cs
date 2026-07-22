using AMG.Utilities;
using HarmonyLib;

namespace AMG.Patches.Gameplay
{
    [HarmonyPatch]
    public static class DoorPatches
    {
        /// <summary>
        /// Fired whenever a door opens or closes.
        /// Invalidates the IsRoomClosed cache and the Pathfinder path cache.
        /// </summary>
        [HarmonyPatch(typeof(PlainDoor), nameof(PlainDoor.SetDoorway))]
        [HarmonyPostfix]
        public static void PlainDoor_SetDoorway_Postfix()
        {
            Utils.NotifyDoorStateChanged();
        }
    }
}
