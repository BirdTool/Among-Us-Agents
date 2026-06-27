using AMG.Utilities;
using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using static NetworkedPlayerInfo;

// Issues
// The task bar doesn't increase, it's probally a Freeplay's issue

namespace AMG.AI.Tools
{
    public static class TaskAssignment
    {
        private static readonly List<NormalPlayerTask> CommonTasks = [];

        private static uint CurrentId = 30;

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

        public static void AssignTasks(PlayerControl agent, byte agentPlayerId)
        {
            if (ShipStatus.Instance == null) return;

            var localPlayer = PlayerControl.LocalPlayer;
            var localPInfo = GameData.Instance?.GetPlayerById(localPlayer.PlayerId);

            agent.myTasks.Clear();

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

            foreach (var task in rawTasks)
            {
                var spawnedTask = UnityEngine.Object.Instantiate(task, localPlayer.transform);
                spawnedTask.Id = CurrentId;
                spawnedTask.Owner = localPlayer; 

                localPlayer.myTasks.Add(spawnedTask);

                if (localPInfo != null)
                {
                    if (localPInfo.Tasks == null)
                        localPInfo.Tasks = new Il2CppSystem.Collections.Generic.List<TaskInfo>();
                    localPInfo.Tasks.Add(new TaskInfo((byte)CurrentId, (uint)task.TaskType));
                }

                TaskTracker.AgentTaskIds[CurrentId] = agentPlayerId;

                agent.myTasks.Add(spawnedTask);

                spawnedTask.Initialize();

                CurrentId++;
            }

            GameData.Instance?.RecomputeTaskCounts();
        }

        public static void RegisterPlayerTasks()
        {
            TaskTracker.PlayerOwnTaskIds.Clear();
            foreach (var task in PlayerControl.LocalPlayer.myTasks)
                TaskTracker.PlayerOwnTaskIds.Add(task.Id);
        }
    }
}