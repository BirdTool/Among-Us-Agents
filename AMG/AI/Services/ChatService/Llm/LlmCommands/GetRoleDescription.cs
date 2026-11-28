using System.Linq;
using AMG.AI.Services.ChatService.Llm.Agents;
using AMG.Enums.LLMEnums;

namespace AMG.AI.Services.ChatService.Llm.LlmCommands
{
    [LlmCommand.Create(Name = "getRoleDescription", Type = LlmCommandTypeEnum.Private_Action,
        Explanation = "Get the description of a role. args: [roleName]")]
    public class GetRoleDescriptionCommand : CommandClass
    {
        public override (bool success, string message) Execute(LlmCommandContext ctx, string[] args)
        {
            var commandStr = args.FirstOrDefault();
            if (string.IsNullOrEmpty(commandStr)) return (false, "error: command is empty.");
            
            var command = ctx.Service.GetRoleDescription(commandStr);
            if (command.StartsWith("Unknown"))
                return (false, command);
            return (true, command);
        }
    }

    [LlmCommand.Create(Name = "getAllRolesDescription", Type = LlmCommandTypeEnum.Private_Action,
        Explanation = "Get the description of all roles. args: []")]
    public class GetAllRolesDescription : CommandClass
    {
        public override (bool success, string message) Execute(LlmCommandContext ctx, string[] args)
        {
            var result = LlmChatService.GetAllRolesDescription();
            return (true, result);
        }
    }
}
