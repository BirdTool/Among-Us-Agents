using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AMG.Enums;
using AMG.Models.LLM;
using AMG.Utilities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AMG.AI.LLM
{
    public static class LlmUsageTracker
    {
        private static readonly object SyncRoot = new();
        private static readonly object FileSyncRoot = new();

        private static readonly List<LlmUsageEntry> Entries = [];

        private static bool _initialized;

        public static readonly string FilePath =
            ModPaths.Data("Stats", "LLM", "llm_usage.jsonl");

        private static readonly JsonSerializerOptions JsonOptions =
            CreateJsonOptions();

        private static JsonSerializerOptions CreateJsonOptions()
        {
            var options = new JsonSerializerOptions();

            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }

        public static void Initialize()
        {
            lock (SyncRoot)
            {
                if (_initialized)
                    return;

                try
                {
                    string directory = Path.GetDirectoryName(FilePath)
                        ?? throw new InvalidOperationException("Invalid LLM usage file path.");

                    Directory.CreateDirectory(directory);

                    if (File.Exists(FilePath))
                    {
                        foreach (string line in File.ReadLines(FilePath))
                        {
                            if (string.IsNullOrWhiteSpace(line))
                                continue;

                            try
                            {
                                var entry =
                                    JsonSerializer.Deserialize<LlmUsageEntry>(
                                        line,
                                        JsonOptions
                                    );

                                if (entry != null)
                                    Entries.Add(entry);
                            }
                            catch (Exception ex)
                            {
                                LogManager.LogWarning($"[LLM] Ignoring invalid usage entry: {ex.GetType().Name}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"[LLM] Could not load usage history: {ex.GetType().Name}");
                }
                finally
                {
                    _initialized = true;
                }
            }
        }

        public static async Task RecordAsync(LlmUsageEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            Initialize();

            lock (SyncRoot)
            {
                Entries.Add(entry);
            }

            string jsonLine =
                JsonSerializer.Serialize(entry, JsonOptions)
                + Environment.NewLine;

            try
            {
                await Task.Run(() =>
                {
                    lock (FileSyncRoot)
                    {
                        File.AppendAllText(FilePath, jsonLine);
                    }
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogManager.LogWarning($"[LLM] Could not persist usage entry: {ex.GetType().Name}");
            }
        }

        public static LlmUsageStats GetTotalStats()
        {
            return GetStats(_ => true);
        }

        public static LlmUsageStats GetProviderStats(
            LlmProvider provider)
        {
            return GetStats(e => e.Provider == provider);
        }

        public static LlmUsageStats GetClientStats(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client ID cannot be empty.", nameof(clientId));

            return GetStats(e => string.Equals(
                e.ClientId,
                clientId,
                StringComparison.OrdinalIgnoreCase
            ));
        }

        public static LlmUsageStats GetConsumerStats(
            LlmConsumer consumer)
        {
            return GetStats(e => e.Consumer == consumer);
        }

        private static LlmUsageStats GetStats(
            Func<LlmUsageEntry, bool> predicate)
        {
            Initialize();

            LlmUsageEntry[] snapshot;

            lock (SyncRoot)
            {
                snapshot = [.. Entries.Where(predicate)];
            }

            return LlmUsageStats.From(snapshot);
        }
    }
}