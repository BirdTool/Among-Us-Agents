using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AMG.AI.Services.ChatService
{
    public static class ChatLexicon
    {
        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length + 4);
            char last = '\0';
            int run = 0;

            foreach (char c in decomposed)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat == UnicodeCategory.NonSpacingMark) continue;
                if (c == '\'' || c == '’' || c == '`') continue;

                char outc;
                if (char.IsLetterOrDigit(c)) outc = c;
                else if (c == '?') { sb.Append(" ? "); last = '\0'; run = 0; continue; }
                else outc = ' ';

                if (outc == last) { run++; if (run >= 3) continue; }
                else { last = outc; run = 1; }

                sb.Append(outc);
            }

            return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        public static readonly HashSet<string> Fillers =
        [
            "in", "at", "near", "by", "the", "a", "around", "its", "it", "is", "was", "from", "outside", "inside",
            "next", "to", "rn", "right", "now", "tho", "though", "yeah", "yea", "yep", "uh", "um", "lol", "and",
            "room", "area", "bro", "pls", "plz", "yes", "ye",
        ];

        public static readonly HashSet<string> SelfWords = ["i", "im", "me", "my", "myself", "ive"];

        public static readonly HashSet<string> BodyWords = ["body", "bodies", "corpse", "dead", "found", "died", "report", "reported"];

        // "red didnt vent" must never match the template "{target} vented": negation has to agree.
        public static readonly HashSet<string> Negators =
        [
            "not", "no", "nope", "never", "dont", "didnt", "doesnt", "wasnt", "werent", "isnt", "arent",
            "cant", "cannot", "wont", "couldnt", "neither", "nor",
        ];

        // "Doe" + s = "does": these are real words, never a possessive of a player ("red's" = "reds").
        public static readonly HashSet<string> PossessiveBlocklist =
            ["does", "goes", "says", "was", "has", "his", "its", "yes", "this", "thus", "gas", "bus", "plus", "less", "miss", "ass", "us", "as"];

        // Words a short sentence may leave out of a longer template without changing its meaning ("red vented" for
        // "{target} vented, I saw it!"). Anything not listed here (vent, kill, scan...) is meaning and must be present.
        public static readonly HashSet<string> SoftWords =
        [
            "i", "im", "saw", "see", "it", "idk", "maybe", "lowkey", "tbh", "fr", "lol", "bro", "sure", "think", "pretty",
            "just", "really", "literally", "honestly", "ngl", "imo", "so", "but", "and", "the", "a", "is", "was", "tho",
            "though", "btw", "yeah", "yea", "yes", "ye", "ok", "that", "this", "they", "them",
        ];

        public static readonly HashSet<string> WhereWords = ["where", "wheres", "loc", "location"];

        public static readonly HashSet<string> YouWords = ["you", "u", "ya", "youre", "ur"];

        public static readonly HashSet<string> WhereYouAbbrev = ["wya", "wru", "wyd"];

        public static readonly HashSet<string> BodyQuestionWords =
        [
            "where", "wheres", "loc", "location", "was", "is", "the", "it", "tho", "though", "again", "body",
            "found", "dead", "pls", "plz", "at", "lol", "bro", "?",
        ];

        public static readonly HashSet<string> StopNames = ["impostor", "imposter", "crewmate", "imp", "crew", "sus", "me", "you"];

        public static readonly HashSet<string> AmbiguousRoomWords = ["engine", "engines"];

        public static readonly Dictionary<string, SystemTypes> StrongRoomAliases = new()
        {
            ["cafeteria"] = SystemTypes.Cafeteria, ["cafe"] = SystemTypes.Cafeteria, ["caf"] = SystemTypes.Cafeteria, ["cafet"] = SystemTypes.Cafeteria,
            ["weapons"] = SystemTypes.Weapons, ["weapon"] = SystemTypes.Weapons, ["weap"] = SystemTypes.Weapons, ["wep"] = SystemTypes.Weapons, ["weps"] = SystemTypes.Weapons,
            ["o2"] = SystemTypes.LifeSupp, ["oxygen"] = SystemTypes.LifeSupp, ["life support"] = SystemTypes.LifeSupp, ["lifesupport"] = SystemTypes.LifeSupp,
            ["navigation"] = SystemTypes.Nav, ["nav"] = SystemTypes.Nav, ["navi"] = SystemTypes.Nav,
            ["shields"] = SystemTypes.Shields, ["shield"] = SystemTypes.Shields,
            ["comms"] = SystemTypes.Comms, ["comm"] = SystemTypes.Comms, ["coms"] = SystemTypes.Comms, ["communications"] = SystemTypes.Comms,
            ["storage"] = SystemTypes.Storage, ["stor"] = SystemTypes.Storage,
            ["admin"] = SystemTypes.Admin,
            ["electrical"] = SystemTypes.Electrical, ["elec"] = SystemTypes.Electrical, ["elect"] = SystemTypes.Electrical, ["electric"] = SystemTypes.Electrical, ["electricity"] = SystemTypes.Electrical,
            ["lower engine"] = SystemTypes.LowerEngine, ["lowerengine"] = SystemTypes.LowerEngine, ["lower"] = SystemTypes.LowerEngine, ["lower e"] = SystemTypes.LowerEngine, ["low engine"] = SystemTypes.LowerEngine,
            ["upper engine"] = SystemTypes.UpperEngine, ["upperengine"] = SystemTypes.UpperEngine, ["upper"] = SystemTypes.UpperEngine, ["upper e"] = SystemTypes.UpperEngine, ["up engine"] = SystemTypes.UpperEngine,
            ["security"] = SystemTypes.Security, ["cams"] = SystemTypes.Security, ["cameras"] = SystemTypes.Security, ["camera"] = SystemTypes.Security,
            ["reactor"] = SystemTypes.Reactor, ["reac"] = SystemTypes.Reactor,
            ["medbay"] = SystemTypes.MedBay, ["med bay"] = SystemTypes.MedBay, ["med"] = SystemTypes.MedBay, ["medical"] = SystemTypes.MedBay,
            ["hallway"] = SystemTypes.Hallway, ["hall"] = SystemTypes.Hallway,
        };

        public static readonly Dictionary<string, SystemTypes> WeakRoomAliases = new()
        {
            ["sec"] = SystemTypes.Security,
            ["ue"] = SystemTypes.UpperEngine, ["up"] = SystemTypes.UpperEngine,
            ["le"] = SystemTypes.LowerEngine, ["low"] = SystemTypes.LowerEngine, ["lo"] = SystemTypes.LowerEngine,
            ["com"] = SystemTypes.Comms,
            ["adm"] = SystemTypes.Admin,
            ["ox"] = SystemTypes.LifeSupp,
        };
    }
}
