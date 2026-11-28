using System;
using System.Collections.Generic;
using System.Linq;

namespace AMG.Modules.Commands
{
    public abstract class ChatCommand
    {
        public abstract string Name { get; }

        public virtual string[] Aliases => [];

        public abstract void Execute(string args);

        public bool Matches(string word)
            => word.Equals(Name, StringComparison.OrdinalIgnoreCase)
               || Aliases.Any(a => word.Equals(a, StringComparison.OrdinalIgnoreCase));
    }

    public static class ChatCommands
    {
        public const string Prefix = ".";

        public static readonly IReadOnlyList<ChatCommand> All =
        [
            new SendPrivateMessageCommand(),
            new PersonaCommand()
        ];

        public static bool TryParse(string text, out ChatCommand command, out string args)
        {
            command = null;
            args = string.Empty;

            if (string.IsNullOrEmpty(text) || !text.StartsWith(Prefix, StringComparison.Ordinal))
                return false;

            int space = text.IndexOfAny([' ', '\t', '\n']);
            string word = (space < 0 ? text : text[..space])[Prefix.Length..];

            command = All.FirstOrDefault(c => c.Matches(word));
            if (command == null) return false;

            args = space < 0 ? string.Empty : text[(space + 1)..].Trim();
            return true;
        }

        public static bool IsCommand(string text) => TryParse(text, out _, out _);
    }
}
