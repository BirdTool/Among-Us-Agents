using AMG.Enums.AgentEnums;
using AMG.Utilities;
using AMG.AI.Tools;
using System.Linq;
using AMG.AI.Mind.ChatDecisions;
using AMG.Enums.SafeRpcEnums;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private readonly CooldownTimer _chatTimer = new();
        private ChatOutput _chatOutput;
        private bool _isThinkingAboutChat = false; 
        
        private SendingMessageType? _pendingChatType;
        private string _pendingChatText;

        public static bool IsAuthorizedToVote = false;

        private void UpdateMeetingState()
        {
            if (myAgent.Data.IsDead) return;

            if (Utils.IsMeeting || Utils.IsExiling)
            {
                SetState(AgentState.OnMeeting);
                if (updateAction != null && updateAction.DeleteOnMeeting) updateAction = null;
                _chatOutput ??= new ChatOutput(this);

                if (!_chatTimer.IsRunning && !_isThinkingAboutChat)
                {
                    _pendingChatType = _chatOutput.Evaluate();

                    if (_pendingChatType != null)
                    {
                        _pendingChatText = _chatOutput.GetMessage(_pendingChatType.Value);
                        
                        if (!string.IsNullOrEmpty(_pendingChatText))
                        {
                            float baseReaction = GetReactionTime() + RandomizerExtensions.GetSecureRandomFloat(0, 3f);
                            float typingSpeed = (_pendingChatText.Length * 0.15f) + GetReactionTime();
                            
                            _chatTimer.StartDelay(baseReaction + typingSpeed);
                            _isThinkingAboutChat = true;
                        }
                    }
                }

                if (_isThinkingAboutChat)
                {
                    if (_chatTimer.Consume())
                    {
                        if (_pendingChatType != null && !string.IsNullOrEmpty(_pendingChatText))
                        {
                            var result = SafeSendChat(_pendingChatText);
                            
                            if (result == ChatRpcEnums.SUCCESS)
                            {
                                _chatOutput.MarkAsSent(_pendingChatType.Value);
                                LogManager.LogDebug($"[Agente {AgentControl.PlayerId}] Enviou mensagem de chat com SUCESSO!");
                            }
                        }
                        
                        _isThinkingAboutChat = false; 
                        _pendingChatType = null;
                        _pendingChatText = null;
                    }
                }
                return;
            }
            else
            {
                bodiesSeenDead.Clear();
                
                _isThinkingAboutChat = false; 
                _pendingChatType = null;
                _pendingChatText = null;
                _chatOutput = null; 
                
                SetState(AgentState.Calculating);
            }
        }
    }
}