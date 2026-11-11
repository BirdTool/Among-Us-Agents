using System;
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
        }
    }

    public abstract class CommandClass
    {
        public string Name { get; internal set; }
        public string Explanation { get; internal set; }
        public LlmCommandTypeEnum LlmCommandTypeEnum { get; internal set; }
 
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