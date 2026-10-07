using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AMG.Models.ChatService;
using UnityEngine;

namespace AMG.AI.Services.ChatService
{
    public static class PhraseBank
    {
        private static readonly string _contextFilePath = Path.Combine(Application.dataPath, "AMG", "PhraseBank.json");

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly object _lock = new();
        private static PhraseBankData _data;

        public static PhraseBankData Data
        {
            get
            {
                if (_data != null) return _data;
                lock (_lock)
                {
                    return _data ??= LoadFromDisk();
                }
            }
        }
        public static void Reload()
        {
            lock (_lock)
            {
                _data = LoadFromDisk();
            }
        }

        public static void Save(PhraseBankData data)
        {
            lock (_lock)
            {
                _data = data;
                File.WriteAllText(_contextFilePath, JsonSerializer.Serialize(data, Options));
            }
        }

        private static PhraseBankData LoadFromDisk()
        {
            if (!File.Exists(_contextFilePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_contextFilePath)!);
                var empty = new PhraseBankData();
                File.WriteAllText(_contextFilePath, JsonSerializer.Serialize(empty, Options));
                return empty;
            }

            var json = File.ReadAllText(_contextFilePath);
            return JsonSerializer.Deserialize<PhraseBankData>(json, Options) ?? new PhraseBankData();
        }
    }
}