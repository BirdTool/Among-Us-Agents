using System.Collections.Generic;
using AMG.AI.Mind.LLMAgentBrain;
using AMG.Enums.SafeRpcEnums;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Services.ChatService.Llm
{
    public sealed partial class LlmChatService(LLMAgentBrain brain)
    {
        private readonly LLMAgentBrain Brain = brain;
        private readonly record struct OutboxItem(string Text, float At);
        private readonly Queue<OutboxItem> _outbox = new();

        private const float StaleOutboxSeconds = 15f;
        private const float MinSendGap = 8f; 

        private float _sendAt;
        private byte? _votedFor;

        public ChatRpcEnums CheckSay(string text) => Brain.SafeSendChatNotExecute(text ?? "...");
        public VoteRpcEnums CheckVote(byte? target) => Brain.SafeVoteNotExecute(target ?? Utils.SkipVotePlayerId);
        public ChatRpcEnums Say(string text) => Brain.SafeSendChat(text ?? "");
        public VoteRpcEnums Vote(byte? target)
        {
            byte id = target ?? Utils.SkipVotePlayerId;
            if (id >= 250) id = Utils.SkipVotePlayerId;
            var result = Brain.SafeVote(id);
            if (result == VoteRpcEnums.SUCCESS) _votedFor = id;
            return result;
        }

        public void AddOutbox(string text)
        {
            _outbox.Enqueue(new OutboxItem(text, Time.time));
        }

        private void PumpOutbox()
        {
            if (_outbox.Count == 0 || Time.time < _sendAt) return;

            var item = _outbox.Peek();
            if (Time.time - item.At > StaleOutboxSeconds)
            {
                _outbox.Dequeue();
                Dbg($"dropped stale message: \"{item.Text}\"");
                return;
            }

            string text = item.Text;
            var check = CheckSay(text);
            if (check == ChatRpcEnums.ERROR_InCooldown)   // game chat cooldown (3 s): keep it queued
            {
                _sendAt = Time.time + 0.5f;
                return;
            }
            _outbox.Dequeue();
            if (check == ChatRpcEnums.SUCCESS)
            {
                Say(text);
                Dbg($"sent: \"{text}\"");
            }
            else
            {
                // AppendUser($"[notice] your message \"{text}\" was not sent: {check}");
                Dbg($"DROPPED \"{text}\": {check}", $"<color=#ff5555>message dropped:</color> {check}");
            }

            float typing = 1.5f + text.Length / 8f;
            _sendAt = Time.time + Mathf.Max(MinSendGap, typing);
        }

        private void Dbg(string log, string chat = null)
        {
            LogManager.LogInfo($"[LLM {Brain.BaseName}] {log}");
            if (chat != null) ChatDbg(chat);
        }

        private void ChatDbg(string chat)
        {
            if (ChatMode.ChatDebug)
                Utils.AddChatPrivate(chat, $"<color=#ffaa00><b>(LLM {Brain.BaseName})</b></color>");
        }

        public void Update()
        {
            
        }
    }
}