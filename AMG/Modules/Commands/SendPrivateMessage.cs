using System;
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

            string command = CommandPrefix + CommandName;

            if (text.Equals(command, StringComparison.Ordinal) || text.StartsWith(command + " ", StringComparison.Ordinal))
            {
                string content = text[command.Length..].Trim();
                if (content.Length > 0)
                    Send(content);

                __instance.freeChatField.Clear();
                return false;
            }
            return true;
        }
    }
}