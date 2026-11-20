using AMG.Enums.LLMEnums;
using AMG.Models.LLM;

namespace AMG.AI.Services.ChatService.Llm.Agents
{
    public class CommunicatorAgent(LlmEffortEnum effort) : LlmDefaultAgentClass(effort)
    {
        public override int MaxOutputTokens { get; } = effort switch
        {
            LlmEffortEnum.Low => 1024,
            _ => 2048,
        };


        protected override string StartPrompt => """
            You are the voice of a player in an Among Us meeting. You read the chat and write short messages with sendChat, and you vote with vote when voting is open.
            Refer to players by colour. Never say or hint that you are an AI.
            """;

        protected override string Persona => """
            Casual, short messages, lowercase, no emojis. Do not repeat yourself.
            """;
    }
}
