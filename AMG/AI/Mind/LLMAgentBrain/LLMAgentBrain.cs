using System;
using System.Linq;
using AMG.AI.Control.AgentController;
using AMG.Interfaces;
using AMG.Models.LLMAssistence;
using AMG.Utilities;
using AMG.Utilities.MapUtils.TasksUtils;

namespace AMG.AI.Mind.LLMAgentBrain
{
    public partial class LLMAgentBrain(IntPtr ptr) : AgentController(ptr)
    {
        private ArtificialTask CurrentTask;
        private ILLMAssistence Assistence;

        private bool _doingTask = false;

        protected override void Awake()
        {
            base.Awake();

            Assistence = LLMDefaultAssistence.GetAssistenceByRole(this);
        }

        void Update()
        {
            Assistence ??= LLMDefaultAssistence.GetAssistenceByRole(this);
            CheckCurrentTask();
            if (HasPath && !_doingTask)
            {
                ProcessPath();
            }
            else
            {
                if (CurrentTask != null)
                {
                    _doingTask = true;
                    if (TryExecuteTask(CurrentTask))
                    {
                        CurrentTask = null;
                        _doingTask = false;
                    }
                }
            }

            if (_chatService == null) InstanceLlmChatService();

            if (Utils.IsMeeting) _chatService.Update();

            Assistence?.Update();
        }

        private void CheckCurrentTask()
        {
            if (CurrentTask == null || CurrentTask.IsCompleted ||
                !Agent.myTasks.ToArray().Any(p => p.Id == CurrentTask.Id))
            {
                AITasks.Remove(CurrentTask);
                CurrentTask = null;
                _doingTask = false;
            }
        }

        private void ProcessPath()
        {
            bool? hasReachedDestination = ProcessPathMovement();

            if (hasReachedDestination == true || hasReachedDestination == null) ResetPath();
        }
    }
}