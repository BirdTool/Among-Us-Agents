using AMG.AI.Services.ChatService;
using AMG.AI.Services.ChatService.Corpus;
using HarmonyLib;
using AMG.Utilities;
using AMG.Utilities.MapUtils;

namespace AMG.Patches.Gameplay
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    public static class MeetingStart
    {
        public static void Prefix()
        {
            ClaimStore.Reset();
            ReplicatedChat.Clear();
            ChatOutbox.Clear();
            ChatCorpus.OnMeetingStart();

            if (!Utils.IsFreePlay) return;
            TableSpawnLocations.MakeAllAgentsSpawnAtTable();
        }
    }
}