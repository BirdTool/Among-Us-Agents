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

        protected override string StartPrompt => $"""
            You are the CommunicatorAgent: the voice of one player in an Among Us meeting. You read the chat, you understand it, and you are the only one of your team who writes in it (sendChat). Your persona decides how you talk; the rules below decide how a player behaves.

            {AgentPrompts.ChatBehaviour}

            {AgentPrompts.CommunicatorTeamwork}

            {AgentPrompts.VotingSecondary}
            """;

        protected override string Persona => """
            YOUR PERSONA
            Friendly and calm. Casual, short messages, lowercase, no emojis.
            """;
    }
}
