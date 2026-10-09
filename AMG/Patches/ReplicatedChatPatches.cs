using AMG.AI.Services.ChatService;
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
            if(sourcePlayer == null || sourcePlayer.Data.IsDead) return;

            ReplicatedChat.Enqueue(sourcePlayer, chatText);
        }
    }
}