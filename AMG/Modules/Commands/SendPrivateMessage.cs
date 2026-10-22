using AMG.AI.Services.MessagesService;
using AMG.Data;
using HarmonyLib;

namespace AMG.Modules.Commands
{
    [HarmonyPatch]
    public static class SendPrivateMessageCommand
    {
        public static string CommandPrefix => ".";
        public static string CommandName => "private";

        public static void Send(string content) => MessageServiceClass.WriteMessage(content, GlobalVariables.MyIdentity);

        [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
        [HarmonyPrefix]
        public static bool Prefix(ChatController __instance)
        {
            string text = __instance.freeChatField.textArea.text;

            if (text.StartsWith(CommandPrefix + CommandName))
            {
                string content = text[(CommandPrefix.Length + CommandName.Length)..];
                Send(content);
                __instance.freeChatField.Clear();
                return false;
            }
            return true;
        }
    }
}