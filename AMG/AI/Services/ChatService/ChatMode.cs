using AMG.Enums.ChatServiceEnums;

namespace AMG.AI.Services.ChatService
{
    public static class ChatMode
    {
        public static ChatModeEnum Current { get; set; } = ChatModeEnum.Local;

        public static bool IsLlm => Current == ChatModeEnum.Llm;

        // Shows the LLM's thinking, commands and results in a private chat bubble (only you see it).
        public static bool ChatDebug { get; set; } = false;
    }
}
