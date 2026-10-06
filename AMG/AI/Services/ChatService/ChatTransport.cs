using System;
using AMG.AI.Control.AgentController;
using AMG.AI.Services.ChatService.History;
using AMG.Interfaces;
using AMG.Utilities;

namespace AMG.AI.Services.ChatService
{
    public class ChatTransport(AgentController brain) : IChatTransport
    {
        private readonly AgentController _brain = brain;
        public bool DebugMode = false;
        
        public void Send(string text)
        {
            var result = _brain.SafeSendChat(text);

            if (result != Enums.SafeRpcEnums.ChatRpcEnums.SUCCESS)
            {
                if (DebugMode)
                    LogManager.LogError($"[ChatTransport] Failed to send chat message: {result}");

                throw new Exception($"[ChatTransport] Failed to send chat message: {result}");
            }

            if (DebugMode)
                LogManager.LogDebug($"[ChatTransport] Sent: {text}");
            
            MessagesSentHistory.AddMessage(_brain, text);
        }
    }
}