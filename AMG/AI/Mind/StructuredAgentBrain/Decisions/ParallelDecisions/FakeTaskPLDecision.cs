using System;
using System.Collections.Generic;
using System.Linq;
using AMG.AI.TasksWork;
using AMG.AI.Tools;
using AMG.Interfaces;
using AMG.Utilities;
using AMG.Utilities.MapUtils.TasksUtils;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain.Decisions.ParallelDecisions
{
    internal class FakeTaskDetectPL : IParallelDecision
    {
        private const float MinDwellTimeToConfirm = 1.7f;
        private const float RecheckInterval = 0.5f;

        private const float SuspicionOnOrderViolation = 20f;
        private const float SuspicionOnCommonTaskMismatch = 35f;
        private const float ObserveDurationOnViolation = 3f;

        private const float LongDwellToleranceMultiplier = 1.8f;
        private const float SuspicionOnLongDwell = 12f;

        private class DwellTracking
        {
            public TaskConsoleInfo Console;
            public TaskTypes TaskType;
            public float DwellStartTime;
            public float LastSeenTime;
            public bool Confirmed;
            public bool LongDwellFlagged;
        }

        private const float ForgetDwellAfterSeconds = 2.5f;

        private readonly Dictionary<(byte observerId, byte targetId), DwellTracking> _dwelling = [];
        private readonly Dictionary<(byte observerId, byte targetId), CooldownTimer> _recheckTimers = [];
        private readonly Dictionary<byte, int> _lastPlayersCountByObserver = [];

        public void Evaluate(StructuredAgentBrain brain)
        {
            try
            {
                if (!TaskConsoleRegistry.IsInitialized)
                {
                    LogOnce("registry-not-initialized", "[FakeTaskDetectPL] TaskConsoleRegistry.IsInitialized = false, decision não vai rodar");
                    return;
                }
                if (brain.IsDead) return;

                float now = Utils.SecondsSinceShipStart.Value;
                int nearbyCount = brain.NearbyPlayersInVision.Count;

                _lastPlayersCountByObserver.TryGetValue(brain.AgentId, out int lastCount);
                if (nearbyCount != lastCount)
                {
                    _lastPlayersCountByObserver[brain.AgentId] = nearbyCount;
                    string names = string.Join(", ", brain.NearbyPlayersInVision.Select(p => p.Data.PlayerName));
                    // LogManager.LogDebug($"[FakeTaskDetectPL][DIAG] {brain.Agent.Data.PlayerName}: {nearbyCount} por perto ({names})");
                }

                foreach (var target in brain.NearbyPlayersInVision)
                {
                    if (target.PlayerId == brain.AgentId || target.Data.IsDead) continue;

                    var key = (brain.AgentId, target.PlayerId);

                    if (!_recheckTimers.TryGetValue(key, out var timer))
                        _recheckTimers[key] = timer = new CooldownTimer();

                    if (timer.IsRunning) continue;
                    timer.StartDelay(RecheckInterval);

                    EvaluateTarget(brain, target, now, key);
                }

                CleanupStaleEntries(brain.AgentId, now);
            }
            catch (Exception e)
            {
                LogManager.LogError("[FakeTaskDetectPL] Error evaluating target: " + e.Message);
                LogManager.LogError("Stack trace: " + e.StackTrace);
            }
        }

        private static readonly HashSet<string> _loggedOnce = [];
        private static void LogOnce(string key, string message)
        {
            if (_loggedOnce.Add(key))
                LogManager.LogDebug(message);
        }

        private void EvaluateTarget(StructuredAgentBrain brain, PlayerControl target, float now, (byte observerId, byte targetId) key)
        {
            Vector2 targetPos = target.transform.position;
            var (console, taskType) = TaskConsoleRegistry.GetNearestConsoleAnyType(targetPos);

            _dwelling.TryGetValue(key, out var dwell);

            if (console == null)
            {
                _dwelling.Remove(key);
                return;
            }

            bool isNewConsole = dwell == null || dwell.Console.ConsoleId != console.ConsoleId || dwell.TaskType != taskType;
            if (isNewConsole)
            {
                // LogManager.LogDebug($"[FakeTaskDetectPL][DIAG] {target.Data.PlayerName} começou dwell em {taskType} console {console.ConsoleId} (observador: {brain.Agent.Data.PlayerName})");
                dwell = new DwellTracking { Console = console, TaskType = taskType, DwellStartTime = now, LastSeenTime = now, Confirmed = false, LongDwellFlagged = false };
                _dwelling[key] = dwell;
                return;
            }

            dwell.LastSeenTime = now;

            var memory = brain.GetOrCreateMemory(target.PlayerId);
            float dwellDuration = now - dwell.DwellStartTime;

            if (!dwell.LongDwellFlagged)
            {
                var expected = TasksGroup.GetTaskTimer(taskType);
                float suspiciousThreshold = expected.MaxSuspiciousDwellSeconds
                    ?? (expected.RegularTimeToFinishTheStep + expected.TimeDisturb) * LongDwellToleranceMultiplier;

                if (dwellDuration > suspiciousThreshold)
                {
                    dwell.LongDwellFlagged = true;
                    memory.RegisterLongTaskDwell(taskType, dwellDuration);
                    memory.IncreaseSuspiciusPercentage(SuspicionOnLongDwell);
                    // LogManager.LogDebug($"[FakeTaskDetectPL] {target.Data.PlayerName} demorou {dwellDuration:F1}s em {taskType} " +
                    //    $"(esperado até {suspiciousThreshold:F1}s) — suspeito de estar só parado fingindo");
                    brain.RequestObserve(target, ObserveDurationOnViolation);
                }
            }

            if (dwell.Confirmed) return;
            if (dwellDuration < MinDwellTimeToConfirm) return;

            dwell.Confirmed = true;

            if (TaskConsoleRegistry.IsCommonTaskType(taskType))
            {
                bool iHaveThisCommonTask = brain.Agent.myTasks.ToArray().Any(t => t.TaskType == taskType);
                if (!iHaveThisCommonTask)
                {
                    memory.RegisterFakeTask();
                    memory.IncreaseSuspiciusPercentage(SuspicionOnCommonTaskMismatch);
                    // LogManager.LogDebug($"[FakeTaskDetectPL] {target.Data.PlayerName} fingiu task comum {taskType}");
                    brain.RequestObserve(target, ObserveDurationOnViolation);
                    return;
                }
            }

            if (TaskConsoleRegistry.GetOrderKind(taskType) != TaskOrderKind.Ordered) return;

            bool violatedOrder = memory.RegisterObservedTaskStep(taskType, console.ConsoleId, now);

            // LogManager.LogDebug($"[FakeTaskDetectPL][DIAG] {target.Data.PlayerName} confirmou uso de {taskType} " +
            //     $"no console {console.ConsoleId} (violou ordem: {violatedOrder})");

            if (!violatedOrder) return;

            memory.RegisterFakeTask();
            memory.IncreaseSuspiciusPercentage(SuspicionOnOrderViolation);
            // LogManager.LogDebug($"[FakeTaskDetectPL] {target.Data.PlayerName} usou {taskType} fora de ordem (console {console.ConsoleId})");

            brain.RequestObserve(target, ObserveDurationOnViolation);
        }

        private void CleanupStaleEntries(byte observerId, float now)
        {
            List<(byte, byte)> toRemove = null;
            foreach (var (key, dwell) in _dwelling)
            {
                if (key.observerId != observerId) continue;
                if (now - dwell.LastSeenTime > ForgetDwellAfterSeconds)
                    (toRemove ??= []).Add(key);
            }
            if (toRemove != null)
                foreach (var key in toRemove) _dwelling.Remove(key);
        }
    }
}