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
                File.Create(_contextFilePath);
            }
            var json = File.ReadAllText(_contextFilePath);
            return JsonSerializer.Deserialize<PhraseBankData>(json) ?? new PhraseBankData();
        }

        public static void WriteFile(PhraseBankData data)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_contextFilePath, json);
        }
    }
}