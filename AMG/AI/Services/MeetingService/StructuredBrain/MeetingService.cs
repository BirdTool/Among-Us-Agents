using System.Collections.Generic;
using System.Linq;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Services.ChatService.Llm;
using AMG.AI.Services.ChatService.Local;
using AMG.AI.Services.ChatService.Local.History;
using AMG.Enums.ChatServiceEnums;
using AMG.Enums.SafeRpcEnums;
using AMG.Models.ChatService;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Services.MeetingService.StructuredBrain
{
    public class MeetingServiceForStructuredBrain
    {
        private const float FollowUpAfter = 8f;
        private readonly ChatServiceClass _chat;
        private readonly LlmMeetingSession _llm;
        private readonly StructuredAgentBrain _brain;
        private readonly float _startedAt = Time.time;
        private readonly List<(float dueAt, ChatIntent intent)> _delayed = [];

        private bool _bodyLocationHandled;

        public ChatContextEnum Context =>
            Time.time - _startedAt < FollowUpAfter ? ChatContextEnum.MeetingStart : ChatContextEnum.MeetingFollowUp;

        private readonly List<ChatIntent> _llmOpenings = [];
        private ChatIntent _llmCurrent;
        private string _llmText;
        private float _llmSendAt;

        public MeetingServiceForStructuredBrain(ChatServiceClass chat, StructuredAgentBrain brain) : this(brain)
        {
            _chat = chat;
            PlanOpening();
        }

        public MeetingServiceForStructuredBrain(LlmMeetingSession llm, StructuredAgentBrain brain) : this(brain)
        {
            _llm = llm;
            if (llm.AllowLocalOpenings) PlanOpening();
        }

        private MeetingServiceForStructuredBrain(StructuredAgentBrain brain) => _brain = brain;

        private void Emit(ChatIntent intent)
        {
            if (_chat != null) _chat.Enqueue(intent);
            else _llmOpenings.Add(intent);
        }

        private void PlanOpening()
        {
            foreach (var m in _brain.GetMemories())
            {
                if (m.PlayerId == _brain.AgentId) continue;

                if (m.SawKilling)
                    Emit(new ChatIntent
                    {
                        Type = ChatIntentEnum.Accuse,
                        PhraseId = "Kill_Culprit",
                        Evidence = EvidenceEnum.SawKilling,
                        Context = ChatContextEnum.MeetingStart,
                        Target = m.PlayerId,                 // the culprit -> {target}
                        Subject = m.LastVictimSeenKilled,    // the victim  -> {subject}
                        Room = m.KillRoom,
                        Urgency = 1f,
                        Confidence = 1f
                    });

                if (m.SawVenting)
                    Emit(new ChatIntent
                    {
                        Type = ChatIntentEnum.Accuse,
                        PhraseId = "Vent_Culprit",
                        Evidence = EvidenceEnum.SawVenting,
                        Context = ChatContextEnum.MeetingStart,
                        Target = m.PlayerId,                 // the culprit -> {target}
                        Room = m.VentRoom,
                        Urgency = 0.95f,
                        Confidence = 1f
                    });
            }

            if (_brain.KnownBodyRoom is { } bodyRoom)
            {
                _bodyLocationHandled = true;
                Emit(new ChatIntent
                {
                    Type = ChatIntentEnum.Report,
                    PhraseId = "Body_Location",
                    Context = ChatContextEnum.MeetingStart,
                    Evidence = EvidenceEnum.FoundBody,
                    Room = bodyRoom,
                    Urgency = 0.9f
                });
            }
            else
            {
                Emit(new ChatIntent
                {
                    Type = ChatIntentEnum.Question,
                    PhraseId = "Body_Location",
                    Context = ChatContextEnum.MeetingStart,
                    Urgency = 0.4f
                });
            }
        }

        public void OnClaim(Claim claim)
        {
            if (_chat == null) return;
            if (claim.ClaimerId == _brain.AgentId) return;
            if (claim.Confidence < 0.4f) return; // unsure parse: don't react

            var said = claim.Intent;
            if (said == null || said.PhraseId != "Body_Location") return;

            if (said.Type == ChatIntentEnum.Report)
            {
                _chat.Cancel(i => i.PhraseId == "Body_Location" && (i.Type == ChatIntentEnum.Question || i.ReplyToClaim != null));
                return;
            }

            if (said.Type == ChatIntentEnum.Question
                && _brain.KnownBodyRoom is { } room
                && !_bodyLocationHandled
                && !ClaimStore.Any("Body_Location", ChatIntentEnum.Report))
            {
                _bodyLocationHandled = true;
                _chat.Enqueue(new ChatIntent
                {
                    Type = ChatIntentEnum.Report,
                    PhraseId = "Body_Location",
                    Context = ClaimStore.Context,
                    Evidence = EvidenceEnum.FoundBody,
                    Room = room,
                    Urgency = 0.7f,
                    ReplyToClaim = claim.Id,
                });
            }
        }

        public void Update()
        {
            for (int i = _delayed.Count - 1; i >= 0; i--)
                if (Time.time >= _delayed[i].dueAt) { Emit(_delayed[i].intent); _delayed.RemoveAt(i); }

            if (_llm != null && _llm.AllowLocalOpenings) TickLlmOpenings();
        }

        private void TickLlmOpenings()
        {
            _llmOpenings.RemoveAll(i => i.IsExpired);
            if (_llmCurrent != null && !_llmOpenings.Contains(_llmCurrent)) ResetLlmCurrent();
            if (_llmOpenings.Count == 0) return;

            var best = _llmOpenings[0];
            foreach (var i in _llmOpenings)
                if (i.Urgency > best.Urgency) best = i;
            if (_llmCurrent == null || best.Urgency > _llmCurrent.Urgency)
            {
                _llmCurrent = best;
                _llmText = null;
            }

            if (_llmText == null)
            {
                _llmText = Realizer.Realize(_llmCurrent);
                if (string.IsNullOrWhiteSpace(_llmText)) { FinishLlmOpening(); return; }

                _llmSendAt = Time.time + ChatServiceClass.TypingDelay(_llmText, _brain.Level);
            }

            if (Time.time < _llmSendAt) return;

            switch (_llm.Say(_llmText))
            {
                case ChatRpcEnums.SUCCESS:
                case ChatRpcEnums.ERROR_ContentIsEmpty:
                case ChatRpcEnums.ERROR_ContentIsBiggerThan100:
                    FinishLlmOpening();
                    break;

                default:
                    break;
            }
        }

        private void FinishLlmOpening()
        {
            _llmOpenings.Remove(_llmCurrent);
            ResetLlmCurrent();
        }

        private void ResetLlmCurrent()
        {
            _llmCurrent = null;
            _llmText = null;
        }
    }
}