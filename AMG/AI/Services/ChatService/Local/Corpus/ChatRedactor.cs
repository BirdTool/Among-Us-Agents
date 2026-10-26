using System.Text.RegularExpressions;

namespace AMG.AI.Services.ChatService.Local.Corpus
{
    public static class ChatRedactor
    {
        private static readonly Regex[] Contact =
        [
            new(@"(https?://|www\.)\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"\b[\w-]+\.(com|net|org|gg|tv|io|me|co|xyz|ly|app|br)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"\S+@\S+\.\S+", RegexOptions.Compiled),
            new(@"(^|\s)@\w{2,}", RegexOptions.Compiled),
            new(@"\w+#\d{3,}", RegexOptions.Compiled),
            new(@"\d{6,}", RegexOptions.Compiled),
            new(@"\+?\d[\d\s().-]{8,}\d", RegexOptions.Compiled),
            new(@"\b(discord|whatsapp|zap|telegram|snap(chat)?|insta(gram)?|tiktok|twitch|youtube)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        ];

        public static bool HasContactInfo(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            foreach (var r in Contact)
                if (r.IsMatch(text)) return true;
            return false;
        }
    }
}
