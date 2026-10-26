public enum SystemTypes { Hallway, Storage, Cafeteria, Reactor, UpperEngine, Nav, Admin, Electrical, LifeSupp, Shields, MedBay, Security, Weapons, LowerEngine, Comms }
namespace UnityEngine
{
    public static class Time { public static float time; }
    public static class Mathf
    {
        public static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
    }
}
