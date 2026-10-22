using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AMG.AI.Services.MessagesService
{
    public static class MessageServiceClass
    {
        private static readonly string MessagesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "AMG",
            "PrivateMessages",
            "messages.json"
        );

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static void SureFileExists()
        {
            var dir = Path.GetDirectoryName(MessagesPath);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(MessagesPath))
                File.WriteAllText(MessagesPath, "[]");
        }

        public static void WriteMessage(string content, PrivateMessageSenderEnum sender)
        {
            SureFileExists();
            
            var messages = GetAllMessages();
            
            messages.Add(new PrivateMessageModel
            {
                Content = content,
                SentAt = DateTime.Now,
                Sender = sender,
                Read = false
            });

            File.WriteAllText(MessagesPath, JsonSerializer.Serialize(messages, Options));
        }

        public static List<PrivateMessageModel> GetNewMessages(PrivateMessageSenderEnum myIdentity)
        {
            SureFileExists();
            var messages = GetAllMessages();
            
            var newMessages = messages.Where(m => !m.Read && m.Sender != myIdentity).ToList();
            
            if (newMessages.Any())
            {
                foreach (var msg in newMessages)
                {
                    msg.Read = true;
                }
                
                File.WriteAllText(MessagesPath, JsonSerializer.Serialize(messages, Options));
            }
            
            return newMessages;
        }

        private static List<PrivateMessageModel> GetAllMessages()
        {
            try
            {
                var json = File.ReadAllText(MessagesPath);
                return JsonSerializer.Deserialize<List<PrivateMessageModel>>(json, Options) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }
    }
}