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
                // if (Utils.IsMeeting || Utils.IsLobby) LlmMeetingTranscript.Add(sourcePlayer, chatText);
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