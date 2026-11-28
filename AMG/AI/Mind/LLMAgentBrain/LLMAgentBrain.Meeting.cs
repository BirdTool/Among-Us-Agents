using AMG.AI.Services.ChatService.Llm;

namespace AMG.AI.Mind.LLMAgentBrain
{
    public partial class LLMAgentBrain
    {
        public LlmChatService _chatService { get; private set; }

        public void InstanceLlmChatService()
        {
            _chatService = new LlmChatService(this);
        }
    }
}