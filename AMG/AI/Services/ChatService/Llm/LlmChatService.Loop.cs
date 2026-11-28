using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AMG.AI.Services.MeetingService;
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
            public bool WokeOnce;       // already woken in this meeting
            public int SeenMessages;    // messages from other players already shown in a prompt
            public float NextWakeAt;    // not before this time
            public int Requests;        // requests answered in this meeting (Profile.MaxRequestsPerMeeting)
            public bool LastCallSent;   // already told that voting is about to end
        }

        private readonly List<AgentRun> _runs = [];
        private bool _meetingReady;
        private float _meetingStartedAt;

        public float SecondsIntoMeeting => Time.time - _meetingStartedAt;

        private EffortProfile Profile => Profiles[EffortLevel];

        public void StartMeeting()
        {
            _meetingStartedAt = Time.time;
            _votedFor = null;
            _votingOpenedAt = -1f;
            ClearOutbox();

            foreach (var run in _runs) run.Agent.Cancel();
            _runs.Clear();

            EffortLevel = DefaultEffort;
            _meetingReady = true;

            foreach (var create in Profile.Agents)
                _runs.Add(new AgentRun(create()));
        }

        // New match: nothing of the previous one is remembered.
        public void ResetMatch()
        {
            foreach (var run in _runs) run.Agent.Cancel();
            _runs.Clear();
            _meetingReady = false;
            _votedFor = null;
            _votingOpenedAt = -1f;
            ClearOutbox();
            Mailbox.Clear();
            _notes.Clear();
        }

        public void Update()
        {
            if (Brain.Agent == null || Brain.Agent.Data == null) return;

            if (Utils.IsMeetingProceeding || Brain.Agent.Data.IsDead)
            {
                foreach (var run in _runs) run.Agent.Cancel();
                _meetingReady = false;   // the meeting is over: the next one starts clean
                return;
            }

            if (!_meetingReady) StartMeeting();   // Update without a MeetingStart (mod loaded mid-meeting)

            if (Utils.IsMeetingVoting && _votingOpenedAt < 0f) _votingOpenedAt = Time.time;

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
            if (_votedFor != null && !agent.WakesAfterVote) return;

            bool lastCall = IsLastCall(run);
            if (!lastCall)
            {
                if (run.Requests >= Profile.MaxRequestsPerMeeting) return;
                if (Time.time < _meetingStartedAt + agent.StartDelaySeconds || Time.time < run.NextWakeAt) return;
            }

            int others = MessagesFromOthers();
            var mail = Mailbox.Unread(agent.AgentName);
            bool chatNews = agent.SeesChat && others > run.SeenMessages && ChatIsReady(agent, others - run.SeenMessages);

            bool wake = lastCall || mail.Count > 0 || chatNews || (!run.WokeOnce && agent.WakesOnMeetingStart);
            if (!wake) return;

            string note = lastCall ? $"Voting is about to end ({VotingRemaining ?? 0f:F0}s left) and you have not voted. Cast your vote now. If most players accuse the same person, vote that person; use 253 (skip) only if nobody has a real case."
                : !run.WokeOnce && agent.WakesOnMeetingStart ? "The meeting just started."
                : mail.Count > 0 ? "New messages from your team arrived (see Team messages)."
                : "New messages arrived in the chat since your last turn.";

            Dbg($"waking {agent.AgentName}: {note}");
            if (!agent.BeginChain(ctx, Profile.MaxSteps, Profile.DeadlineSeconds, note))
            {
                run.NextWakeAt = Time.time + FailureBackoffSeconds;
                return;
            }

            if (mail.Count > 0) Mailbox.MarkRead(agent.AgentName, mail[^1]);
            run.SeenMessages = others;
            run.WokeOnce = true;
            if (lastCall) run.LastCallSent = true;
        }

        // Voting is about to end and this agent may still vote: wake it even if it is resting.
        private bool IsLastCall(AgentRun run)
        {
            var agent = run.Agent;
            if (run.LastCallSent || _votedFor != null || !Utils.IsMeetingVoting || !agent.CanVote) return false;
            if (VotingRemaining is not float left) return false;

            float window = agent.MainVoter || !MainVoterActive(agent) ? LastCallSeconds : SecondaryVoteWindowSeconds;
            return left <= window;
        }

        // A burst of chat messages costs one wake-up: wait until the chat is quiet (or enough messages piled up).
        private bool ChatIsReady(LlmDefaultAgentClass agent, int unseen)
            => unseen >= 5 || SecondsIntoMeeting - LastMessageFromOthersAt() >= agent.ChatQuietSeconds;

        private void OnTurn(AgentRun run, LlmCommandContext ctx, LlmTurnResult result)
        {
            var agent = run.Agent;

            if (result.Status != LlmTurnStatus.Ok)
            {
                if (result.Status == LlmTurnStatus.Failed)
                {
                    run.Requests++;
                    run.NextWakeAt = Time.time + FailureBackoffSeconds;
                    Dbg($"{agent.AgentName} failed ({result.ErrorCode}): {result.Error}", $"<color=#ff5555>[{agent.AgentName}] failed:</color> {result.ErrorCode}");
                }
                return;
            }

            run.Requests++;
            var turn = result.Turn;
            ChatDbg($"[{agent.AgentName}] {turn.Reasoning}");
            SaveNotes(agent.AgentName, turn.Notes);

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

            run.SeenMessages = MessagesFromOthers();   // the next step's prompt includes them
            if (agent.ContinueChain())
                MarkMailRead(agent);                   // team messages that arrived during this step are in that prompt
        }

        private void EndTurnChain(AgentRun run)
        {
            run.Agent.EndChain();
            run.NextWakeAt = Time.time + (run.Agent.RestSeconds >= 0f ? run.Agent.RestSeconds : Profile.MinInterval);
        }

        private void MarkMailRead(LlmDefaultAgentClass agent)
        {
            var mail = Mailbox.Unread(agent.AgentName);
            if (mail.Count > 0) Mailbox.MarkRead(agent.AgentName, mail[^1]);
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
        private float LastMessageFromOthersAt()
        {
            byte me = Brain.Agent.PlayerId;
            return MeetingTranscriptService.GetCurrentMeetingMessages().Where(m => m.PlayerId != me).Select(m => m.TimeSinceMeetingStarted).DefaultIfEmpty(0f).Max();
        }

        private int MessagesFromOthers()
        {
            byte me = Brain.Agent.PlayerId;
            return MeetingTranscriptService.GetCurrentMeetingMessages().Count(m => m.PlayerId != me);
        }
    }
}
