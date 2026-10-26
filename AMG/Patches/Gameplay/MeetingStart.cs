using AMG.AI.Services.ChatService;
using AMG.AI.Services.ChatService.Llm;
using AMG.AI.Services.ChatService.Local;
using AMG.AI.Services.ChatService.Local.Corpus;
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
            if (ChatMode.IsLlm)
            {
                LlmMeetingTranscript.Reset();
            }
            else
            {
                ClaimStore.Reset();
                ReplicatedChat.Clear();
                ChatOutbox.Clear();
                ChatCorpus.OnMeetingStart();
            }

            if (!Utils.IsFreePlay) return;
            TableSpawnLocations.MakeAllAgentsSpawnAtTable();
        }
    }
}