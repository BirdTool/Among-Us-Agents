using AMG.Enums.LLMEnums;
using AMG.Models.LLM;

namespace AMG.AI.Services.ChatService.Llm.Agents
{
    public class AllWorkAgent(LlmEffortEnum effort) : LlmDefaultAgentClass
    {
        public override int MaxOutputTokens { get; } = effort switch
        {
            LlmEffortEnum.Low => 1024,
            LlmEffortEnum.Medium => 2048,
            LlmEffortEnum.High => 4096,
            _ => 2048
        };
    }
}   