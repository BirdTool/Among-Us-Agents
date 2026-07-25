using AMG.Enums;
using AMG.Interfaces;
using AMG.Utilities;
using UnityEngine;

namespace AMG.Models.TasksModel
{
    public class LongTimeTask : TaskTimer
    {
        public LongTimeTask()
        {
            RegularTimeToFinishTheStep = 10.0f;
            TimeDisturb = 3.0f;
        }
    }

    public class MediumTimeTask : TaskTimer
    {
        public MediumTimeTask()
        {
            RegularTimeToFinishTheStep = 5.0f;
            TimeDisturb = 2.0f;
        }
    }

    public class ShortTimeTask : TaskTimer
    {
        public ShortTimeTask()
        {
            RegularTimeToFinishTheStep = 3.0f;
            TimeDisturb = 1.0f;
        }
    }
}
