using System;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.Utilities.MapUtils.TasksUtils
{
    public class ArtificialTask
    {
        public uint Id { get; set; }
        public Vector2 Position { get; set; }
        public bool IsCompleted { get; set; }
        private readonly List<TaskPosition> TaskPositions = [];
        public int CurrentStep { get; private set; } = 0;
        public PlayerTask Task { get; set; }
        public bool IsImpostorTask { get; set; } = false;
        public float BlockedUntil { get; set; } = 0f;
        public int MaxSteps
        {
            get
            {
                if (IsImpostorTask && TaskPositions.Count > 0) return TaskPositions.Count;
                return GetNormalPlayerTask()?.MaxStep ?? 1;
            }
        }

        public void SetTaskPositions(List<TaskPosition> taskPositions)
        {
            TaskPositions.Clear();
            TaskPositions.AddRange(taskPositions);
        }

        public List<TaskPosition> GetTaskPositions()
        {
            if (IsImpostorTask && TaskPositions.Count > 0) return TaskPositions;

            var positions = new List<TaskPosition>();
            foreach (var location in GetSafeTaskLocations())
                positions.Add(new TaskPosition { Position = location });
            return positions;
        }

        public void Complete() => IsCompleted = true;

        public void Uncomplete() => IsCompleted = false;

        public void NextStep()
        {
            try
            {
                if (!IsImpostorTask)
                {
                    var normalTask = GetNormalPlayerTask();
                    normalTask.NextStep();

                    if (normalTask.taskStep >= normalTask.MaxStep)
                    {
                        normalTask.taskStep = normalTask.MaxStep;

                        if (GameData.Instance != null)
                        {
                            GameData.Instance.CompletedTasks++;

                            HudManager.Instance?.taskDirtyTimer = 0f;
                        }
                    }

                    CurrentStep = normalTask.taskStep;
                    IsCompleted = normalTask.IsComplete;
                }
                else
                {
                    if (CurrentStep >= MaxSteps)
                    {
                        Complete();
                        return;
                    }

                    CurrentStep++;

                    if (CurrentStep == MaxSteps)
                        Complete();
                }
            }
            catch (System.Exception ex)
            {
                LogManager.LogDebug($"[ArtificialTask] NextStep falhou: {ex}");
                Complete();
            }
        }

        public TaskPosition GetCurrentStepTaskPosition()
        {
            if (IsImpostorTask && TaskPositions.Count > 0)
                return TaskPositions[Math.Min(CurrentStep, TaskPositions.Count - 1)];

            var positions = GetTaskPositions();
            return positions.Count > 0 ? positions[0] : null;
        }

        public NormalPlayerTask GetNormalPlayerTask()
        {
            return Task.TryCast<NormalPlayerTask>();
        }

        public List<Vector2> GetSafeTaskLocations()
        {
            var locs = new List<Vector2>();

            var normalTask = GetNormalPlayerTask();
            if (normalTask != null)
            {
                try
                {
                    var validPositions = normalTask.FindValidConsolesPositions();
                    if (validPositions != null)
                    {
                        foreach (var pos in validPositions) locs.Add(pos);
                        if (locs.Count > 0)
                        {
                            return locs;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    LogManager.LogDebug($"[TaskDecision] FindValidConsolesPositions falhou: {ex.Message}");
                }
            }

            try
            {
                foreach (var loc in Task.Locations) locs.Add(loc);
                if (locs.Count > 0) return locs;
            }
            catch (System.Exception) { }

            try
            {
                if (ShipStatus.Instance != null && ShipStatus.Instance.AllConsoles != null)
                {
                    foreach (var console in ShipStatus.Instance.AllConsoles)
                    {
                        bool hasTaskType = false;
                        foreach (var t in console.TaskTypes)
                        {
                            if (t == Task.TaskType) { hasTaskType = true; break; }
                        }
                        if (hasTaskType) locs.Add(console.transform.position);
                    }
                }
            }
            catch (System.Exception) { }

            return locs;
        }

        public bool IsNearCurrentStep(Vector2 pos, float radius)
        {
            if (IsImpostorTask && TaskPositions.Count > 0)
            {
                var p = GetCurrentStepTaskPosition();
                return p != null && Vector2.Distance(pos, p.Position) <= radius;
            }

            foreach (var loc in GetSafeTaskLocations())
                if (Vector2.Distance(pos, loc) <= radius) return true;

            return false;
        }
    }
}