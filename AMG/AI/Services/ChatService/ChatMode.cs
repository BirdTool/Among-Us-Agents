using AMG.Enums.ChatServiceEnums;

namespace AMG.AI.Services.ChatService
{
    public static class ChatMode
    {
        public static ChatModeEnum Current { get; set; } = ChatModeEnum.Local;

        public static bool IsLlm => Current == ChatModeEnum.Llm;

        // Shows the LLM's thinking, commands and results in a private chat bubble (only you see it).
        public static bool ChatDebug { get; set; } = false;

        // The LLM prompts are built and logged, but no request is sent (nothing is spent). Every answer is an empty turn.
        public static bool LlmDryRun { get; set; } = false;
    }
}
