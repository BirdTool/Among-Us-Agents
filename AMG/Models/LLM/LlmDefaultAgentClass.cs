using System.Collections.Generic;
using System.Text;
using AMG.AI.Mind.LLMAgentBrain;
using AMG.AI.Services.ChatService.Llm;
using AMG.AI.Services.MeetingService;
using AMG.Utilities;

namespace AMG.Models.LLM
{
    public class LlmDefaultAgentClass
    {
        protected readonly List<LlmMessage> Messages = [];
        public virtual int MaxOutputTokens { get; } = 1024;
        protected string StartPrompt;
        protected virtual string LlmPlayerInfo(LLMAgentBrain brain) => $"You: Name: {brain.BaseName}, Colour: {Utils.Colours.ColourOf(brain.Agent)}, Role: {brain.Agent.Data.Role}";
        protected string Persona;

        protected virtual string GetCommandsInfo(LlmChatService service)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Commands. You may send up to {LlmChatService.MaxCommandsPerStep} per reply; they run in order:");
            foreach (var c in LlmCommandRegistry.All)
                sb.AppendLine($"{c.Name} - {c.Explanation}");

            return sb.ToString();
        }

        public List<LlmMessage> GetMessages() => Messages;
        public void AddMessage(LlmMessage message) => Messages.Add(message);

        public virtual string BuildPrompt(LlmCommandContext ctx)
        {
            var sb = new StringBuilder();
            sb.AppendLine(StartPrompt);
            sb.AppendLine(Persona);
            sb.AppendLine(LlmPlayerInfo(ctx.Brain));
            sb.AppendLine(GetCommandsInfo(ctx.Service));
            sb.AppendLine(MeetingTranscriptService.GetCurrentMeetingTranscriptString());
            
            return sb.ToString();
        }
    }
}