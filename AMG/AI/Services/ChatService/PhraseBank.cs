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

        public static PhraseBankData ReadFile()
        {
            if (!File.Exists(_contextFilePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_contextFilePath)!);
                WriteFile(new PhraseBankData());
            }
            var json = File.ReadAllText(_contextFilePath);
            return JsonSerializer.Deserialize<PhraseBankData>(json, Options) ?? new PhraseBankData();
        }

        public static void WriteFile(PhraseBankData data)
        {
            var json = JsonSerializer.Serialize(data, Options);
            File.WriteAllText(_contextFilePath, json);
        }

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }
}