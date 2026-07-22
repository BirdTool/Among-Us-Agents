using AMG.AI.TasksWork.CommonTasks;
using AMG.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AMG.AI.TasksWork
{
    public static class TasksGroup
    {
        private static readonly Dictionary<TaskTypes, Func<ITaskWork>> TaskFactories = new()
        {
            [TaskTypes.SwipeCard] = () => new CardTask(),
        };

        public static ITaskWork GetTaskOrGeneric(TaskTypes task)
        {
            if (TaskFactories.TryGetValue(task, out var factory))
            {
                return factory();
            }

            bool isShort = true;
            var longTasks = ShipStatus.Instance?.LongTasks;

            if (longTasks != null)
            {
                // Manual way to avoid problems (instead of Any method)
                for (int i = 0; i < longTasks.Count; i++)
                {
                    if (longTasks[i]?.TaskType == task)
                    {
                        isShort = false;
                        break;
                    }
                }
            }

            return new GenericTask(isShort);
        }
    }
}