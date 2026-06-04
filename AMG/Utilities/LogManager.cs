using System;
using System.Collections.Generic;
using System.Timers;

namespace AMG.Utilities
{
    public static class LogManager
    {
        private static readonly string logFilePath = "AMG/logs/log.txt";
        private static readonly string allLogFilePath = "AMG/logs/all-logs.txt";
        private static readonly bool debugLogEnabled = true;

        private static readonly List<string> LogQueue = [];
        private static Timer flushTimer;
        private static readonly object queueLock = new object();
        private static readonly int flushDelayMs = 2000;
        private static readonly int maxQueueSize = 5;

        static LogManager()
        {
            flushTimer = new Timer(flushDelayMs);
            flushTimer.Elapsed += OnFlushTimerElapsed;
            flushTimer.AutoReset = false;
        }

        private static void OnFlushTimerElapsed(object sender, ElapsedEventArgs e)
        {
            FlushQueue();
        }

        private static void FlushQueue()
        {
            lock (queueLock)
            {
                if (LogQueue.Count == 0)
                    return;

                string allMessages = string.Join("\n", LogQueue);

                try
                {
                    System.IO.File.AppendAllText(logFilePath, allMessages + "\n");

                    LogQueue.Clear();

                    flushTimer.Stop();
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("[ERROR] trying to flush logs to file: " + e.Message);
                }
            }
        }

        private static void AddToQueue(string message)
        {
            string currentHour = DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = $"[{currentHour}] [AMG] {message}";

            System.Console.WriteLine(formattedMessage);

            lock (queueLock)
            {
                LogQueue.Add(formattedMessage);

                flushTimer.Stop();

                if (LogQueue.Count >= maxQueueSize)
                {
                    FlushQueue();
                }
                else
                {
                    flushTimer.Start();
                }
            }
        }

        public static void Log(string message)
        {
            AddToQueue(message);
        }

        public static void LogError(string message)
        {
            AddToQueue("[ERROR] " + message);
        }

        public static void LogWarning(string message)
        {
            AddToQueue("[WARNING] " + message);
        }

        public static void LogInfo(string message)
        {
            AddToQueue("[INFO] " + message);
        }

        public static void LogDebug(string message)
        {
            if (!debugLogEnabled) return;
            AddToQueue("[DEBUG] " + message);
        }

        public static void TransferLogsToAllLogs()
        {
            FlushQueue();

            try
            {
                string logContent = System.IO.File.ReadAllText(logFilePath);
                if (!string.IsNullOrEmpty(logContent))
                {
                    System.IO.File.AppendAllText(allLogFilePath, logContent);
                    System.IO.File.WriteAllText(logFilePath, "");
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine("[ERROR] trying to transfer logs to all-logs file: " + e.Message);
            }
        }

        public static void ForceFlush()
        {
            FlushQueue();
        }
    }
}