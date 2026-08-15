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
        }
    }

    public class AsteroidsTask : TaskTimer
    {
        public AsteroidsTask()
        {
            RegularTimeToFinishTheStep = 1f;
            TimeDisturb = 2.4f;
        }
    }

    public class ResetReactorTask : TaskTimer
    {
        public ResetReactorTask()
        {
            RegularTimeToFinishTheStep = 3f;
            TimeDisturb = 4f;
        }
    }

    public class EmptyGarbageTask : TaskTimer
    {
        public EmptyGarbageTask()
        {
            RegularTimeToFinishTheStep = 3.4f;
            TimeDisturb = 3.0f;
        }
    }

    public class CleanO2Filter : TaskTimer
    {
        public CleanO2Filter()
        {
            RegularTimeToFinishTheStep = 1.3f;
            TimeDisturb = 2.7f;
        }
    }
}
