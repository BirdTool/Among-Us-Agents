using System;
using AMG.Utilities;
using HarmonyLib;
using UnityEngine;

namespace AMG.Modules.Commands
{
    [HarmonyPatch]
    public static class ChatCommandPatches
    {
        public const int ChatCharacterLimit = 100;

        [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
        [HarmonyPrefix]
        public static bool SendChat_Prefix(ChatController __instance)
        {
            string text = __instance.freeChatField.textArea.text;

            if (ChatCommands.TryParse(text, out var command, out string args))
            {
                try
                {
                    command.Execute(args);
                }
                catch (Exception e)
                {
                    LogManager.LogWarning($"Chat command '{command.Name}' failed: {e}");
                }

                __instance.freeChatField.Clear();
                return false;
            }

            if (text.Length > ChatCharacterLimit)
            {
                Utils.AddChatPrivate($"Message too long ({text.Length}/{ChatCharacterLimit}). Shorten it.");
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
        [HarmonyPostfix]
        public static void Update_Postfix(ChatController __instance) => ChatBoxStyle.Apply(__instance);
    }

    // While the typed text is a valid command the chat box turns grey and has no character limit.
    // Everything that touches the game's chat box is here: if a member is renamed in a game update, fix it in this class.
    public static class ChatBoxStyle
    {
        private static readonly Color Gray = new(0.6f, 0.6f, 0.6f, 1f);

        private static IntPtr _chat; // the ChatController the values below belong to
        private static bool _styled;
        private static int _vanillaLimit = ChatCommandPatches.ChatCharacterLimit;
        private static Color _vanillaBackground = Color.white;
        private static Color _vanillaCount = Color.black;

        public static void Apply(ChatController chat)
        {
            var field = chat.freeChatField;
            if (field == null || field.textArea == null) return;

            if (chat.Pointer != _chat)
            {
                _chat = chat.Pointer;
                _styled = false;
            }

            string text = field.textArea.text;
            bool command = ChatCommands.IsCommand(text);

            if (command)
            {
                _styled = true;
                field.textArea.characterLimit = int.MaxValue;
                field.background.color = Gray;
                field.charCountText.text = text.Length.ToString();
                field.charCountText.color = Gray;
            }
            else if (_styled)
            {
                _styled = false;
                field.textArea.characterLimit = _vanillaLimit;
                field.background.color = _vanillaBackground;
                field.charCountText.text = $"{text.Length}/{_vanillaLimit}";
                field.charCountText.color = _vanillaCount;
            }
            else
            {
                // untouched box: remember how the game draws it
                _vanillaLimit = field.textArea.characterLimit;
                _vanillaBackground = field.background.color;
                _vanillaCount = field.charCountText.color;
            }
        }
    }
}
