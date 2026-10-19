using AMG.Enums.ChatServiceEnums;

namespace AMG.AI.Services.ChatService
{
    public static class ChatMode
    {
        public static ChatModeEnum Current { get; set; } = ChatModeEnum.Local;

        public static bool IsLlm => Current == ChatModeEnum.Llm;
    }
}
