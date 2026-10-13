using System.Collections.Generic;
using System.Linq;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Services.ChatService;
using AMG.AI.Services.ChatService.History;
using AMG.Enums.ChatServiceEnums;
using AMG.Models.ChatService;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Services.MeetingService.StructuredBrain
{
    public class MeetingServiceForStructuredBrain
    {
        private const float FollowUpAfter = 8f;
        private readonly ChatServiceClass _chat;
        private readonly StructuredAgentBrain _brain;
        private readonly float _startedAt = Time.time;
        private readonly List<(float dueAt, ChatIntent intent)> _delayed = [];

        private bool _bodyLocationHandled;

        public ChatContextEnum Context =>
            Time.time - _startedAt < FollowUpAfter ? ChatContextEnum.MeetingStart : ChatContextEnum.MeetingFollowUp;

        public MeetingServiceForStructuredBrain(ChatServiceClass chat, StructuredAgentBrain brain)
        {
            _chat = chat; _brain = brain;
            PlanOpening();
        }

        private void PlanOpening()
        {
            foreach (var m in _brain.GetMemories())
            {
                if (m.PlayerId == _brain.AgentId) continue;

                if (m.SawKilling)
                    _chat.Enqueue(new ChatIntent
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
                    _chat.Enqueue(new ChatIntent
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
                _chat.Enqueue(new ChatIntent
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
                _chat.Enqueue(new ChatIntent
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
                if (Time.time >= _delayed[i].dueAt) { _chat.Enqueue(_delayed[i].intent); _delayed.RemoveAt(i); }
        }
    }
}