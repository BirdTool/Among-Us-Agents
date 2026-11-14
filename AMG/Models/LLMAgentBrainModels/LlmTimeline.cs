using AMG.AI.Navigation;
using AMG.Utilities;
using UnityEngine;

namespace AMG.Models.LLMAgentBrainModels
{
    public class LlmTimeline(string txt, Vector2 position, Vector2 selfPosition)
    {
        public float CreatedAt { get; set; } = (float)Utils.SecondsSinceShipStart;
        public string Text { get; set; } = txt;

        public Vector2 Position { get; set; } = position;
        public Waypoint TargetWaypoint => Position.GetClosestNode();
        public SystemTypes TargetRoom => TargetWaypoint.Room;

        public Vector2 SelfPosition { get; set; } = selfPosition;
        public Waypoint SelfWaypoint => SelfPosition.GetClosestNode();
        public SystemTypes SelfRoom => SelfWaypoint.Room;

        public float Distance => Vector2.Distance(Position, SelfPosition);

        public string Transcript
            => $"Time: {CreatedAt:F2} | {Text} (Distance: {Distance:F2}m) | Your location: {Location(SelfPosition)} | Event/Player location: {Location(Position)}";

        private static string Location(Vector2 pos)
        {
            var waypoint = pos.GetClosestNode();
            var room = waypoint.Room;
            if (room == SystemTypes.Hallway)
                return $"Hallway that makes borders with: {string.Join(", ", waypoint.NeighborRooms)}";
            return room.ToString();
        }
    }
}