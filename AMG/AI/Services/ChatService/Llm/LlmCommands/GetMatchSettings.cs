using System.Text;
using AMG.AI.Services.ChatService.Llm.Agents;
using AMG.Enums.LLMEnums;
using AmongUs.GameOptions;

namespace AMG.AI.Services.ChatService.Llm.LlmCommands
{
    [LlmCommand.Create(Name = "getMatchSettings", Type = LlmCommandTypeEnum.Private_Action,
        Explanation = "Get the current match settings.",
        AllowedModels = new[] { typeof(AllWorkAgent), typeof(DetectiveAgent) })]
    public class GetMatchSettings : CommandClass
    {
        public override (bool success, string message) Execute(LlmCommandContext ctx, string[] args)
        {
            var options = GameOptionsManager.Instance?.currentNormalGameOptions;
            if (options == null)
                return (false, "Unable to retrieve match settings at the moment.");

            var roleOptions = options.RoleOptions;
            string[] killDistances = ["Short", "Medium", "Long"];
            string killDistanceText = options.KillDistance >= 0 && options.KillDistance < killDistances.Length
                ? killDistances[options.KillDistance]
                : options.KillDistance.ToString();

            MapNames? map = ShipStatus.Instance.Type switch
            {
                ShipStatus.MapType.Ship => MapNames.Skeld,
                ShipStatus.MapType.Hq => MapNames.MiraHQ,
                ShipStatus.MapType.Pb => MapNames.Polus,
                ShipStatus.MapType.Fungle => MapNames.Fungle,
                _ => null
            };

            var sb = new StringBuilder();
            sb.AppendLine("=== Match Settings ===");
            sb.AppendLine($"Current Map: {map.ToString() ?? "Unknown"}");
            sb.AppendLine($"Impostors: {options.NumImpostors}");
            sb.AppendLine($"Kill Cooldown: {options.KillCooldown}s");
            sb.AppendLine($"Kill Distance: {killDistanceText}");
            sb.AppendLine();
            sb.AppendLine("=== Roles (Count / Chance) ===");

            var crewRolesToCheck = new[]
            {
                (RoleTypes.Engineer, "Engineer"),
                (RoleTypes.Scientist, "Scientist"),
                (RoleTypes.Tracker, "Tracker"),
                (RoleTypes.Noisemaker, "Noisemaker"),
                (RoleTypes.Detective, "Detective"),
                (RoleTypes.Judge, "Judge"),
            };

            var impRolesToCheck = new[]
            {
                (RoleTypes.Shapeshifter, "Shapeshifter"),
                (RoleTypes.Viper, "Viper"),
                (RoleTypes.Phantom, "Phantom")
            };

            sb.AppendLine("=== Crew Roles (Count / Chance) ===");

            foreach (var (roleType, roleName) in crewRolesToCheck)
            {
                int count = roleOptions.GetNumPerGame(roleType);
                int chance = roleOptions.GetChancePerGame(roleType);
                sb.AppendLine($"- {roleName}: {count} ({chance}%)");
            }

            sb.AppendLine("=== Impostor Roles (Count / Chance) ===");

            foreach (var (roleType, roleName) in impRolesToCheck)
            {
                int count = roleOptions.GetNumPerGame(roleType);
                int chance = roleOptions.GetChancePerGame(roleType);
                sb.AppendLine($"- {roleName}: {count} ({chance}%)");
            }

            sb.AppendLine("=== Other Settings ===");
            sb.AppendLine($"Engineer max time in vents: {options.GetFloat(FloatOptionNames.EngineerInVentMaxTime)}");
            sb.AppendLine($"Engineer time to vent again: {options.GetFloat(FloatOptionNames.EngineerCooldown)}");

            return (true, sb.ToString().TrimEnd());
        }
    }
}