using System.Linq;
using AMG.AI.Services.ChatService.Llm.Agents;
using AMG.Enums.LLMEnums;

namespace AMG.AI.Services.ChatService.Llm.LlmCommands
{
    [LlmCommand.Create(Name = "report", Type = LlmCommandTypeEnum.Private_Action,
        Explanation = "Send the detective a summary of the chat and what you noticed. args: [summary]")]
    public class ReportCommand : CommandClass
    {
        public override (bool success, string message) Execute(LlmCommandContext ctx, string[] args)
        {
            string summary = string.Join(" ", args).Trim();
            if (summary.Length == 0) return (false, "error: summary is empty.");

            return ctx.Service.SendToAgent(ctx.Agent, nameof(DetectiveAgent), "report", summary)
                ? (true, "ok: the detective will read it.")
                : (false, "error: the detective is not in this meeting, or it is you.");
        }
    }
}
