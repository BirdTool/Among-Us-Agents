using HarmonyLib;
using AMG.Utilities;
using System.Collections.Generic;
using AMG.Utilities.MapUtils;

namespace AMG.Patches.Gameplay
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    public static class MeetingStart
    {
        public static void Prefix()
        {
            if (!Utils.IsFreePlay) return;
            TableSpawnLocations.MakeAllAgentsSpawnAtTable();
        }
    }
}