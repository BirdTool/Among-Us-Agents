using System;
using System.Linq;
using System.Text.RegularExpressions;
using AMG.Enums.LLMEnums;
using AMG.Enums.SafeRpcEnums;

namespace AMG.AI.Services.ChatService.Llm.LlmCommands
{
    [LlmCommand.Create(Name = "sendChat", Explanation = "Send a message on chat within 0-100 characters, args: message", Type = LlmCommandTypeEnum.Public_Action)]
    public class SendChatCommand : CommandClass
    {
        private const int MaxChatLength = 100;

        public override (bool success, string message) Execute(LlmCommandContext ctx, string[] args)
        {
            var argText = args.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(argText)) { return (false, "error: text is empty."); }

            // A message over the game's limit is shortened instead of being refused (the model would lose its turn)
            string text = FitToChat(argText.Trim(), out bool shortened);

            var check = ctx.Service.CheckSay(text);
            // cooldown is not an error here: PumpOutbox keeps the message queued until it can go out
            if (check != ChatRpcEnums.SUCCESS && check != ChatRpcEnums.ERROR_InCooldown) { return (true, $"error: {check}"); }
            ctx.Service.AddOutbox(text);
            return shortened
                ? (true, $"ok: queued, but it was over {MaxChatLength} characters and was shortened to: \"{text}\". Keep messages short.")
                : (true, "ok: queued, it will appear in the chat in a few seconds.");
        }

        private static (Regex Pattern, string Short) Abbr(string from, string to) =>
            (new Regex($@"\b{Regex.Escape(from).Replace("'", "['’]")}\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), to);

        private static readonly (Regex Pattern, string Short)[] ChatAbbreviations =
        [
            // phrases
            Abbr("are you", "r u"), Abbr("you are", "u r"), Abbr("by the way", "btw"),
            Abbr("right now", "rn"), Abbr("hard clear", "hc"), Abbr("vent clear", "vc"),
            Abbr("i don't know", "idk"), Abbr("i think", "i thk"), Abbr("what do you", "wdu"),
            Abbr("upper engine", "upper eng"), Abbr("lower engine", "lower eng"),

            // contractions and pronouns
            Abbr("you're", "ur"), Abbr("your", "ur"), Abbr("you", "u"), Abbr("are", "r"),
            Abbr("don't", "dont"), Abbr("didn't", "didnt"), Abbr("doesn't", "doesnt"),
            Abbr("can't", "cant"), Abbr("i'm", "im"), Abbr("that's", "thats"), Abbr("it's", "its"),

            // common words
            Abbr("because", "bc"), Abbr("before", "b4"), Abbr("about", "abt"), Abbr("people", "ppl"),
            Abbr("please", "pls"), Abbr("thanks", "thx"), Abbr("okay", "ok"), Abbr("really", "rly"),
            Abbr("why", "y"), Abbr("something", "smth"),Abbr("anyone", "any1"), Abbr("everyone", "every1"),
             Abbr("someone", "some1"),

            // game words
            Abbr("suspicious", "sus"), Abbr("impostor", "imp"), Abbr("crewmate", "crew"),
            Abbr("cafeteria", "cafe"), Abbr("electrical", "elec"), Abbr("navigation", "nav"),
            Abbr("security", "sec"), Abbr("weapons", "weps"),
        ];

        private static readonly string[] DanglingWords =
            ["and", "or", "but", "so", "if", "the", "a", "an", "to", "of", "in", "on", "at", "is", "was", "that", "w/"];

        private static string FitToChat(string text, out bool shortened)
        {
            shortened = text.Length > MaxChatLength;
            if (!shortened) return text;

            foreach (var (pattern, abbreviation) in ChatAbbreviations)
            {
                text = pattern.Replace(text, abbreviation);
                if (text.Length <= MaxChatLength) return text;
            }

            string cut = text[..MaxChatLength];

            int end = cut.LastIndexOfAny(['.', '!', '?']);
            if (end >= 40) return cut[..(end + 1)];

            int clause = cut.LastIndexOfAny([',', ';']);
            if (clause >= 40) return cut[..clause];

            int space = cut.LastIndexOf(' ');
            if (space >= 40) cut = cut[..space];

            return TrimDanglingWords(cut);
        }

        private static string TrimDanglingWords(string s)
        {
            s = s.TrimEnd(',', ';', ' ');
            while (true)
            {
                int space = s.LastIndexOf(' ');
                if (space < 0) return s;

                string last = s[(space + 1)..].ToLowerInvariant();
                if (Array.IndexOf(DanglingWords, last) < 0) return s;

                s = s[..space].TrimEnd(',', ';', ' ');
            }
        }
    }
}