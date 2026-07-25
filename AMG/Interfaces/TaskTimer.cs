using AMG.Utilities;
using UnityEngine;

namespace AMG.Interfaces
{
    public abstract class TaskTimer : ITaskWork
    {
        protected float Timer = float.MaxValue;
        public float RegularTimeToFinishTheStep;
        public float TimeDisturb;
        public bool IsReady => Time.time >= Timer;

        public int Points => 0;

        public bool Execute()
        {
            if (Timer == float.MaxValue)
            {
                StartTimer();
                return false;
            }

            return IsReady;
        }

        public void StartTimer()
        {
            Timer = Time.time + Utils.GetDisturbTime(RegularTimeToFinishTheStep, TimeDisturb);
        }
    }
}
