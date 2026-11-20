using System;
using System.Linq;
using AMG.Enums.LLMEnums;

namespace AMG.AI.Services.ChatService.Llm
{
    public static class LlmCommand
    {
        [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
        public sealed class CreateAttribute : Attribute
        {
            public string Name { get; set; }
            public string Explanation { get; set; }
            public LlmCommandTypeEnum Type { get; set; }

            // Agent models (classes in Agents/) that may use this command. null or empty = every model.
            public Type[] AllowedModels { get; set; }
        }
    }

    public abstract class CommandClass
    {
        public string Name { get; internal set; }
        public string Explanation { get; internal set; }
        public LlmCommandTypeEnum LlmCommandTypeEnum { get; internal set; }
        public Type[] AllowedModels { get; internal set; } = [];

        public bool CanBeUsedBy(AMG.Models.LLM.LlmDefaultAgentClass agent)
            => AllowedModels == null || AllowedModels.Length == 0 || AllowedModels.Any(t => t.IsInstanceOfType(agent));
 
        public abstract (bool success, string message) Execute(LlmCommandContext ctx, string[] args);
    }

    public sealed partial class LlmChatService
    {
        public bool TryExecute(string name, string[] args, out (bool success, string message) result)
        {
            result = (false, "Command not found");
            if (!LlmCommandRegistry.TryGet(name, out var cmd)) return false;
                result = cmd.Execute(new LlmCommandContext(this, Brain), args);
            return true;
        }
    }
}