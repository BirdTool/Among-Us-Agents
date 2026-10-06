using AMG.Interfaces;
using AMG.Utilities;

namespace AMG.AI.Services.ChatService
{
    public class TestChatTransport : IChatTransport
    {
        public void Send(string text)
        {
            LogManager.Log($"[TEST] [ChatTransport] Sent: {text}");
        }
    }
}