using AMG.Utilities;
using AmongUs.GameOptions;
using System; // Necessário para o Math.Min
using System.Collections.Generic;

namespace AMG.AI.Tools
{
    public static class TaskAssignment
    {
        private static List<NormalPlayerTask> CommonTasks = new List<NormalPlayerTask>();

        public static void SetCommonTask()
        {
            var globalCommonTasks = ShipStatus.Instance.CommonTasks;
            if (globalCommonTasks != null)
            {
                var tasksCopy = new List<NormalPlayerTask>();
                foreach (var task in globalCommonTasks) tasksCopy.Add(task);

                tasksCopy.ShuffleSecure();

                int commonTaskCount = GameOptionsManager.Instance.CurrentGameOptions.GetInt(Int32OptionNames.NumCommonTasks);
                int limit = Math.Min(commonTaskCount, tasksCopy.Count);

                CommonTasks.Clear();
                for (int i = 0; i < limit; i++)
                {
                    CommonTasks.Add(tasksCopy[i]);
                }
            }
        }

        public static void AssignTasks(PlayerControl player)
        {
            if (ShipStatus.Instance == null) return;

            player.myTasks.Clear();

            var rawTasks = new List<NormalPlayerTask>();

            if (CommonTasks.Count > 0) rawTasks.AddRange(CommonTasks);

            int shortTaskCount = GameOptionsManager.Instance.CurrentGameOptions.GetInt(Int32OptionNames.NumShortTasks);
            int longTaskCount = GameOptionsManager.Instance.CurrentGameOptions.GetInt(Int32OptionNames.NumLongTasks);

            var shortTasksCopy = new List<NormalPlayerTask>();
            if (ShipStatus.Instance.ShortTasks != null)
                foreach (var t in ShipStatus.Instance.ShortTasks) shortTasksCopy.Add(t);

            var longTasksCopy = new List<NormalPlayerTask>();
            if (ShipStatus.Instance.LongTasks != null)
                foreach (var t in ShipStatus.Instance.LongTasks) longTasksCopy.Add(t);

            shortTasksCopy.ShuffleSecure();
            longTasksCopy.ShuffleSecure();

            int shortLimit = Math.Min(shortTaskCount, shortTasksCopy.Count);
            for (int i = 0; i < shortLimit; i++)
            {
                rawTasks.Add(shortTasksCopy[i]);
            }

            int longLimit = Math.Min(longTaskCount, longTasksCopy.Count);
            for (int i = 0; i < longLimit; i++)
            {
                rawTasks.Add(longTasksCopy[i]);
            }

            int currentId = 0;

            foreach (var task in rawTasks)
            {
                var spawnedTask = UnityEngine.Object.Instantiate(task, player.transform);
                spawnedTask.Id = (uint)currentId;
                spawnedTask.Owner = player;
                player.myTasks.Add(spawnedTask);
                currentId++;
            }
        }

        public static void AssignTasks(List<PlayerControl> playerList)
        {
            foreach (PlayerControl player in playerList)
            {
                AssignTasks(player);
            }
        }
    }
}