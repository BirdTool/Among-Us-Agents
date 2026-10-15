using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AMG.AI.Services.ChatService.Corpus
{
    public sealed class SafetyVerdict
    {
        public static readonly SafetyVerdict Clean = new();

        public string Category;   // null = clean
        public bool Drop;         // true = the text must not be stored or logged anywhere
    }

    /// </summary>
    public sealed class ChatSafetyFilter
    {
        public const string DefaultFile =
@"# AMG chat corpus blocklist.
# Messages containing any of these (whole words, after folding leetspeak / repeated letters / spaced letters) are NOT stored:
# only the category is counted. 'profanity' is just flagged, everything else is dropped.
# This list is a starter. It WILL miss things (euphemisms, other languages): add what you see in your lobbies.

[sexual]
sex*
porn*
nude*
nsfw
dick
cock
pussy
penis
vagina
boob*
tits
cum
anal
blowjob
handjob
horny
masturbat*
orgasm*
sexo
buceta

[harassment]
rape*
kys
kill yourself

[hate]
# add slurs here, one per line. Not shipped on purpose.

[profanity]
fuck*
shit*
bitch*
porra
caralho
";

        private const string FlagOnly = "profanity";

        private readonly Dictionary<string, string> _exact = [];
        private readonly List<(string prefix, string category)> _prefixes = [];
        private readonly List<(string[] words, string category)> _phrases = [];

        public static ChatSafetyFilter Parse(IEnumerable<string> lines)
        {
            var f = new ChatSafetyFilter();
            string category = "other";

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                if (line[0] == '[' && line[^1] == ']') { category = line[1..^1].Trim().ToLowerInvariant(); continue; }

                bool prefix = line.EndsWith('*');
                var words = Tokens(Fold(prefix ? line[..^1] : line)).Select(Skeleton).Where(w => w.Length > 0).ToArray();
                if (words.Length == 0) continue;

                if (words.Length > 1) f._phrases.Add((words, category));
                else if (prefix) f._prefixes.Add((words[0], category));
                else f._exact[words[0]] = category;
            }

            return f;
        }

        public SafetyVerdict Check(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return SafetyVerdict.Clean;

            var tokens = JoinSpacedLetters(Tokens(Fold(text))).Select(Skeleton).Where(t => t.Length > 0).ToList();
            string found = null;

            void Hit(string category)
            {
                // the worst category wins: a dropped one beats a merely flagged one
                if (found == null || (found == FlagOnly && category != FlagOnly)) found = category;
            }

            foreach (var t in tokens)
            {
                if (_exact.TryGetValue(t, out var c)) Hit(c);
                foreach (var (prefix, category) in _prefixes)
                    if (t.StartsWith(prefix, StringComparison.Ordinal)) Hit(category);
            }

            foreach (var (words, category) in _phrases)
            {
                for (int i = 0; i + words.Length <= tokens.Count; i++)
                {
                    bool all = true;
                    for (int j = 0; j < words.Length && all; j++) all = tokens[i + j] == words[j];
                    if (all) { Hit(category); break; }
                }
            }

            return found == null ? SafetyVerdict.Clean : new SafetyVerdict { Category = found, Drop = found != FlagOnly };
        }

        private static string Fold(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c0 in s.ToLowerInvariant().Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c0) == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(c0 switch
                {
                    '0' => 'o', '1' => 'i', '3' => 'e', '4' => 'a', '5' => 's', '7' => 't',
                    '@' => 'a', '$' => 's', '!' => 'i', '|' => 'i', '+' => 't',
                    _ => c0,
                });
            }
            return sb.ToString();
        }

        private static List<string> Tokens(string folded)
        {
            var list = new List<string>();
            var cur = new StringBuilder();
            foreach (char c in folded)
            {
                if (char.IsLetter(c)) cur.Append(c);
                else if (cur.Length > 0) { list.Add(cur.ToString()); cur.Clear(); }
            }
            if (cur.Length > 0) list.Add(cur.ToString());
            return list;
        }

        private static List<string> JoinSpacedLetters(List<string> tokens)
        {
            var result = new List<string>();
            for (int i = 0; i < tokens.Count;)
            {
                int j = i;
                while (j < tokens.Count && tokens[j].Length == 1) j++;

                if (j - i >= 3) { result.Add(string.Concat(tokens.Skip(i).Take(j - i))); i = j; }
                else { result.Add(tokens[i]); i++; }
            }
            return result;
        }
        
        private static string Skeleton(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
                if (sb.Length == 0 || sb[^1] != c) sb.Append(c);
            return sb.ToString();
        }
    }
}
