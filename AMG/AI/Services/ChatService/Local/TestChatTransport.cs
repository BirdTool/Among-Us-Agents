using AMG.Enums.SafeRpcEnums;
using AMG.Interfaces;
using AMG.Utilities;

namespace AMG.AI.Services.ChatService.Local
{
    public class TestChatTransport : IChatTransport
    {
        public ChatRpcEnums TrySend(string text)
        {
            LogManager.Log($"[TEST] [ChatTransport] Sent: {text}");
            return ChatRpcEnums.SUCCESS;
        }
    }
}