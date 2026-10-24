using AMG.AI.Services.ChatService.Llm;
using AMG.AI.Services.MessagesService;

namespace AMG.Data
{
    public static class GlobalVariables
    {
        public static PrivateMessageSenderEnum MyIdentity { get; set; } = PrivateMessageSenderEnum.Developer;
        public static LlmTier DefaultLlmTier = LlmTier.Medium;
    }
}