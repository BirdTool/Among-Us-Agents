using System.Collections.Generic;
using AMG.Enums.SafeRpcEnums;
using AMG.Interfaces;
using AMG.Models.ChatService;
using AMG.Utilities;

namespace AMG.AI.Services.ChatService
{
    public class ChatService(IChatTransport chatTransport)
    {
        private readonly List<ChatIntent> _intents = [];
        private readonly IChatTransport _chatTransport = chatTransport;

        private ChatIntent _current;
        private string _currentText;

        public void Enqueue(ChatIntent i) => _intents.Add(i);

        public void Clear()
        {
            _intents.Clear();
            ResetCurrent();
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

                if (string.IsNullOrWhiteSpace(_currentText))
                {
                    Finish();
                    return;
                }
            }

            switch (_chatTransport.TrySend(_currentText))
            {
                case ChatRpcEnums.SUCCESS:
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