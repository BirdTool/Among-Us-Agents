using AMG.Interfaces;
using AMG.Models.TasksModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AMG.AI.TasksWork
{
    public static class TasksGroup
    {
        private static readonly Dictionary<TaskTypes, Func<ITaskWork>> TaskFactories = new()
        {
            [TaskTypes.SwipeCard] = () => new MediumTimeTask(),
            [TaskTypes.UploadData] = () => new AMG.Models.TasksModel.UploadDataTask(),
            [TaskTypes.ClearAsteroids] = () => new AMG.Models.TasksModel.AsteroidsTask(),
            [TaskTypes.ResetReactor] = () => new AMG.Models.TasksModel.ResetReactorTask(),
            [TaskTypes.EmptyGarbage] = () => new AMG.Models.TasksModel.EmptyGarbageTask(),
            [TaskTypes.EmptyChute] = () => new AMG.Models.TasksModel.EmptyGarbageTask(),
            [TaskTypes.CleanO2Filter] = () => new AMG.Models.TasksModel.CleanO2Filter(),
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

            return isShort ? new ShortTimeTask() : new LongTimeTask();
        }
    }
}