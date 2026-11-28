using AMG.AI.Services.ChatService.Llm;
using AMG.Utilities;

namespace AMG.Modules.Commands
{
    // .persona <text>  sets the extra persona of the LLM agents that speak in chat
    // .persona         shows it
    // .persona clear   removes it
    public class PersonaCommand : ChatCommand
    {
        public const int MaxLength = 1500;

        public override string Name => "persona";
        public override string[] Aliases => ["pe"];

        private static void LogChat(string text)
        {
            if (Utils.IsFreePlay)
                PlayerControl.LocalPlayer.RpcSendChat(text);
            else
                Utils.AddChatPrivate(text);
        }

        public override void Execute(string args)
        {
            if (args.Length == 0)
            {
                string current = LlmChatService.CustomPersona;
                LogChat(string.IsNullOrWhiteSpace(current)
                    ? "No persona set. Use .persona <text> to set one, .persona clear to remove it."
                    : $"Persona ({current.Length} characters): {Preview(current)}");
                return;
            }

            if (args.Equals("clear", System.StringComparison.OrdinalIgnoreCase))
            {
                Save(string.Empty);
                LogChat("Persona removed.");
                return;
            }

            bool cut = args.Length > MaxLength;
            if (cut) args = args[..MaxLength];

            Save(args);
            LogChat($"Persona saved ({args.Length} characters{(cut ? $", cut to the {MaxLength} limit" : "")}). It applies from the next LLM turn.");
        }

        private static void Save(string persona)
        {
            LlmChatService.CustomPersona = persona;
            AMGPlugin.LlmPersona?.Value = persona;
        }

        private static string Preview(string text) => text.Length <= 200 ? text : text[..200] + "...";
    }
}
