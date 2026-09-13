using System.Collections.Generic;
using System.Linq;

namespace AMG.Utilities
{
    public static class ScenarioUtils
    {
        public static PlayerControl FindSingleCrewmateInRooms(params SystemTypes[] rooms)
        {
            PlayerControl target = null;
            int count = 0;
            foreach (var room in rooms)
            {
                foreach (var p in Utils.Players.GetAllAlivePlayersInARoom(room))
                {
                    if (p.Data.Role.IsImpostor) continue;
                    target = p;
                    count++;
                }
            }
            return count == 1 ? target : null;
        }

        public static bool HasWitnessOutside(List<PlayerControl> nearby, params SystemTypes[] allowedRooms)
        {
            return nearby.Any(p => !allowedRooms.Contains(p.GetTruePosition().GetClosestNode().Room));
        }
    }
}