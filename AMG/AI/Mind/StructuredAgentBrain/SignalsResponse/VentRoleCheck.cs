using System.Collections.Generic;
using AMG.AI.Mind.StructuredAgentBrain.Memories;
using AMG.AI.Services.ChatService;
using AMG.AI.Services.MessagesService;
using AMG.Utilities;
using AmongUs.GameOptions;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    // The agent peeks at the role of whoever it sees in a vent: venting is normal for an Engineer, expected from an
    // Impostor and impossible for anyone else (that is a cheater, and the developer is told).
    public static class VentRoleCheck
    {
        private const float EngineerBoost = 8f, EngineerCap = 30f;
        private const float ImpostorBoost = 35f, ImpostorCap = 100f;
        private const float UnknownBoost = 20f, UnknownCap = 60f;

        private static readonly HashSet<byte> Alerted = [];

        // New match: warn about the same cheater again.
        public static void Reset() => Alerted.Clear();

        public static void Apply(AgentPeopleMemories memory, PlayerControl venter)
        {
            var role = venter.Data?.Role;

            if (role == null)
            {
                memory.SawVenting = true;
                Raise(memory, UnknownBoost, UnknownCap);
                return;
            }

            if (role.IsImpostor)
            {
                memory.SawVenting = true;
                Raise(memory, ImpostorBoost, ImpostorCap);
                return;
            }

            if (role.Role == RoleTypes.Engineer)
            {
                // Legitimate: a small bump, and nothing the agent could present as "I saw X vent".
                Raise(memory, EngineerBoost, EngineerCap);
                return;
            }

            memory.SawVenting = true;
            memory.SetSuspiciusPercentage(100f);
            Alert(venter, role.Role);
        }

        private static void Raise(AgentPeopleMemories memory, float boost, float cap)
        {
            if (memory.SuspiciusPercentage >= cap) return;
            memory.SetSuspiciusPercentage(Mathf.Min(cap, memory.SuspiciusPercentage + boost));
        }

        private static void Alert(PlayerControl venter, RoleTypes role)
        {
            if (!Alerted.Add(venter.PlayerId)) return;
        }
    }
}
