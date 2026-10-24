using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Services.ChatService.Llm
{
    // Developer directives (.private messages) and the rules that keep the agent from spamming the chat.
    public sealed partial class LlmMeetingSession
    {
        private const float QuietWindow = 6f;           // chat must be quiet this long before the agent reacts to messages not aimed at it
        private const float MaxSilence = 20f;           // ...unless the agent has not had a turn for this long
        private const float AddressedGap = 3f;          // minimum pause between turns when someone talks to the agent
        private const float EmergencyGrace = 12f;       // wait before asking why the button was pressed, if nobody explains it
        private const int MaxMessagesPerMeeting = 6;
        private const float MinSendGap = 8f;            // minimum seconds between two chat messages
        private const float StaleOutboxSeconds = 15f;   // a message that waited longer than this is outdated: drop it
        private const int SimilarityWindow = 3;         // how many of its own last messages it may not repeat
        private const float SimilarityLimit = 0.6f;
        private const int VoteReminderAtSeconds = 20;     // voting: gentle reminder if it still has not voted with this many seconds left
        private const int HurryAtSeconds = 10;            // voting: last call, vote now
        private const int MaxDirectives = 10;
        private const int MaxDirectiveLength = 300;

        private sealed record DevDirective(string Text, int Meeting);
        private readonly record struct OutboxItem(string Text, float At);

        private readonly List<DevDirective> _directives = [];
        private bool _directiveWake;
        private int _directiveEpoch, _epochAtSend;

        private float _debounceAt, _lastChatAt, _lastChainEnd, _graceUntil;
        private int _sentThisMeeting;
        private readonly List<string> _recentSent = [];
        private bool _hurryWoken, _reminderWoken;

        private readonly record struct PhaseClock(bool Known, bool Voting, bool Unlimited, int Left, int DiscussionTime, int VotingTime);

        // Seconds left in the current meeting phase, read from the game's own meeting timer.
        // Voting time 0 means "no limit" in the game settings.
        private static PhaseClock ReadClock()
        {
            bool voting = Utils.IsMeetingVoting;
            var hud = MeetingHud.Instance;
            var options = GameOptionsManager.Instance.CurrentGameOptions;
            if (hud == null || options == null) return default;

            int discussionTime = options.GetInt(AmongUs.GameOptions.Int32OptionNames.DiscussionTime);
            int votingTime = options.GetInt(AmongUs.GameOptions.Int32OptionNames.VotingTime);
            float elapsed = hud.discussionTimer;   // keeps counting through the voting phase

            if (!voting)
                return new PhaseClock(true, false, false, Mathf.Max(0, Mathf.CeilToInt(discussionTime - elapsed)), discussionTime, votingTime);

            if (votingTime <= 0)
                return new PhaseClock(true, true, true, 0, discussionTime, votingTime);

            float left = Mathf.Min(votingTime, discussionTime + votingTime - elapsed);
            return new PhaseClock(true, true, false, Mathf.Max(0, Mathf.CeilToInt(left)), discussionTime, votingTime);
        }

        private void AppendClock(StringBuilder sb, TierProfile profile)
        {
            if (profile.Lobby) return;

            var c = ReadClock();
            if (!c.Known) return;

            string votingLength = c.VotingTime > 0 ? $"{c.VotingTime}s" : "no time limit";
            if (!c.Voting)
            {
                sb.AppendLine($"Time (read just now): discussion ends in {c.Left}s, then voting stays open for {votingLength}. The chat stays open while voting is open: you can keep talking and vote at any moment of it, so there is no need to decide early. If you do not vote before voting ends, your vote is lost (a skip (253) is better than no vote).");
            }
            else if (c.Unlimited)
            {
                sb.AppendLine("Time: voting is open and has no time limit.");
            }
            else
            {
                sb.AppendLine($"Time (read just now): VOTING IS OPEN and ends in {c.Left}s. The chat is still open: keep listening to the others and vote when you are ready; there is no need to rush while there is plenty of time. If you do not vote before it ends, your vote is lost, and a reply of yours takes several seconds to arrive, so do not leave it for the very last seconds. A skip (253) is better than no vote.");
                if (c.Left <= HurryAtSeconds)
                    sb.AppendLine("VERY LITTLE TIME LEFT: vote immediately and do not send any chat message first.");
            }
            sb.AppendLine();
        }

        // Wakes the agent when the clock says it is about to run out of time to vote.
        private bool TimeAlert()
        {
            if (Profiles[Active].Lobby || _pendingVote != null) return false;

            var c = ReadClock();
            if (!c.Known || MeetingHud.Instance == null) return false;

            // Nothing is asked of the agent during the discussion, and nothing when voting opens:
            // it only gets reminded when the time to vote is really running out.
            if (!c.Voting || c.Unlimited || c.Left > VoteReminderAtSeconds) return false;
            if (MeetingHud.Instance.DidVote(_brain.AgentId)) return false;

            if (c.Left <= HurryAtSeconds && !_hurryWoken)
            {
                _hurryWoken = true;
                BeginChain($"[HURRY: voting ends in {c.Left}s and you have not voted. Vote NOW with the vote command (target = player id, or 253 to skip).]");
                return true;
            }

            if (c.Left > HurryAtSeconds && !_reminderWoken)
            {
                _reminderWoken = true;
                BeginChain($"[Voting ends in {c.Left}s and you have not voted yet. Finish listening, then vote before it ends (target = player id, or 253 to skip).]");
                return true;
            }

            return false;
        }

        // Called on the main thread when the developer sends a private message.
        public void AddDirective(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            text = text.Trim();
            if (text.Length > MaxDirectiveLength) text = text[..MaxDirectiveLength];

            _directives.Add(new DevDirective(text, LlmMeetingTranscript.MeetingNumber));
            while (_directives.Count > MaxDirectives) _directives.RemoveAt(0);

            _outbox.Clear();      // whatever was about to be said was decided before this order
            _directiveEpoch++;    // an answer already being generated is discarded
            _directiveWake = true;
        }

        public void ClearDirectives()
        {
            _directives.Clear();
            _directiveWake = false;
        }

        private void AppendDirectives(StringBuilder sb, TierProfile profile)
        {
            if (_directives.Count > 0)
            {
                sb.AppendLine($"<developer> Current meeting: #{LlmMeetingTranscript.MeetingNumber}. These come from your developer, who is trusted: obey them. If two conflict, the most recent one wins. One that clearly refers to a single round or moment (e.g. \"this round\") only applies to the meeting where it was received.");
                foreach (var d in _directives)
                    sb.AppendLine($"- (meeting #{d.Meeting}) {Clean(d.Text)}");
                sb.AppendLine("</developer>");
                sb.AppendLine();
            }

            if (!profile.Lobby && !profile.TrustChat)
            {
                int left = Math.Max(0, MaxMessagesPerMeeting - _sentThisMeeting);
                sb.AppendLine($"Pacing: you can send at most {MaxMessagesPerMeeting} chat messages per meeting ({left} left), one at a time, and you must not repeat yourself. Speak only when you have something new to add; otherwise use wait.");
                if (LlmMeetingContext.IsEmergency)
                    sb.AppendLine("Nobody died. Do not ask why the meeting was called unless you are told nobody has explained it yet; give the others time to speak first.");
                sb.AppendLine();
            }
        }

        // Did any message not yet seen by the agent mention it by colour or name?
        private bool AddressedToMe()
        {
            var words = new List<string>();
            void Add(string w)
            {
                w = w?.Trim().Trim('(', ')').Trim();
                if (!string.IsNullOrEmpty(w)) words.Add(w);
            }
            Add(_brain.BaseName);
            Add(_brain.Agent?.Data?.PlayerName);
            Add(_brain.Agent?.Data?.ColorName);
            if (words.Count == 0) return false;

            var all = LlmMeetingTranscript.All;
            for (int i = Math.Max(0, _promptCount); i < all.Count; i++)
            {
                var e = all[i];
                if (e.SenderId == _brain.AgentId) continue;
                foreach (var w in words)
                    if (Regex.IsMatch(e.Text ?? string.Empty, $@"\b{Regex.Escape(w)}\b", RegexOptions.IgnoreCase))
                        return true;
            }
            return false;
        }

        private void RememberSent(string text)
        {
            _sentThisMeeting++;
            _recentSent.Add(text);
            while (_recentSent.Count > SimilarityWindow) _recentSent.RemoveAt(0);
        }

        private bool IsRepeat(string text)
        {
            var a = Tokens(text);
            if (a.Count == 0) return false;

            foreach (var previous in _recentSent)
            {
                var b = Tokens(previous);
                int shared = 0;
                foreach (var t in a) if (b.Contains(t)) shared++;
                int union = a.Count + b.Count - shared;
                if (union > 0 && (float)shared / union >= SimilarityLimit) return true;
            }
            return false;
        }

        private static HashSet<string> Tokens(string s)
        {
            var set = new HashSet<string>();
            var word = new StringBuilder();
            foreach (char c in (s ?? string.Empty).ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c)) word.Append(c);
                else if (word.Length > 0) { set.Add(word.ToString()); word.Clear(); }
            }
            if (word.Length > 0) set.Add(word.ToString());
            return set;
        }
    }
}
