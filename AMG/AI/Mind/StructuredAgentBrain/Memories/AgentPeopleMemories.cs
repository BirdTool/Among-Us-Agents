using System.Collections.Generic;
using AMG.AI.Navigation;
using AMG.Models;
using AMG.Utilities;
using AmongUs.GameOptions;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain.Memories
{
    public class AgentPeopleMemories(byte playerId)
    {
        public byte PlayerId { get; private set; } = playerId;

        public float SuspiciusPercentage { get; private set; } = 0f;

        // Se o dono deste cérebro é inocente: o quanto ELE confia nesse jogador.
        // Se o dono deste cérebro é impostor: o quanto ele ACHA que esse jogador confia NELE
        public float Trust { get; private set; } = 50f; // neutro

        public Dictionary<RoleTypes, float> ProbablyRole = [];

        public float LastSeenTime { get; private set; } = 0f; // Use SecondsSinceShipStart
        public Vector2 LastKnownPosition { get; private set; } = Vector2.zero;

        public float PositionUncertainty { get; private set; } = 0f;

        public bool SawKilling = false;
        public byte? LastVictimSeenKilled = null;
        public float TimeOfKillWitnessed = -1f;

        public bool SawVenting = false;

        public bool SeenVerifiedTask = false;
        public float LastVerifiedTaskTime = -1f;
        public bool SeenFakeTask = false;

        public SystemTypes? ClaimedTaskLocation = null; 
        public float ClaimedTaskTime = -1f;

        public bool SelfReportedBody = false;

        public byte? LastVoteTarget = null;
        public bool VotedAgainstConsensusLastMeeting = false;

        private readonly List<(byte playerId, float time)> _seenTogetherWith = [];
        public IReadOnlyList<(byte playerId, float time)> SeenTogetherWith => _seenTogetherWith;
        private const int MaxGroupMemory = 10;

        public void RealisticForgetInformation(uint level) // Executado a cada ~4.2f segundos
        {
            DecaySuspicionAndTrust(level);
            DecayPositionMemory(level);
        }

        private void DecayPositionMemory(uint level)
        {
            if (level >= 5) return;
            if (LastKnownPosition == Vector2.zero) return;

            float timeElapsed = Utils.SecondsSinceShipStart.Value - LastSeenTime;
            float timePenalty = (timeElapsed / 10f) * 0.02f;
            float suspicionBonus = (SuspiciusPercentage / 100f) * 0.25f;

            float chanceOfForgetLastPosition = 0.02f;
            switch (level)
            {
                case 0:
                    chanceOfForgetLastPosition += 0.60f + timePenalty;
                    break;
                case 1:
                    chanceOfForgetLastPosition += 0.40f + timePenalty - suspicionBonus;
                    break;
                case 2:
                    chanceOfForgetLastPosition += 0.25f + timePenalty - suspicionBonus;
                    break;
                case 3:
                    chanceOfForgetLastPosition += 0.10f + timePenalty - suspicionBonus;
                    break;
                case 4:
                    chanceOfForgetLastPosition += 0.03f + timePenalty - suspicionBonus;
                    break;
            }

            chanceOfForgetLastPosition = Mathf.Clamp01(chanceOfForgetLastPosition);

            if (Utils.ExecuteProbability(chanceOfForgetLastPosition))
            {
                LastKnownPosition = Vector2.zero;
                LastSeenTime = 0;
                PositionUncertainty = 0f;
                return;
            }

            float uncertaintyGrowth = level switch
            {
                0 => 1.2f,
                1 => 0.8f,
                2 => 0.5f,
                3 => 0.25f,
                4 => 0.1f,
                _ => 0f
            };
            PositionUncertainty += uncertaintyGrowth;
        }

        private void DecaySuspicionAndTrust(uint level)
        {
            if (level >= 5) return;

            float pullStrength = level switch
            {
                0 => 0.006f,
                1 => 0.0035f,
                2 => 0.002f,
                3 => 0.0008f,
                4 => 0.0002f,
                _ => 0f
            };

            SuspiciusPercentage = Mathf.Lerp(SuspiciusPercentage, 0f, pullStrength);
            Trust = Mathf.Lerp(Trust, 50f, pullStrength);
        }

        public void IncreaseSuspiciusPercentage(float value)
        {
            SuspiciusPercentage = Mathf.Clamp(SuspiciusPercentage + value, 0f, 100f);
        }

        public void DecreaseSuspiciusPercentage(float value)
        {
            SuspiciusPercentage = Mathf.Clamp(SuspiciusPercentage - value, 0f, 100f);
        }

        public void IncreaseTrust(float value)
        {
            Trust = Mathf.Clamp(Trust + value, 0f, 100f);
        }

        public void DecreaseTrust(float value)
        {
            Trust = Mathf.Clamp(Trust - value, 0f, 100f);
        }

        public void UpdateRoleProbability(RoleTypes role, float delta)
        {
            ProbablyRole.TryAdd(role, 0f);
            ProbablyRole[role] = Mathf.Clamp01(ProbablyRole[role] + delta);
        }

        public void RegisterVerifiedTask(float time)
        {
            SeenVerifiedTask = true;
            LastVerifiedTaskTime = time;
        }

        public void RegisterFakeTask()
        {
            SeenFakeTask = true;
        }

        public void RegisterClaimedTask(SystemTypes location, float time)
        {
            ClaimedTaskLocation = location;
            ClaimedTaskTime = time;
        }

        public void RegisterKillWitnessed(byte victimId, float time)
        {
            SawKilling = true;
            LastVictimSeenKilled = victimId;
            TimeOfKillWitnessed = time;
        }

        public void RegisterVote(byte? targetId, bool againstConsensus)
        {
            LastVoteTarget = targetId;
            VotedAgainstConsensusLastMeeting = againstConsensus;
        }

        public void RegisterSeenTogether(byte otherPlayerId, float time)
        {
            _seenTogetherWith.Add((otherPlayerId, time));
            if (_seenTogetherWith.Count > MaxGroupMemory)
                _seenTogetherWith.RemoveAt(0);
        }

        public void UpdateLastSeen(float time, Vector2 position)
        {
            LastSeenTime = time;
            LastKnownPosition = position;
            PositionUncertainty = 0f;
        }

        public void UpdateLastSeen(float time, Waypoint waypoint) => UpdateLastSeen(time, waypoint.Position);

        public void UpdateLastSeen(float time, Vector3 position) => UpdateLastSeen(time, (Vector2)position);
    }
}