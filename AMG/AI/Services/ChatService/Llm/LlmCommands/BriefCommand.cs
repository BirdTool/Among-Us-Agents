using System.Linq;
using AMG.AI.Services.ChatService.Llm.Agents;
using AMG.Enums.LLMEnums;

namespace AMG.AI.Services.ChatService.Llm.LlmCommands
{
    [LlmCommand.Create(Name = "brief", Type = LlmCommandTypeEnum.Private_Action,
        Explanation = "Tell the communicator what to get across in chat. args: [intent, facts]. intent: a short word for what you want it to do (for example question, accuse, defend, vouch, agree, deny, ask, report, greet, reply, or anything else that fits). Use clarify when you only need the communicator to check the chat and report back: it says nothing in the chat. facts: what you know, so it can say it. Never the words to say: the communicator chooses them.",
        AllowedModels = new[] { typeof(DetectiveAgent) })]
    public class BriefCommand : CommandClass
    {
        private const int MaxIntentLength = 30;
        private const int MaxFactsLength = 500;

        public override (bool success, string message) Execute(LlmCommandContext ctx, string[] args)
        {
            string intent = args.Length > 0 ? args[0].Trim() : string.Empty;
            if (intent.Length == 0 || intent.Length > MaxIntentLength)
                return (false, $"error: the first arg must be a short intent (up to {MaxIntentLength} characters), like accuse or question.");

            string facts = string.Join(" ", args.Skip(1)).Trim();
            if (facts.Length == 0) return (false, "error: facts are empty.");
            if (facts.Length > MaxFactsLength) facts = facts[..MaxFactsLength];

            return ctx.Service.SendToAgent(ctx.Agent, nameof(CommunicatorAgent), intent.ToLowerInvariant(), facts)
                ? (true, "ok: the communicator will read it.")
                : (false, "error: the communicator is not in this meeting, or it is you.");
        }
    }
}
