using System;
using System.Collections.Generic;
using System.Linq;
using AMG.Enums.ChatServiceEnums;
using AMG.Models.ChatService;

namespace AMG.AI.Services.ChatService.Local
{
    public enum ClaimStatus { Open, Answered }

    // One thing somebody said in the meeting, in the same vocabulary the PhraseBank uses (Type + PhraseId + slots).
    public sealed class Claim
    {
        public int Id;
        public byte ClaimerId;
        public ChatIntent Intent;
        public float Time;
        public bool FromAgent;
        public float Confidence;
        public ClaimStatus Status = ClaimStatus.Open;
        public int? ReplyToClaimId;

        public bool IsQuestion => Intent.Type == ChatIntentEnum.Question;

        public float Age => UnityEngine.Time.time - Time;

        // Position of this message in the meeting (1 = first message). Questions go stale by messages, not only by seconds.
        public int MessageIndex;
    }

    // Public record of what was SAID in the current meeting (shared by every agent, reset on each meeting).
    // It never judges: a claim may be a lie. What each agent KNOWS lives in its own AgentPeopleMemories.
    public static class ClaimStore
    {
        public const float StartWindow = 8f;

        public const float QuestionMaxAge = 45f;

        public const int QuestionMaxDistance = 8;

        private static int _messageCount;

        public static void NoteMessage() => _messageCount++;

        public static int MessagesBefore => Math.Max(0, _messageCount - 1);

        private static readonly List<Claim> _claims = [];
        private static int _nextId = 1;

        public static float MeetingStartedAt { get; private set; }
        public static float Elapsed => UnityEngine.Time.time - MeetingStartedAt;
        public static ChatContextEnum Context => Elapsed < StartWindow ? ChatContextEnum.MeetingStart : ChatContextEnum.MeetingFollowUp;
        public static IReadOnlyList<Claim> All => _claims;

        public static void Reset()
        {
            _claims.Clear();
            _nextId = 1;
            _messageCount = 0;
            MeetingStartedAt = UnityEngine.Time.time;
        }

        public static Claim Register(byte claimerId, ChatIntent intent, bool fromAgent, float confidence)
        {
            var claim = new Claim
            {
                Id = _nextId++,
                ClaimerId = claimerId,
                Intent = intent,
                Time = UnityEngine.Time.time,
                FromAgent = fromAgent,
                Confidence = confidence,
                ReplyToClaimId = intent.ReplyToClaim,
                MessageIndex = _messageCount,
            };

            if (!claim.IsQuestion) CloseQuestionAnsweredBy(claim);

            _claims.Add(claim);
            return claim;
        }

        public static bool Any(string phraseId, ChatIntentEnum? type = null) =>
            _claims.Any(c => c.Intent.PhraseId == phraseId && (type == null || c.Intent.Type == type));

        public static List<string> OpenQuestionPhraseIds() =>
            [.. _claims.Where(c => c.Status == ClaimStatus.Open && c.IsQuestion && UnityEngine.Time.time - c.Time <= QuestionMaxAge
                                   && _messageCount - c.MessageIndex <= QuestionMaxDistance)
                   .Select(c => c.Intent.PhraseId).Distinct()];

        public static Claim Latest(string phraseId, ChatIntentEnum? type = null) =>
            _claims.LastOrDefault(c => c.Intent.PhraseId == phraseId && (type == null || c.Intent.Type == type));

        public static Claim FindById(int id) => _claims.FirstOrDefault(c => c.Id == id);
        public static Claim OpenQuestionFor(byte answererId) =>
            LatestOpenQuestion(c => c.Intent.Target == answererId && c.ClaimerId != answererId);

        public static Claim OpenGeneralQuestion(string phraseId, byte notBy) =>
            LatestOpenQuestion(c => c.Intent.PhraseId == phraseId && c.Intent.Target == null && c.ClaimerId != notBy);

        private static Claim LatestOpenQuestion(System.Func<Claim, bool> match)
        {
            for (int i = _claims.Count - 1; i >= 0; i--)
            {
                var c = _claims[i];
                if (c.Status != ClaimStatus.Open || !c.IsQuestion) continue;
                if (UnityEngine.Time.time - c.Time > QuestionMaxAge) continue;
                if (_messageCount - c.MessageIndex > QuestionMaxDistance) continue;
                if (match(c)) return c;
            }
            return null;
        }

        private static void CloseQuestionAnsweredBy(Claim answer)
        {
            Claim question = answer.ReplyToClaimId is int id
                ? FindById(id)
                : LatestOpenQuestion(q => q.Intent.PhraseId == answer.Intent.PhraseId && q.ClaimerId != answer.ClaimerId);

            if (question == null || question.Status != ClaimStatus.Open) return;

            question.Status = ClaimStatus.Answered;
            answer.ReplyToClaimId = question.Id;
        }
    }
}
