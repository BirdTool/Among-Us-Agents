using System.Collections.Generic;
using AMG.Enums.SafeRpcEnums;
using AMG.Interfaces;
using AMG.Models.ChatService;
using UnityEngine;

namespace AMG.AI.Services.ChatService.Local
{
    public class ChatServiceClass(IChatTransport chatTransport, uint level, System.Func<byte> getAgentId)
    {
        private uint Level { get; set; } = level;
        private readonly System.Func<byte> _getAgentId = getAgentId;
        private readonly List<ChatIntent> _intents = [];
        private readonly IChatTransport _chatTransport = chatTransport;

        private float _sendAt;
        private const float MaxTypingDelay = 6f;

        private ChatIntent _current;
        private string _currentText;

        public void Enqueue(ChatIntent i) => _intents.Add(i);

        public void Clear()
        {
            _intents.Clear();
            ResetCurrent();
        }

        // Drops every queued intent that matches (e.g. a "where?" nobody needs to ask anymore)
        public void Cancel(System.Predicate<ChatIntent> match)
        {
            _intents.RemoveAll(match);
            if (_current != null && !_intents.Contains(_current)) ResetCurrent();
        }

        public bool HasPending => _intents.Count > 0;

        public void Tick()
        {
            if (_intents.Count == 0) return;

            _intents.RemoveAll(i => i.IsExpired);
            if (_current != null && !_intents.Contains(_current)) ResetCurrent();
            if (_intents.Count == 0) return;

            var best = _intents[0];
            foreach (var i in _intents)
                if (i.Urgency > best.Urgency) best = i;
            if (_current == null || best.Urgency > _current.Urgency)
            {
                _current = best;
                _currentText = null;
            }

            if (_currentText == null)
            {
                _currentText = Realizer.Realize(_current);
                if (string.IsNullOrWhiteSpace(_currentText)) { Finish(); return; }

                _sendAt = Time.time + TypingDelay(_currentText, Level);
            }

            if (Time.time < _sendAt) return;

            switch (_chatTransport.TrySend(_currentText))
            {
                case ChatRpcEnums.SUCCESS:
                    // keep the structured meaning: when this text comes back through AddChat it must not be re-parsed
                    ChatOutbox.Register(_getAgentId(), _currentText, _current);
                    Finish();
                    break;

                case ChatRpcEnums.ERROR_ContentIsEmpty:
                case ChatRpcEnums.ERROR_ContentIsBiggerThan100:
                    Finish();
                    break;

                case ChatRpcEnums.ERROR_InCooldown:
                case ChatRpcEnums.ERROR_IsNotInMeeting:
                default:
                    break;
            }
        }

        public static float TypingDelay(string text, uint level)
        {
            float t = Mathf.Clamp01(level / 5f);
            float think = Mathf.Lerp(2.0f, 0.4f, t); 
            float cps = Mathf.Lerp(2.5f, 7.0f, t);
            float jitter = Random.Range(0.85f, 1.2f);

            return Mathf.Min((think + text.Length / cps) * jitter, MaxTypingDelay);
        }

        private void Finish()
        {
            _intents.Remove(_current);
            ResetCurrent();
        }

        private void ResetCurrent()
        {
            _current = null;
            _currentText = null;
        }
    }
}