using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AMG.AI.Tools
{
    public static class DoorCooldownTracker
    {
        private static readonly Dictionary<int, float> _doorClosedTimestamps = [];
        
        private const float DOOR_CLOSE_DURATION = 10f;

        public static void UpdateDoorsState()
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllDoors == null) return;

            foreach (var door in ShipStatus.Instance.AllDoors)
            {
                bool isClosed = !door.IsOpen; 

                if (isClosed)
                {
                    if (!_doorClosedTimestamps.ContainsKey(door.Id))
                    {
                        _doorClosedTimestamps[door.Id] = Time.time;
                    }
                }
                else
                {
                    if (_doorClosedTimestamps.ContainsKey(door.Id))
                    {
                        _doorClosedTimestamps.Remove(door.Id);
                    }
                }
            }
        }

        public static float GetRoomDoorCooldown(SystemTypes doorRoom)
        {
            var doorsInRoom = ShipStatus.Instance.AllDoors.Where(x => x.Room == doorRoom).ToList();
            
            if (doorsInRoom.Count == 0) return 0f;

            float maxCooldown = 0f;

            foreach (var door in doorsInRoom)
            {
                if (_doorClosedTimestamps.TryGetValue(door.Id, out float closedTimestamp))
                {
                    float timePassed = Time.time - closedTimestamp;
                    
                    float remainingTime = DOOR_CLOSE_DURATION - timePassed;

                    if (remainingTime > maxCooldown)
                    {
                        maxCooldown = remainingTime;
                    }
                }
            }

            return Mathf.Max(0f, maxCooldown);
        }
    }
}