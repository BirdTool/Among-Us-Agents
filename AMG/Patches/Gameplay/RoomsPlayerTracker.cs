using System.Linq;
using AMG.Utilities;
using HarmonyLib;

namespace AMG.Patches.Gameplay
{
    [HarmonyPatch]
    public static class RoomsPlayerTracker
    {
        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
        [HarmonyPostfix]
        public static void PlayerTrackerRoom(PlayerPhysics __instance)
        {
            if (Utils.IsMeeting) return;
            var amgPlayer = AMGPlayers.GetPlayer(__instance.myPlayer);
            var history = amgPlayer.PlayerRoomHistory;
            var lastRoom = history.LastOrDefault();

            var currentNode = amgPlayer.Player.GetTruePosition().GetClosestNode();
            var currentRoom = currentNode.Room;
            
            if (lastRoom == null || lastRoom.Room != currentRoom)
                amgPlayer.AddRoomHistory();
        }
    }
}