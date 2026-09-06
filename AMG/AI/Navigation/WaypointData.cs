using System;

namespace AMG.AI.Navigation
{
    [Serializable]
    public class WaypointData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public bool IsGold { get; set; }
    }
}