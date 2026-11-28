using AMG.AI.Services.ChatService;
using AMG.AI.Services.ChatService.Local;
using AMG.AI.Services.MeetingService;
using AMG.Utilities;
using HarmonyLib;

namespace AMG.Patches
{
    [HarmonyPatch]
    public class ReplicatedChatPatches
    {
        [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
        [HarmonyPostfix]
        public static void ReceiveMessages(PlayerControl sourcePlayer, string chatText, bool censor, ChatController __instance)
        {
            if (sourcePlayer == null || sourcePlayer.Data.IsDead) return;

            if (ChatMode.IsLlm)
            {
                // The LLM reads the meeting through the transcript (own messages included, so it knows what it said).
                if (Utils.IsMeeting) MeetingTranscriptService.AddMessageToCurrentMeeting(sourcePlayer.PlayerId, chatText);
                return;
            }

            if (!Utils.IsMeeting) return;

            ReplicatedChat.Enqueue(sourcePlayer, chatText);
            MeetingTranscriptService.AddMessageToCurrentMeeting(sourcePlayer.PlayerId, chatText);
        }
        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
        [HarmonyPostfix]
        public static void DrainChat()
        {
            ReplicatedChat.Proccess();
        }
    }
}