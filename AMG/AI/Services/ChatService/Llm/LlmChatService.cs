using AMG.AI.Mind.LLMAgentBrain;

namespace AMG.AI.Services.ChatService.Llm
{
    public sealed partial class LlmChatService(LLMAgentBrain brain)
    {
        private readonly LLMAgentBrain Brain = brain;
    }
}