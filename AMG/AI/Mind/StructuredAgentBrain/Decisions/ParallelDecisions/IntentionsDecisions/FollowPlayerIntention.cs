using System.Collections.Generic;
using AMG.AI.Mind.StructuredAgentBrain.Memories;
using AMG.Enums.AgentEnums;
using AMG.Interfaces;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain.Decisions.ParallelDecisions.IntentionsDecisions
{
    public class FollowPlayerIntention : IParallelDecision
    {
        private const float StartEvalInterval = 1.5f;
        private const float KeepEvalInterval = 2.5f;
        private const float MinFollowTime = 6f;
        private const float CooldownMin = 8f;
        private const float CooldownMax = 14f;
        private const float MaxLostTime = 12f;
        private const float FatigueStart = 25f;

        private const float HighPriority = 60f;
        private const float BaseStartChance = 10f;
        private const float BaseKeepChance = 55f;
        private const float ScoreDistanceRef = 5f;

        private sealed class AgentData
        {
            public float NextEvalTime;
            public float CooldownUntil;
            public bool WasFollowing;
        }

        private readonly Dictionary<byte, AgentData> _data = [];

        public void Evaluate(StructuredAgentBrain brain)
        {
            if (brain.IsDead) return;

            var data = GetData(brain.AgentId);
            bool following = brain.currentState == AgentState.FollowingPlayer;

            if (data.WasFollowing && !following)
            {
                data.WasFollowing = false;
                data.CooldownUntil = Time.time + RandomCooldown();
                brain.StopFollowing(changeState: false);
            }

            if (Time.time < data.NextEvalTime) return;

            if (following) EvaluateKeepFollowing(brain, data);
            else EvaluateStartFollowing(brain, data);
        }

        private void EvaluateStartFollowing(StructuredAgentBrain brain, AgentData data)
        {
            float now = Time.time;
            data.NextEvalTime = now + Jitter(StartEvalInterval);

            var intentions = brain.Intentions;
            var targets = intentions.FollowPlayers;

            if (targets.Count == 0 || intentions.AvoidAllPlayers) return;
            if (now < data.CooldownUntil || !CanBeInterrupted(brain.currentState)) return;

            PlayerControl best = null;
            AgentPeopleMemories bestMemory = null;
            float bestScore = float.MinValue;
            float bestPriority = 0f;
            Vector2 myPos = brain.Vector2Position;

            for (int i = 0; i < targets.Count; i++)
            {
                var (id, priority) = targets[i];
                if (priority <= 0f || intentions.AvoidPlayers.Contains(id)) continue;

                var player = GetPlayer(id);
                if (player == null || !brain.CanSee(player)) continue;

                var memory = brain.GetOrCreateMemory(id);
                float dist = Vector2.Distance(myPos, player.transform.position);

                float score = priority * 0.5f
                            + memory.SuspiciusPercentage * 0.3f
                            + Mathf.Clamp01(1f - dist / ScoreDistanceRef) * 20f; // mais perto = mais pontos

                if (score <= bestScore) continue;
                bestScore = score;
                best = player;
                bestMemory = memory;
                bestPriority = priority;
            }

            if (best == null) return;

            float chance = BaseStartChance
                         + Mathf.Min(bestPriority, 100f) * 0.3f
                         + (bestPriority > HighPriority ? 15f : 0f)
                         + bestMemory.SuspiciusPercentage * 0.2f
                         - GetCompetingPressure(brain);

            if (!Utils.ExecuteProbabilityAs100(chance)) return;

            data.WasFollowing = true;
            brain.StartFollowing(best);
        }

        private void EvaluateKeepFollowing(StructuredAgentBrain brain, AgentData data)
        {
            float now = Time.time;
            data.WasFollowing = true;
            data.NextEvalTime = now + Jitter(KeepEvalInterval);

            var target = brain.PlayerToFollow;
            var intentions = brain.Intentions;

            if (target == null
                || intentions.AvoidAllPlayers
                || intentions.AvoidPlayers.Contains(target.PlayerId)
                || !TryGetPriority(intentions.FollowPlayers, target.PlayerId, out float priority)
                || priority <= 0f)
            {
                Stop(brain, data);
                return;
            }

            float followedFor = now - brain.FollowStartedAt;
            if (followedFor < MinFollowTime) return;

            float lostFor = brain.FollowTargetLostFor;
            if (lostFor > MaxLostTime)
            {
                Stop(brain, data);
                return;
            }

            var memory = brain.GetOrCreateMemory(target.PlayerId);
            float fatigue = Mathf.Max(0f, followedFor - FatigueStart) * 0.8f;
            float lostPenalty = lostFor > 0.5f ? lostFor * 3f : 0f;

            float keep = BaseKeepChance
                       + Mathf.Min(priority, 100f) * 0.4f
                       + memory.SuspiciusPercentage * 0.2f
                       - GetCompetingPressure(brain)
                       - fatigue
                       - lostPenalty;

            if (!Utils.ExecuteProbabilityAs100(keep)) Stop(brain, data);
        }

        private void Stop(StructuredAgentBrain brain, AgentData data)
        {
            data.WasFollowing = false;
            data.CooldownUntil = Time.time + RandomCooldown();
            brain.StopFollowing();
        }

        private static float GetCompetingPressure(StructuredAgentBrain brain)
        {
            var intentions = brain.Intentions;
            float pressure = 0f;

            if (brain.currentSabotageStep != null) pressure += 50f;
            if (brain.currentLocalTask != null) pressure += 15f;

            if (intentions.CompleteTaskAttention > 10f) pressure += 15f;
            else if (intentions.CompleteTaskAttention < -10f) pressure -= 10f;

            if (brain.IsCrewmate) pressure += Mathf.Min(brain.RemainingTasks, 4) * 2f;
            if (intentions.AvoidBeAlone) pressure -= 15f;

            return pressure;
        }

        private static bool CanBeInterrupted(AgentState state) => state switch
        {
            AgentState.Navigating or AgentState.Calculating or AgentState.Wandering or
            AgentState.SmartWandering or AgentState.Stopped or AgentState.Observing => true,
            _ => false
        };

        private static bool TryGetPriority(List<(byte playerId, float priority)> list, byte id, out float priority)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].playerId != id) continue;
                priority = list[i].priority;
                return true;
            }
            priority = 0f;
            return false;
        }

        private static PlayerControl GetPlayer(byte id) => GameData.Instance?.GetPlayerById(id)?.Object;

        private static float Jitter(float interval) => interval * UnityEngine.Random.Range(0.8f, 1.2f);
        private static float RandomCooldown() => UnityEngine.Random.Range(CooldownMin, CooldownMax);

        private AgentData GetData(byte agentId)
        {
            if (_data.TryGetValue(agentId, out var data)) return data;

            data = new AgentData { NextEvalTime = Time.time + UnityEngine.Random.Range(0f, StartEvalInterval) };
            _data[agentId] = data;
            return data;
        }
    }
}