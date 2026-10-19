using System.Collections.Generic;
using AMG.AI.Control.AgentController;
using AMG.Enums.SafeRpcEnums;
using AMG.Utilities;

namespace AMG.AI.Services.ChatService.Llm
{
    public sealed class LlmMeetingSession(AgentController brain)
    {
        public const byte Skip = Utils.SkipVotePlayerId;

        private readonly AgentController _brain = brain;

        public IReadOnlyList<LlmTranscriptEntry> Transcript => LlmMeetingTranscript.All;

        public ChatRpcEnums CheckSay(string text) => _brain.SafeSendChatNotExecute(text ?? "...");
        public VoteRpcEnums CheckVote(byte? target) => _brain.SafeVoteNotExecute(target ?? Skip);

        public ChatRpcEnums Say(string text) => _brain.SafeSendChat(text ?? "");
        public VoteRpcEnums Vote(byte? target) => _brain.SafeVote(target ?? Skip);
        public void Update() { }

        public void Reset() { }
    }
}
