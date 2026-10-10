using System.IO;
using BepInEx;
using UnityEngine;

namespace AMG.Utilities
{
    public static class ModPaths
    {
        public const string FolderName = "AMG";

        private static string _dataFolder;

        public static string GameRoot { get; } = Paths.GameRootPath;

        public static string RootFolder { get; } = Path.Combine(Paths.GameRootPath, FolderName);

        public static string DataFolder => _dataFolder ??= Path.Combine(Application.dataPath, FolderName);

        public static void Initialize()
        {
            Directory.CreateDirectory(RootFolder);
            Directory.CreateDirectory(DataFolder);
        }

        public static string Root(params string[] parts) => Build(RootFolder, parts);

        public static string Data(params string[] parts) => Build(DataFolder, parts);

        private static string Build(string baseFolder, string[] parts)
        {
            string full = parts == null || parts.Length == 0
                ? baseFolder
                : Path.Combine(baseFolder, Path.Combine(parts));

            string folder = parts == null || parts.Length == 0 ? full : Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

            return full;
        }
    }
}
