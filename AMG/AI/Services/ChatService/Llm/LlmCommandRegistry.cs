using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AMG.AI.Mind.LLMAgentBrain;
using AMG.Utilities;

namespace AMG.AI.Services.ChatService.Llm
{
    public sealed class LlmCommandContext(LlmChatService service, LLMAgentBrain brain)
    {
        public LlmChatService Service { get; } = service;
        public LLMAgentBrain Brain { get; } = brain;
    }

    public static class LlmCommandRegistry
    {
        private static readonly Dictionary<string, CommandClass> Commands =
            new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<CommandClass> All => Commands.Values;

        public static void RegisterAll(Assembly assembly)
        {
            Commands.Clear();

            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = [.. e.Types.Where(t => t != null)]; }

            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<LlmCommand.CreateAttribute>(false);
                if (attr == null) continue;

                if (!typeof(CommandClass).IsAssignableFrom(type) || type.IsAbstract)
                {
                    LogManager.LogWarning($"[LlmCommand] {type.Name} tem o atributo mas não herda de CommandClass.");
                    continue;
                }

                var cmd = (CommandClass)Activator.CreateInstance(type);
                cmd.Name = attr.Name ?? type.Name;
                cmd.Explanation = attr.Explanation ?? "";
                cmd.LlmCommandTypeEnum = attr.Type;

                if (!Commands.TryAdd(cmd.Name, cmd))
                    LogManager.LogWarning($"[LlmCommand] Nome duplicado: {cmd.Name}");
            }
        }

        public static bool TryGet(string name, out CommandClass cmd) =>
            Commands.TryGetValue(name, out cmd);
    }
}