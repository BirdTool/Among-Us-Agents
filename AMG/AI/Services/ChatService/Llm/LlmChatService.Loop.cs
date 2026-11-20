using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AMG.AI.Services.MeetingService;
using AMG.Enums.LLMEnums;
using AMG.Models.LLM;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Services.ChatService.Llm
{
    public sealed partial class LlmChatService
    {
        private const float FailureBackoffSeconds = 10f;

        private sealed class AgentRun(LlmDefaultAgentClass agent)
        {
            public LlmDefaultAgentClass Agent { get; } = agent;
            public bool WokeOnce;       // already woken in this meeting (the first wake has no "new messages" condition)
            public int SeenMessages;    // messages from other players already shown in a prompt
            public float NextWakeAt;    // not before this time
        }

        private readonly List<AgentRun> _runs = [];
        private LlmEffortEnum? _runsBuiltFor;
        private float _meetingStartedAt;

        public float SecondsIntoMeeting => Time.time - _meetingStartedAt;

        private EffortProfile Profile => Profiles[EffortLevel];

        public void StartMeeting()
        {
            _meetingStartedAt = Time.time;
            _votedFor = null;
            ClearOutbox();

            foreach (var run in _runs) run.Agent.Cancel();
            _runs.Clear();
            _runsBuiltFor = EffortLevel;

            foreach (var create in Profile.Agents)
                _runs.Add(new AgentRun(create()));
        }

        public void Update()
        {
            if (Brain.Agent == null || Brain.Agent.Data == null) return;

            if (Utils.IsMeetingProceeding || Brain.Agent.Data.IsDead)
            {
                foreach (var run in _runs) run.Agent.Cancel();
                return;
            }

            if (_runsBuiltFor != EffortLevel) StartMeeting();   // Update without a MeetingStart (mod loaded mid-meeting)

            PumpOutbox();

            foreach (var run in _runs)
                StepAgent(run);
        }

        private void StepAgent(AgentRun run)
        {
            var agent = run.Agent;
            var ctx = new LlmCommandContext(this, Brain, agent);

            if (agent.IsBusy)
            {
                if (agent.TryHarvest(out var result)) OnTurn(run, ctx, result);
                return;
            }

            if (agent.InChain)
            {
                // between two steps with no request in flight (should not happen): go on or close it, never stall
                if (!agent.ContinueChain()) EndTurnChain(run);
                return;
            }

            if (!agent.HasCommands) return;

            if (Time.time < _meetingStartedAt + agent.StartDelaySeconds || Time.time < run.NextWakeAt) return;

            int others = MessagesFromOthers();
            var mail = Mailbox.Unread(agent.AgentName);
            if (run.WokeOnce && mail.Count == 0 && others <= run.SeenMessages) return;   // nothing new to react to

            string note = !run.WokeOnce ? "The meeting just started."
                : mail.Count > 0 ? "New messages from your team arrived."
                : "New messages arrived in the chat since your last turn.";
            if (mail.Count > 0) note += "\n" + MailNote(mail);

            Dbg($"waking {agent.AgentName}: {note}");
            if (!agent.BeginChain(ctx, Profile.MaxSteps, Profile.DeadlineSeconds, note))
            {
                run.NextWakeAt = Time.time + FailureBackoffSeconds;
                return;
            }

            if (mail.Count > 0) Mailbox.MarkRead(agent.AgentName, mail[^1]);
            run.SeenMessages = others;
            run.WokeOnce = true;
        }

        private void OnTurn(AgentRun run, LlmCommandContext ctx, LlmTurnResult result)
        {
            var agent = run.Agent;

            if (result.Status != LlmTurnStatus.Ok)
            {
                if (result.Status == LlmTurnStatus.Failed)
                {
                    run.NextWakeAt = Time.time + FailureBackoffSeconds;
                    Dbg($"{agent.AgentName} failed ({result.ErrorCode}): {result.Error}", $"<color=#ff5555>[{agent.AgentName}] failed:</color> {result.ErrorCode}");
                }
                return;
            }

            var turn = result.Turn;
            ChatDbg($"[{agent.AgentName}] {turn.Reasoning}");

            if (turn.Commands.Count == 0)
            {
                EndTurnChain(run);
                return;
            }

            string results = RunCommands(ctx, turn);
            agent.AppendUser(results);

            if (result.StepsLeft <= 0)
            {
                EndTurnChain(run);
                return;
            }

            DeliverMail(agent);                        // team messages that arrived during this step
            run.SeenMessages = MessagesFromOthers();   // the next step's prompt includes them
            agent.ContinueChain();
        }

        private void EndTurnChain(AgentRun run)
        {
            run.Agent.EndChain();
            run.NextWakeAt = Time.time + (run.Agent.RestSeconds >= 0f ? run.Agent.RestSeconds : Profile.MinInterval);
        }

        // Hands the agent's unread team messages to it, inside the chain it is in.
        private void DeliverMail(LlmDefaultAgentClass agent)
        {
            var mail = Mailbox.Unread(agent.AgentName);
            if (mail.Count == 0) return;

            agent.AppendUser(MailNote(mail));
            Mailbox.MarkRead(agent.AgentName, mail[^1]);
        }

        private static string MailNote(List<AgentMailbox.Item> mail)
        {
            var sb = new StringBuilder("New messages from your team:");
            foreach (var m in mail)
                sb.Append($"\n- round {m.Round}, from {m.From} ({m.Kind}): {m.Text}");

            return sb.ToString();
        }

        // Posts a message for another agent of this player. false = that agent is not running in this meeting (or is the sender).
        public bool SendToAgent(LlmDefaultAgentClass from, string to, string kind, string text)
        {
            if (to == from.AgentName || !_runs.Any(r => r.Agent.AgentName == to)) return false;

            Mailbox.Post(from.AgentName, to, kind, text, Utils.Round.CurrentRoundNumber);
            return true;
        }

        private string RunCommands(LlmCommandContext ctx, LlmTurn turn)
        {
            var agent = ctx.Agent;
            var sb = new StringBuilder("Command results:\n");
            int number = 0;

            foreach (var command in turn.Commands)
            {
                if (number >= MaxCommandsPerStep)
                {
                    sb.AppendLine($"(skipped the rest: at most {MaxCommandsPerStep} commands per reply)");
                    break;
                }

                number++;
                string name = command.Name?.Trim() ?? string.Empty;
                string outcome;

                if (!LlmCommandRegistry.TryGet(name, out var known))
                    outcome = "error: unknown command.";
                else if (!known.CanBeUsedBy(agent))
                    outcome = "error: you cannot use this command.";
                else
                {
                    try
                    {
                        outcome = TryExecute(ctx, name, command.GetArgs(), out var r) ? r.message : "error: command failed.";
                    }
                    catch (Exception e)
                    {
                        LogManager.LogWarning($"[LLM {Brain.BaseName}] command '{name}' threw: {e}");
                        outcome = "error: the command failed.";
                    }
                }

                Dbg($"{agent.AgentName} > {name} -> {outcome}", $"<color=#88ccff>{name}</color> -> {outcome}");
                sb.AppendLine($"{number}. {name} -> {outcome}");
            }

            return sb.ToString();
        }

        // Messages in this meeting's chat written by someone else. The agent's own messages must not wake it
        // (it would answer itself forever).
        private int MessagesFromOthers()
        {
            byte me = Brain.Agent.PlayerId;
            return MeetingTranscriptService.GetCurrentMeetingMessages().Count(m => m.PlayerId != me);
        }
    }
}
