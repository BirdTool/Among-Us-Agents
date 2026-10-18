using System;
using System.IO;
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

        public static void LoadKeys()
        {
            if (!File.Exists(FilePath))
            {
                LogManager.LogError("[LLM] PrivateConfig/config.cfg not found");
                return;
            }

            var lines = File.ReadAllLines(FilePath);

            foreach (var line in lines)
            {
                var parsedData = GetApiKeyFromLine(line);
                if (parsedData.HasValue)
                {
                    var (provider, key, index, clientId) = parsedData.Value;
                    LogManager.LogInfo($"[LLM] Loaded key for {provider} at index {index}.");
                    LlmProviderClient.AddClient(LlmProviderClient.CreateClient(provider, key, index, clientId));
                }
            }
        }
    }
}