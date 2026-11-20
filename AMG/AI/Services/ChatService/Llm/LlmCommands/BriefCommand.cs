using System;
using System.Linq;
using AMG.AI.Services.ChatService.Llm.Agents;
using AMG.Enums.ChatServiceEnums;
using AMG.Enums.LLMEnums;

namespace AMG.AI.Services.ChatService.Llm.LlmCommands
{
    [LlmCommand.Create(Name = "brief", Type = LlmCommandTypeEnum.Private_Action,
        Explanation = "Tell the communicator what to get across. args: [intent, facts]. intent is one of: Question, Accuse, Defend, Vouch, Confirm, Deny, Request, Report, Greet, Reply. facts: what you know. Never the words to say.")]
    public class BriefCommand : CommandClass
    {
        public override (bool success, string message) Execute(LlmCommandContext ctx, string[] args)
        {
            if (args.Length == 0 || !Enum.TryParse(args[0].Trim(), true, out ChatIntentEnum intent) || !Enum.IsDefined(intent))
                return (false, $"error: the first arg must be an intent: {string.Join(", ", Enum.GetNames(typeof(ChatIntentEnum)))}.");

            string facts = string.Join(" ", args.Skip(1)).Trim();
            if (facts.Length == 0) return (false, "error: facts are empty.");

            return ctx.Service.SendToAgent(ctx.Agent, nameof(CommunicatorAgent), intent.ToString(), facts)
                ? (true, "ok: the communicator will read it.")
                : (false, "error: the communicator is not in this meeting, or it is you.");
        }
    }
}
