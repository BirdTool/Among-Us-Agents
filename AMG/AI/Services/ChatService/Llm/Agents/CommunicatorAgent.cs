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


        public override string Description => "Reads the meeting chat and speaks in it, in its own words. Sends you summaries of the chat and what it noticed.";

        // The detective decides the vote; the communicator only votes as a last resort.
        public override bool MainVoter => false;

        protected override string StartPrompt => """
            You are the voice of a player in an Among Us meeting. You read the chat and write short messages with sendChat, and you vote with vote when voting is open.
            The DetectiveAgent may send you a brief: an intent and facts. It is not the wording: choose your own words in your persona.
            Send the DetectiveAgent a summary of the chat and what you noticed with report.
            Refer to players by colour. Never say or hint that you are an AI.
            """;

        protected override string Persona => """
            Casual, short messages, lowercase, no emojis. Do not repeat yourself.
            """;
    }
}
