using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using AMG.Data;
using AMG.Utilities;

namespace AMG.AI.Services.MessagesService
{
    public static class MessageServiceClass
    {
        private const string FileName = "messages.json";

        private static readonly string MessagesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "AMG",
            "PrivateMessages",
            FileName
        );

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static event Action<PrivateMessageModel> OnReceive;

        private static readonly Mutex FileMutex = new(false, "AMG_PrivateMessages");
        private static readonly object ProcessLock = new();

        private static FileSystemWatcher _watcher;
        private static Timer _debounce;
        private static Timer _poll;
        private static DateTime _lastWriteUtc;

        public static void StartListening()
        {
            if (_watcher != null) return;

            SureFileExists();
            _lastWriteUtc = File.GetLastWriteTimeUtc(MessagesPath);
            _debounce = new Timer(_ => ProcessNewMessages(), null, Timeout.Infinite, Timeout.Infinite);

            _watcher = new FileSystemWatcher(Path.GetDirectoryName(MessagesPath), FileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };
            _watcher.Changed += (_, _) => Schedule();
            _watcher.Created += (_, _) => Schedule();
            _watcher.Renamed += (_, _) => Schedule();

            _poll = new Timer(_ =>
            {
                if (File.GetLastWriteTimeUtc(MessagesPath) != _lastWriteUtc)
                    Schedule();
            }, null, 2000, 2000);
        }

        public static void StopListening()
        {
            _watcher?.Dispose();
            _watcher = null;
            _debounce?.Dispose();
            _debounce = null;
            _poll?.Dispose();
            _poll = null;
        }

        private static void Schedule() => _debounce?.Change(100, Timeout.Infinite);

        private static void ProcessNewMessages()
        {
            lock (ProcessLock)
            {
                try
                {
                    _lastWriteUtc = File.GetLastWriteTimeUtc(MessagesPath);

                    foreach (var message in GetNewMessages(GlobalVariables.MyIdentity))
                    {
                        try { OnReceive?.Invoke(message); }
                        catch (Exception e) { LogManager.LogError($"[Messages] OnReceive handler failed: {e}"); }
                    }
                }
                catch (Exception e)
                {
                    LogManager.LogError($"[Messages] Failed to process new messages: {e}");
                }
            }
        }

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

            WithFileLock(() =>
            {
                var messages = GetAllMessages();

                messages.Add(new PrivateMessageModel
                {
                    Content = content,
                    SentAt = DateTime.Now,
                    Sender = sender,
                    Read = false
                });

                WriteText(JsonSerializer.Serialize(messages, Options));
                return true;
            });
        }

        public static List<PrivateMessageModel> GetNewMessages(PrivateMessageSenderEnum myIdentity)
        {
            SureFileExists();

            return WithFileLock(() =>
            {
                var messages = GetAllMessages();
                var newMessages = messages.Where(m => !m.Read && m.Sender != myIdentity).ToList();

                if (newMessages.Count > 0)
                {
                    foreach (var msg in newMessages)
                        msg.Read = true;

                    WriteText(JsonSerializer.Serialize(messages, Options));
                }

                return newMessages;
            });
        }

        private static List<PrivateMessageModel> GetAllMessages()
        {
            try
            {
                return JsonSerializer.Deserialize<List<PrivateMessageModel>>(ReadText(), Options) ?? [];
            }
            catch (JsonException)
            {
                try { File.Copy(MessagesPath, MessagesPath + ".corrupt", true); } catch { }
                return [];
            }
        }

        private static string ReadText()
        {
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    using var fs = new FileStream(MessagesPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var reader = new StreamReader(fs);
                    return reader.ReadToEnd();
                }
                catch (IOException) when (attempt < 5)
                {
                    Thread.Sleep(20);
                }
            }
        }

        private static void WriteText(string json)
        {
            string tmp = MessagesPath + ".tmp";
            File.WriteAllText(tmp, json);

            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    File.Replace(tmp, MessagesPath, null);
                    return;
                }
                catch (IOException) when (attempt < 5)
                {
                    Thread.Sleep(20);
                }
            }
        }

        private static T WithFileLock<T>(Func<T> action)
        {
            bool held = false;

            try
            {
                try { held = FileMutex.WaitOne(2000); }
                catch (AbandonedMutexException) { held = true; }

                return action();
            }
            finally
            {
                if (held) FileMutex.ReleaseMutex();
            }
        }
    }
}