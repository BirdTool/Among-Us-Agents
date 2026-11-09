using AMG.AI.Mind.LLMAgentBrain;

namespace AMG.AI.Services.ChatService.Llm
{
    public class LlmChatService(LLMAgentBrain brain)
    {
        private readonly LLMAgentBrain Brain = brain;
    }
}