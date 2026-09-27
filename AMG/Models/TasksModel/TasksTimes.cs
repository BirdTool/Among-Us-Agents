using AMG.Enums;
using AMG.Interfaces;
using AMG.Utilities;
using UnityEngine;

namespace AMG.Models.TasksModel
{
    public class UploadDataTask : TaskTimer
    {
        public UploadDataTask()
        {
            RegularTimeToFinishTheStep = 10.0f;
            TimeDisturb = 0f;
            MaxSuspiciousDwellSeconds = 16.0f;
        }
    }

    public class AsteroidsTask : TaskTimer
    {
        public AsteroidsTask()
        {
            RegularTimeToFinishTheStep = 1f;
            TimeDisturb = 2.4f;
            MaxSuspiciousDwellSeconds = 27.0f;
        }
    }

    public class ResetReactorTask : TaskTimer
    {
        public ResetReactorTask()
        {
            RegularTimeToFinishTheStep = 3f;
            TimeDisturb = 4f;
            MaxSuspiciousDwellSeconds = 35.0f;
        }
    }

    public class EmptyGarbageTask : TaskTimer
    {
        public EmptyGarbageTask()
        {
            RegularTimeToFinishTheStep = 3.4f;
            TimeDisturb = 3.0f;
            MaxSuspiciousDwellSeconds = 6.0f;
        }
    }

    public class CleanO2Filter : TaskTimer
    {
        public CleanO2Filter()
        {
            RegularTimeToFinishTheStep = 1.3f;
            TimeDisturb = 2.7f;
            MaxSuspiciousDwellSeconds = 15.0f;
        }
    }

    public class FixWiringTask : TaskTimer
    {
        public FixWiringTask()
        {
            RegularTimeToFinishTheStep = 6.7f;
            TimeDisturb = 3.5f;
            MaxSuspiciousDwellSeconds = 18f;
        }
    }
}
