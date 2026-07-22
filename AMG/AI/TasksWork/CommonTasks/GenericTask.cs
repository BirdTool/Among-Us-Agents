using AMG.Interfaces;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.TasksWork.CommonTasks
{
    public class GenericTask(bool IsShort) : ITaskWork
    {
        public int Points => 0;
        private float? StartTime = null;
        private int SecondsDuration = IsShort ? Utils.GetRandomInt(4, 7) : Utils.GetRandomInt(8, 14);

        public bool Execute()
        {
            if (StartTime == null)
            {
                StartTime = Time.time;
                return false;
            }
            if (SecondsDuration > 0)
            {
                float secondsSinceStart = Time.time - StartTime.Value;
                return secondsSinceStart > SecondsDuration;
            }
            return true;
        }
    }
}