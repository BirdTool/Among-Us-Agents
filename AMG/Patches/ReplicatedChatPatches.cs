using AMG.AI.Services.ChatService;
using AMG.AI.Services.ChatService.Llm;
using AMG.AI.Services.ChatService.Local;
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
            if (!Utils.IsMeeting) return;
            if (sourcePlayer == null || sourcePlayer.Data.IsDead) return;

            if (ChatMode.IsLlm)
            {
                LlmMeetingTranscript.Add(sourcePlayer, chatText);
                return;
            }

            ReplicatedChat.Enqueue(sourcePlayer, chatText);
        }
        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
        [HarmonyPostfix]
        public static void DrainChat()
        {
            ReplicatedChat.Proccess();
        }
    }
}