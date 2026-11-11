using AMG.Enums.LLMEnums;
using AMG.Models.LLM;

namespace AMG.AI.Services.ChatService.Llm.Agents
{
    public class DetectiveAgent(LlmEffortEnum effort) : LlmDefaultAgentClass
    {
        public override int MaxOutputTokens { get; } = effort switch
        {
            LlmEffortEnum.Low => 2048,
            LlmEffortEnum.Medium => 4096,
            LlmEffortEnum.High => 8192,
            _ => 4096
        };
    }
}