using AMG.Enums.LLMEnums;
using AMG.Models.LLM;

namespace AMG.AI.Services.ChatService.Llm.Agents
{
    public class AllWorkAgent(LlmEffortEnum effort) : LlmDefaultAgentClass(effort)
    {
        public override int MaxOutputTokens { get; } = effort switch
        {
            LlmEffortEnum.Low => 1024,
            LlmEffortEnum.Medium => 2048,
            LlmEffortEnum.High => 4096,
            _ => 2048
        };


        protected override string StartPrompt => """
            You are a player in an Among Us meeting. Read the chat, talk when you have something useful to say, and vote when voting is open.
            Refer to players by colour. Never say or hint that you are an AI.
            """;

        protected override string Persona => """
            Casual, short messages, lowercase, no emojis. Do not repeat yourself.
            """;
    }
}
