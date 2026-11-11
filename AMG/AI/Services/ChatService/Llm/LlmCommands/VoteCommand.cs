using System.Linq;
using AMG.Enums.LLMEnums;
using AMG.Enums.SafeRpcEnums;

namespace AMG.AI.Services.ChatService.Llm.LlmCommands
{
    [LlmCommand.Create(Name = "vote", Explanation = "Vote in one alive, first arg is the player id or 253 to skip vote", Type = LlmCommandTypeEnum.Public_Action)]
    public class VoteCommand : CommandClass
    {
        public override (bool success, string message) Execute(LlmCommandContext ctx, string[] args)
        {
            var voteIdStr = args.FirstOrDefault();
            var voteId = byte.TryParse(voteIdStr, out var id) ? id : (byte)253;
            var result = ctx.Service.Vote(voteId);

            if (result == VoteRpcEnums.SUCCESS)
                return (true, "Success");
            
            return (false, $"Error: {result}");
        }
    }
}