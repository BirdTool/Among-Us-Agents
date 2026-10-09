using AMG.AI.Control.AgentController;
using AMG.AI.Services.ChatService.History;
using AMG.Enums.SafeRpcEnums;
using AMG.Interfaces;
using AMG.Utilities;

namespace AMG.AI.Services.ChatService
{
    public class ChatTransport(AgentController brain) : IChatTransport
    {
        private readonly AgentController _brain = brain;
        public bool DebugMode = false;

        public ChatRpcEnums TrySend(string text)
        {
            var result = _brain.SafeSendChat(text);

            if (result == ChatRpcEnums.SUCCESS)
            {
                if (DebugMode) LogManager.LogDebug($"[ChatTransport] Sent: {text}");
                MessagesSentHistory.AddMessage(_brain, text);
            }
            else if (DebugMode && result != ChatRpcEnums.ERROR_InCooldown)
            {
                LogManager.LogError($"[ChatTransport] Failed to send chat message: {result}");
            }

            return result;
        }
    }
}