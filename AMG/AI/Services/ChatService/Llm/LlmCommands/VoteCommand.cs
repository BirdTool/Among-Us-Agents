using System;
using System.Linq;
using AMG.AI.Services.ChatService.Llm.Agents;
using AMG.Enums.LLMEnums;
using AMG.Utilities;

namespace AMG.AI.Services.ChatService.Llm.LlmCommands
{
    [LlmCommand.Create(Name = "vote", Type = LlmCommandTypeEnum.Public_Action,
        Explanation = "Vote for an alive player. args: [player, certain]. player: the player id (or the colour), or 253 to skip. A vote is final. Early votes are refused unless you add \"certain\" as the second arg, which is only for when you are 100% sure who the impostor is.",
        AllowedModels = new[] { typeof(CommunicatorAgent), typeof(AllWorkAgent), typeof(DetectiveAgent) })]
    public class VoteCommand : CommandClass
    {
        public override (bool success, string message) Execute(LlmCommandContext ctx, string[] args)
        {
            string who = args.FirstOrDefault()?.Trim() ?? string.Empty;
            if (!TryResolve(who, out byte target, out string error))
                return (false, error);

            bool certain = args.Skip(1).Any(a => a.Trim().Equals("certain", StringComparison.OrdinalIgnoreCase));
            return ctx.Service.TryVote(ctx.Agent, target, certain);
        }

        // The id, a colour, or 253. Anything else is an error: it must never turn into a skip by accident.
        private static bool TryResolve(string who, out byte target, out string error)
        {
            error = null;
            target = Utils.SkipVotePlayerId;

            if (who.Length == 0)
            {
                error = "error: missing player. Use the player id, or 253 to skip.";
                return false;
            }

            if (byte.TryParse(who, out var id))
            {
                if (id >= 250) return true;   // skip

                if (AMGPlayers.GetPlayer(id) is { IsAlive: true })
                {
                    target = id;
                    return true;
                }

                error = $"error: no alive player has Id {id}.";
                return false;
            }

            var byColour = AMGPlayers.AlivePlayers.FirstOrDefault(p => p.PlayerColour.Equals(who, StringComparison.OrdinalIgnoreCase));
            if (byColour != null)
            {
                target = byColour.PlayerId;
                return true;
            }

            error = $"error: \"{who}\" is not an alive player. Use the player id, or 253 to skip.";
            return false;
        }
    }
}
