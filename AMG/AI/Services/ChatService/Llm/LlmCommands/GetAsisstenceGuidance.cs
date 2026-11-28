using System;
using System.Linq;
using AMG.AI.Services.ChatService.Llm.Agents;
using AMG.Enums.LLMEnums;
using AMG.Interfaces;
using AMG.Models.Guidances;
using AMG.Utilities;

namespace AMG.AI.Services.ChatService.Llm.LlmCommands
{
    [LlmCommand.Create(Name = "getGuidance", Type = LlmCommandTypeEnum.Private_Action,
        Explanation = "Ask your assistant for guidance. args: [guidanceType] [typeArgs]. "
            + "Examples: [body] [bodyLocationRoom] (suspicious players around a body in that room) | [suspicious] (the assistant's view of suspicious players) | [impostor] (impostor only: what to do as an impostor)",
        AllowedModels = new[] { typeof(AllWorkAgent), typeof(DetectiveAgent) })]
    public class GetAsisstenceGuidanceCommand : CommandClass
    {
        public override (bool success, string message) Execute(LlmCommandContext ctx, string[] args)
        {
            if (args.Length == 0)
                return (false, "error: missing guidance type.");

            string[] availableTypes = ["body", "suspicious", "impostor"];
            string guidanceType = args[0].Trim().ToLowerInvariant();
            if (!availableTypes.Contains(guidanceType))
                return (false, "error: invalid guidance type.");

            IGuidance guidance;

            switch (guidanceType)
            {
                case "body":
                    if (args.Length < 2)
                        return (false, "error: missing body location.");
                    // the room may come in one arg ("upper engine") or several ("upper", "engine")
                    if (!Utils.TryGetRoomFromText(string.Join(" ", args.Skip(1)), out SystemTypes room))
                        return (false, "error: no known room in the body location.");
                    guidance = new MeetingBodyLocation(room);
                    break;
                case "suspicious":
                    guidance = new SuspiciousPlayers();
                    break;
                case "impostor":
                    guidance = new MeetingImpostorGuidance();
                    break;
                default:
                    // a type was added to availableTypes without a case here
                    return (false, "error: guidance type not implemented yet.");
            }

            string text = ctx.Brain.Assistence?.Guidance(guidance);
            return string.IsNullOrWhiteSpace(text)
                ? (true, "ok: your assistant has nothing to add right now.")
                : (true, text);
        }
    }
}
