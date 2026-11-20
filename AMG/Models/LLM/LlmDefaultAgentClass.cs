using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AMG.AI.Mind.LLMAgentBrain;
using AMG.AI.Services.ChatService.Llm;
using AMG.AI.Services.MeetingService;
using AMG.Enums.LLMEnums;
using AMG.Utilities;

namespace AMG.Models.LLM
{
    // Base of every LLM "model" (agent). Subclasses in AI/Services/ChatService/Llm/Agents only override the
    // "model settings" below; everything that runs the model (requests, steps, commands) lives in LlmDefaultAgentClass.Request.cs
    // and in LlmChatService.Loop.cs.
    public partial class LlmDefaultAgentClass
    {
        protected readonly List<LlmMessage> Messages = [];

        protected LlmDefaultAgentClass(LlmEffortEnum effort) => Effort = effort;

        // ===== MODEL SETTINGS (override these in the agent classes) =====

        // Max tokens the model may write per step (thinking included, when the provider counts it).
        public virtual int MaxOutputTokens { get; } = 1024;

        // Who the model is and what it must do. Written once per agent, goes first in every prompt.
        protected virtual string StartPrompt => string.Empty;

        // How it talks / its personality.
        protected virtual string Persona => string.Empty;

        // Seconds after the meeting starts before this agent is woken for the first time.
        public virtual float StartDelaySeconds => 0f;

        // ===== PROMPT =====

        protected virtual string LlmPlayerInfo(LLMAgentBrain brain) => $"You: Name: {brain.BaseName}, Colour: {Utils.Colours.ColourOf(brain.Agent)}, Role: {brain.Agent.Data.Role}";

        // How the model must answer. The loop parses exactly this (LlmTurn), so it is not a "model setting".
        protected virtual string ResponseFormat => """
            Answer ONLY with a JSON object, nothing before or after it:
            {"reasoning": "<your short thinking>", "commands": [{"name": "<command name>", "args": ["<arg1>", "<arg2>"]}]}
            Use "commands": [] when there is nothing to do right now (you will be called again when something changes).
            """;

        // Which commands a model may use is declared on the command (LlmCommand.Create, AllowedModels).
        // A model no command lists has nothing to do, so the loop does not wake it (it would only cost tokens).
        public bool HasCommands => LlmCommandRegistry.All.Any(c => c.CanBeUsedBy(this));

        protected virtual string GetCommandsInfo(LlmChatService service)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Commands. You may send up to {LlmChatService.MaxCommandsPerStep} per reply; they run in order:");
            foreach (var c in LlmCommandRegistry.All.Where(c => c.CanBeUsedBy(this)))
                sb.AppendLine($"{c.Name} - {c.Explanation}");

            return sb.ToString();
        }

        protected virtual string GetPlayersInfo(LlmChatService service)
        {
            var me = service.Brain.Agent.PlayerId;
            var sb = new StringBuilder();
            sb.AppendLine("Players alive (use the Id in commands):");
            foreach (var p in AMGPlayers.AlivePlayers)
                sb.AppendLine($"Id: {p.PlayerId}, Colour: {p.PlayerColour}{(p.PlayerId == me ? " (you)" : "")}");

            var gone = AMGPlayers.AllPlayers.Where(p => !p.IsAlive).ToList();
            if (gone.Count > 0)
                sb.AppendLine("Not alive (dead, ejected or left): " + string.Join(", ", gone.Select(p => $"{p.PlayerColour} (Id {p.PlayerId})")));

            return sb.ToString();
        }

        protected virtual string GetMeetingInfo(LlmChatService service)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Meeting phase: {(Utils.IsMeetingVoting ? "voting is open" : "discussion, voting is not open yet")}. {service.SecondsIntoMeeting:F0}s since the meeting started.");
            sb.AppendLine(service.VotedFor is byte v
                ? $"You already voted ({(v == Utils.SkipVotePlayerId ? "skip" : $"Id {v}")}). You cannot vote again."
                : "You have not voted yet.");
            return sb.ToString();
        }

        public List<LlmMessage> GetMessages() => Messages;
        public void AddMessage(LlmMessage message) => Messages.Add(message);

        // Order of the prompt. Override to reorder or to add sections.
        public virtual string BuildPrompt(LlmCommandContext ctx)
        {
            var sb = new StringBuilder();
            sb.AppendLine(StartPrompt);
            sb.AppendLine(Persona);
            sb.AppendLine(LlmPlayerInfo(ctx.Brain));
            sb.AppendLine(GetPlayersInfo(ctx.Service));
            sb.AppendLine(GetMeetingInfo(ctx.Service));
            sb.AppendLine(GetCommandsInfo(ctx.Service));
            sb.AppendLine(ResponseFormat);
            sb.AppendLine(MeetingTranscriptService.GetCurrentMeetingTranscriptString(ctx.Brain.AgentId));

            return sb.ToString();
        }
    }
}
