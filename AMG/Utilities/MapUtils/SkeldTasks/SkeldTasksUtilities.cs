using System.Collections.Generic;
using System.Linq;
using AMG.Utilities.MapUtils.TasksUtils;
using UnityEngine;

namespace AMG.Utilities.MapUtils.SkeldTasks
{
    public static class SkeldTasksUtilities
    {
        public static readonly List<TaskPosition> FixingWiresPosition = [
            new TaskPosition { Id = 0, Name = "Electrical", Position = new Vector2(-7.73f, -7.67f) },
            new TaskPosition { Id = 1, Name = "Storage", Position = new Vector2(-1.93f, -8.72f) },
            new TaskPosition { Id = 2, Name = "Admin", Position = new Vector2(1.39f, -6.41f) },
            new TaskPosition { Id = 3, Name = "Nav", Position = new Vector2(14.52f, -3.77f) },
            new TaskPosition { Id = 4, Name = "Cafeteria", Position = new Vector2(-5.28f, 5.25f) },
            new TaskPosition { Id = 5, Name = "Security", Position = new Vector2(-15.56f, -4.59f) }
        ];

        private static readonly HashSet<TaskTypes> VisualTasks =
        [
            TaskTypes.ClearAsteroids,
            TaskTypes.SubmitScan,
            TaskTypes.EmptyGarbage,
            TaskTypes.PrimeShields
        ];

        public static List<ArtificialTask> ResolveDuplicateIds(List<ArtificialTask> tasks)
        {
            var result = new List<ArtificialTask>();

            foreach (var group in tasks.GroupBy(t => t.Id))
            {
                var candidates = group.ToList();
                if (candidates.Count == 1)
                {
                    result.Add(candidates[0]);
                    continue;
                }

                var winner = candidates
                    .OrderBy(t => VisualTasks.Contains(t.Task.TaskType))
                    .ThenBy(t => t.MaxSteps)
                    .ThenBy(_ => Random.value)
                    .First();

                LogManager.LogDebug(
                    $"[MapTasks] Id {group.Key} duplicado. Mantida: {winner.Task.TaskType}. " +
                    $"Descartadas: {string.Join(", ", candidates.Where(c => c != winner).Select(c => c.Task.TaskType))}");

                result.Add(winner);
            }

            return result;
        }

        public static List<int> GetOrder(int min, int max, int quantity)
        {
            HashSet<int> numbers = [];

            if (quantity > max - min)
                quantity = max - min;

            while (numbers.Count < quantity)
                numbers.Add(Utils.GetRandomInt(min, max));

            List<int> result = [.. numbers];
            result.Sort();

            return result;
        }

        public static ArtificialTask GenerateFixWiring(uint taskId, PlayerTask task)
        {
            var ids = GetOrder(0, 5, 3);
            var taskPositions = new List<TaskPosition>();

            foreach (int id in ids)
                taskPositions.Add(FixingWiresPosition[id]);

            var artificialTask = new ArtificialTask
            {
                Id = taskId,
                Task = task,
                IsImpostorTask = true
            };

            artificialTask.SetTaskPositions(taskPositions);

            return artificialTask;
        }

        public static List<ArtificialTask> GetArtificialTasks(PlayerControl player)
        {
            List<ArtificialTask> tasks = [];
            bool isImpostor = player.Data.Role.IsImpostor;

            foreach (var task in player.myTasks)
            {
                if (task.TaskType == TaskTypes.FixWiring && isImpostor)
                {
                    tasks.Add(GenerateFixWiring(task.Id, task));
                    continue;
                }

                tasks.Add(new ArtificialTask { Id = task.Id, Task = task, IsImpostorTask = isImpostor });
            }

            return tasks;
        }
    }
}