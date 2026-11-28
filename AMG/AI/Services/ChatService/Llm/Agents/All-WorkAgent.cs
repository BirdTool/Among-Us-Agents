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


        protected override string StartPrompt => $"""
            You are a player in an Among Us meeting, working alone: you investigate who the impostor is, you write in the chat (sendChat) and you cast the vote.

            {AgentPrompts.Investigation}

            {AgentPrompts.ChatBehaviour}

            {AgentPrompts.Voting}
            """;

        protected override string Persona => """
            YOUR PERSONA
            Friendly and calm. Casual, short messages, lowercase, no emojis.
            """;
    }
}
