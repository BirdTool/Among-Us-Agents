using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Services.ChatService.Llm
{
    // How the agent talks: players are always referred to by colour, and the conversation rules that keep it calm.
    public sealed partial class LlmMeetingSession
    {
        // The game writes colours as "(Rose)": keep only the word.
        internal static string Colour(NetworkedPlayerInfo data)
        {
            string c = data?.ColorName?.Trim().Trim('(', ')').Trim();
            return string.IsNullOrEmpty(c) ? "Unknown" : c;
        }

        internal static string Colour(PlayerControl player) => Colour(player?.Data);

        internal static string ColourOf(byte playerId)
        {
            string c = Colour(Utils.Players.GetPlayerByPlayerId(playerId));
            return c == "Unknown" ? $"player {playerId}" : c;
        }

        private const int MaxChatLength = 100;

        private const RegexOptions Flags = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        private static readonly Regex RevealRegex = new(@"\b(?:developer|devs?|ai|a\.i\.|bots?|llm|language model|npc|chatgpt|gemini|claude|programmed)\b", Flags);
        private static readonly Regex VoteAnnounceRegex = new(@"\b(?:i|im|i['’]m|ill|i['’]ll|gonna|gotta)\b[^.?!]{0,12}\bvot(?:e|ing)\b|\bmy vote\b|\bvoting\s+(?:for\s+)?(?!is\b|time\b|phase\b|open\b|ends?\b|now\b|started\b)\w+", Flags);
        private static readonly Regex WhereRegex = new(@"\bwh?ere\b|\bwhr\b|\bwher\b", Flags);
        private static readonly Regex AccuseRegex = new(@"\b(?:sus|vent(?:ed|ing|s)?|saw|seen|kill(?:ed|ing|s)?|liar|lying|lied|imp(?:ostor)?|fake(?:d|s)?|accus\w*)\b", Flags);
        private static readonly Regex FirstPersonSaw = new(@"\b(?:i|ive|i['’]ve)\s+(?:personally\s+|just\s+|actually\s+|also\s+|clearly\s+)?(?:saw|seen|watched|witnessed)\b|\bsaw it\b", Flags);
        private static readonly Regex VentWord = new(@"\bvent(?:s|ed|ing)?\b", Flags);
        private static readonly Regex KillWord = new(@"\b(?:kill(?:ed|ing|s)?|murder(?:ed)?|stab(?:bed)?)\b", Flags);

        // Rules the model keeps breaking even when told: they are checked here, and a break sends the error
        // back to the model so it can say it differently. Returns null when the message is fine.
        private string ValidateOutgoing(string text)
        {
            if (RevealRegex.IsMatch(text) && !_directives.Any(d => RevealRegex.IsMatch(d.Text)))
                return "error: never mention a developer, instructions, or being an AI/bot in the chat. Say it as your own opinion.";

            if (VoteAnnounceRegex.IsMatch(text) && !text.Contains('?'))
                return "error: do not announce your vote. Say your suspicion and the reason instead; cast the vote silently with the vote command.";

            if (WhereRegex.IsMatch(text) && text.Contains('?'))
            {
                if (_askedWhere)
                    return "error: you already asked 'where' in this meeting. Drop it and do not ask it again.";
                if (AccuseRegex.IsMatch(text) || Time.time - _lastAccusationAt < 30f)
                    return "error: do not ask 'where' while or right after accusing someone. Either accuse or ask.";
            }

            return FindUnseenClaim(text);
        }

        // "I saw X vent / kill" is only allowed when the agent's own observations say so.
        private string FindUnseenClaim(string text)
        {
            if (!FirstPersonSaw.IsMatch(text) || _brain is not StructuredAgentBrain brain) return null;

            bool vent = VentWord.IsMatch(text), kill = KillWord.IsMatch(text);
            if (!vent && !kill) return null;

            foreach (var pl in GameData.Instance.AllPlayers)
            {
                if (pl.PlayerId == brain.AgentId) continue;

                string colour = Colour(pl);
                if (!Regex.IsMatch(text, $@"\b{Regex.Escape(colour)}\b", Flags)) continue;

                var memory = brain.GetOrCreateMemory(pl.PlayerId);
                if (vent && !memory.SawVenting)
                    return $"error: your observations do not show that you saw {colour} use a vent. Only say what you really saw; if you only heard it, say who told you.";
                if (kill && !memory.SawKilling)
                    return $"error: your observations do not show that you saw {colour} kill. Only say what you really saw; if you only heard it, say who told you.";
            }

            return null;
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

        private static string GetPlayerNames()
        {
            var sb = new StringBuilder("=== Player names (id, colour, name) ===\n");
            foreach (var p in GameData.Instance.AllPlayers)
                sb.AppendLine($"- {p.PlayerId} {Colour(p)}: {Clean(p.PlayerName)}");
            return sb.ToString();
        }

        private void AppendBehaviour(StringBuilder sb, TierProfile profile)
        {
            if (profile.Lobby) return;

            sb.AppendLine("Players are identified by COLOUR. Always call, ask and accuse players by colour, never by their name. Names are not shown: if someone writes a name you do not know, use get_player_names (when you have it) to find the colour, and do not guess. Commands still use the player id.");

            if (!profile.TrustChat)
            {
                sb.AppendLine("How to behave in the meeting:");
                sb.AppendLine("- Stay calm and keep it short. Speak only when you have something new: a fact you saw, an answer to someone, a claim, or your opinion for the vote. Otherwise use wait. Talk only about the meeting (who died, who is suspicious, votes), never about anything else.");
                sb.AppendLine("- Answer a question addressed to you before asking anything yourself.");
                sb.AppendLine("- Ask at most ONE question per message, and never ask the same thing twice. If nobody answers your question, drop it: do not repeat it and do not pressure anyone for an answer.");
                sb.AppendLine("- Do not ask where the body was if it was already said or if someone already asked. Where a body was found is only known by whoever saw it; a missing answer is not suspicious by itself.");
                sb.AppendLine("- Do not ask who was nearby, who was with someone, or what someone was doing there by default. Ask only when a story contradicts something you saw yourself or what another player said. When two players report the same body or confirm each other, believe them instead of interrogating either one.");
                sb.AppendLine("- A 'poof' is the Phantom's vanish ability (the Phantom is an impostor role). If someone says they saw a poof, do not ask for where or when: weigh it together with everything else against the colour that did it, remembering it can also be a joke or a mistake.");
                sb.AppendLine("- If you personally saw something, say it once, clearly, with the colour and the room, and do not repeat it afterwards.");
                sb.AppendLine("- Only say you saw something if it appears in your notes marked [seen] or in your observations. Never invent a sighting: if you only heard it, say who said it; if nobody has proof, say you do not know.");
                sb.AppendLine("- A body report and the emergency button are two different things. The meeting facts say which one it was; if they do not say, do not claim either.");
                sb.AppendLine("- Never announce who you are going to vote for and never write \"I'm voting X\". Share your suspicion and the reason, and cast the vote silently with the vote command.");
                sb.AppendLine("- When you accuse someone, do not add a question in the same message, and never ask \"where\" about a player you are accusing. Asking where is only for a body nobody has located yet, and only once.");
                sb.AppendLine("- Never mention a developer, instructions, or that you are an AI, a bot or a program, unless the developer explicitly orders you to say it. What the developer tells you about the game is something you simply know: say it as your own opinion.");
                sb.AppendLine();
            }
            else
                sb.AppendLine();
        }
    }
}
