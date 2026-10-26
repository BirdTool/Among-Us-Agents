using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using AMG.Enums;
using AMG.Utilities;

namespace AMG.AI.LLM
{
    public static class LlmApiKeyLoader
    {
        public static readonly string FilePath = ModPaths.Data("PrivateConfig", "config.cfg");

        private static (LlmProvider provider, string key, uint index, string clientId)? GetApiKeyFromLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            line = line.Trim();

            if (line.StartsWith("#", StringComparison.Ordinal))
                return null;

            var match = Regex.Match(
                line,
                @"^([A-Z]+)_KEY_(\d+)\s*=\s*(.*?)\s*$",
                RegexOptions.IgnoreCase
            );

            if (!match.Success)
                return null;

            string providerStr = match.Groups[1].Value;
            if (!uint.TryParse(match.Groups[2].Value, out uint index))
                return null;
            string key = match.Groups[3].Value.Trim();

            if (string.IsNullOrWhiteSpace(key))
                return null;

            string clientId = $"{providerStr.ToUpperInvariant()}_KEY_{index}";

            if (Enum.TryParse(providerStr, true, out LlmProvider provider) && Enum.IsDefined(typeof(LlmProvider), provider))
                return (provider, key, index, clientId);

            LogManager.LogWarning($"[LLM] Unknown provider in config.cfg: {providerStr}");

            return null;
        }

        // First-run file: explanation on top, every provider (one slot per line) and SPREAD_KEYS at the bottom.
        private static string BuildDefaultFile()
        {
            var sb = new StringBuilder();

            sb.AppendLine("# ==============================================================================");
            sb.AppendLine("#  AMG - LLM API keys");
            sb.AppendLine("# ==============================================================================");
            sb.AppendLine("#");
            sb.AppendLine("#  HOW IT WORKS");
            sb.AppendLine("#   - Paste your key right after the '=' of a line. Lines without a key are ignored,");
            sb.AppendLine("#     so fill in only the providers you have.");
            sb.AppendLine("#   - Format:  PROVIDER_KEY_NUMBER=your-key   (no quotes, no spaces inside the key)");
            sb.AppendLine("#   - You may use as many keys as you want, even several from the same provider.");
            sb.AppendLine("#     The NUMBER only has to be different between keys of the same provider.");
            sb.AppendLine("#   - PRIORITY = order of the lines, top to bottom. The first key with a value is the");
            sb.AppendLine("#     one used first; if it fails, the next one answers. To change the priority, move");
            sb.AppendLine("#     the lines up or down.");
            sb.AppendLine("#   - A key that keeps failing is paused for a while and the next ones are used.");
            sb.AppendLine("#   - Lines starting with # are comments.");
            sb.AppendLine("#   - This file is only read when the game starts: restart the game after editing it.");
            sb.AppendLine("#");
            sb.AppendLine("#  SPREAD_KEYS (at the bottom)");
            sb.AppendLine("#   - false: the top key answers everything; the others only step in if it fails.");
            sb.AppendLine("#   - true:  requests are shared between all your keys, still by priority. The top key");
            sb.AppendLine("#     answers more requests than the others. Example: a paid Claude key on top and a");
            sb.AppendLine("#     free Gemini key below = 3 requests on Claude, then 2 on Gemini, and so on.");
            sb.AppendLine("#     The more keys you add, the longer the top key's turn gets (3, then 4, then 5...).");
            sb.AppendLine("#     Use it to save a paid key with the help of a free one.");
            sb.AppendLine("# ==============================================================================");
            sb.AppendLine();

            foreach (string name in Enum.GetNames(typeof(LlmProvider)))
            {
                string provider = name.ToUpperInvariant();
                sb.AppendLine($"{provider}_KEY_1=");
                sb.AppendLine($"{provider}_KEY_2=");
                sb.AppendLine();
            }

            sb.AppendLine("SPREAD_KEYS=false");

            return sb.ToString();
        }

        private static void EnsureFileExists()
        {
            if (File.Exists(FilePath))
                return;

            try
            {
                File.WriteAllText(FilePath, BuildDefaultFile());
                LogManager.LogInfo($"[LLM] Created {FilePath}. Open it and add your API key(s) to use the LLM features.");
            }
            catch (Exception e)
            {
                LogManager.LogError($"[LLM] Could not create {FilePath}: {e.Message}");
            }
        }

        // config.cfg line: SPREAD_KEYS = true
        private static bool? GetSpreadSettingFromLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            var match = Regex.Match(line.Trim(), @"^SPREAD_KEYS\s*=\s*(\w+)\s*$", RegexOptions.IgnoreCase);

            if (!match.Success)
                return null;

            string value = match.Groups[1].Value;

            return value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("1", StringComparison.Ordinal)
                || value.Equals("on", StringComparison.OrdinalIgnoreCase);
        }

        public static void LoadKeys()
        {
            EnsureFileExists();

            if (!File.Exists(FilePath))
                return;

            var lines = File.ReadAllLines(FilePath);

            foreach (var line in lines)
            {
                if (GetSpreadSettingFromLine(line) is bool spread)
                {
                    LlmApiRouter.SpreadAcrossKeys = spread;
                    LogManager.LogInfo($"[LLM] SPREAD_KEYS = {spread}.");
                    continue;
                }

                var parsedData = GetApiKeyFromLine(line);
                if (parsedData.HasValue)
                {
                    var (provider, key, index, clientId) = parsedData.Value;
                    LogManager.LogInfo($"[LLM] Loaded key for {provider} at index {index}.");
                    LlmProviderClient.AddClient(LlmProviderClient.CreateClient(provider, key, index, clientId));
                }
            }

            if (LlmProviderClient.Clients.Count == 0)
                LogManager.LogWarning($"[LLM] No API key found in {FilePath}. LLM features stay off until you add one (restart the game after editing).");
        }
    }
}