using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AMG.Enums;
using AMG.Models.LLM;
using AMG.Utilities;

namespace AMG.AI.LLM
{
    public static class LlmReasoningTracker
    {
        private static readonly object SyncRoot = new();
        private static readonly object FileSyncRoot = new();

        private static readonly List<LlmReasoningEntry> Entries = [];

        private static bool _initialized;

        public static readonly string FilePath =
            ModPaths.Data("Stats", "LLM", "llm_reasonings.jsonl");

        private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

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
                        ?? throw new InvalidOperationException("Invalid LLM reasoning file path.");

                    Directory.CreateDirectory(directory);

                    if (File.Exists(FilePath))
                    {
                        foreach (string line in File.ReadLines(FilePath))
                        {
                            if (string.IsNullOrWhiteSpace(line))
                                continue;

                            try
                            {
                                var entry = JsonSerializer.Deserialize<LlmReasoningEntry>(line, JsonOptions);

                                if (entry != null)
                                    Entries.Add(entry);
                            }
                            catch (Exception ex)
                            {
                                LogManager.LogWarning($"[LLM] Ignoring invalid reasoning entry: {ex.GetType().Name}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"[LLM] Could not load reasoning history: {ex.GetType().Name}");
                }
                finally
                {
                    _initialized = true;
                }
            }
        }

        public static async Task RecordAsync(LlmReasoningEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            Initialize();

            lock (SyncRoot)
            {
                Entries.Add(entry);
            }

            string jsonLine = JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine;

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
                LogManager.LogWarning($"[LLM] Could not persist reasoning entry: {ex.GetType().Name}");
            }
        }

        public static LlmReasoningStats GetTotalStats() => GetStats(_ => true);

        public static LlmReasoningStats GetConsumerStats(LlmConsumer consumer) =>
            GetStats(e => e.Consumer == consumer);

        public static IReadOnlyList<LlmReasoningEntry> GetRecent(int count)
        {
            Initialize();

            lock (SyncRoot)
            {
                return [.. Entries.TakeLast(Math.Max(0, count))];
            }
        }

        private static LlmReasoningStats GetStats(Func<LlmReasoningEntry, bool> predicate)
        {
            Initialize();

            LlmReasoningEntry[] snapshot;

            lock (SyncRoot)
            {
                snapshot = [.. Entries.Where(predicate)];
            }

            return LlmReasoningStats.From(snapshot);
        }
    }
}
