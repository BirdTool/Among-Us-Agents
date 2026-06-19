using UnityEngine;

namespace AMG.AI.Tools
{
    public class CooldownTimer
    {
        private float? endDelayTime = null;

        public void StartDelay(float seconds)
        {
            endDelayTime = Time.time + seconds;
        }

        public bool IsOver()
        {
            if (!endDelayTime.HasValue) return true;
            return Time.time >= endDelayTime.Value;
        }

        public bool Consume()
        {
            if (IsOver())
            {
                endDelayTime = null;
                return true;
            }
            return false;
        }

        public void Stop()
        {
            endDelayTime = null;
        }

        public bool IsRunning => endDelayTime.HasValue && Time.time < endDelayTime.Value;
    }
}