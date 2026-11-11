using AMG.Enums.LLMEnums;
using AMG.Models.LLM;

namespace AMG.AI.Services.ChatService.Llm.Agents
{
    public class CommunicatorAgent(LlmEffortEnum effort) : LlmDefaultAgentClass
    {
        public override int MaxOutputTokens { get; } = effort switch
        {
            LlmEffortEnum.Low => 1024,
            _ => 2048,
        };
    }
}