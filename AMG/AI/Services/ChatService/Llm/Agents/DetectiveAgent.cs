using AMG.Enums.LLMEnums;
using AMG.Models.LLM;

namespace AMG.AI.Services.ChatService.Llm.Agents
{
    public class DetectiveAgent(LlmEffortEnum effort) : LlmDefaultAgentClass(effort)
    {
        public override int MaxOutputTokens { get; } = effort switch
        {
            LlmEffortEnum.Low => 2048,
            LlmEffortEnum.Medium => 4096,
            LlmEffortEnum.High => 8192,
            _ => 4096
        };


        // Gives the communicator time to report the round before the detective starts.
        public override float StartDelaySeconds => 5f;

        protected override string StartPrompt => """
            TODO: you are the detective: you find out who the impostor is.
            """;

        protected override string Persona => string.Empty;
    }
}
