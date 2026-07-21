using System.Collections.Generic;
using System.Linq;
using AMG.AI.Navigation;

namespace AMG.Models
{
    public class RoomDoor
    {
        public SystemTypes Room;
        public List<List<Waypoint>> DoorPoints; // Each List inside this list is a door
        public bool IsOpen => ShipStatus.Instance.AllDoors.FirstOrDefault(d => d.Room == Room).IsOpen;
        public PlainShipRoom RoomObject => ShipStatus.Instance.AllRooms.FirstOrDefault(r => r.RoomId == Room);
    }
}
