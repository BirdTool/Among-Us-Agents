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
        private bool _isDecidingToVote = false; 
        private readonly CooldownTimer _votingTimer = new();
        
        private SendingMessageType? _pendingChatType;
        private string _pendingChatText;

        public static bool IsAuthorizedToVote = false;
        private bool _hasVoted = false;

        private readonly CooldownTimer _endMeetingTimer = new();
        private bool _isDecidingToMove = false;

        // Temp
        private bool _reachedVotingTime = false;

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

                if (Utils.IsMeetingVoting && !_hasVoted)
                {
                    if (!_reachedVotingTime)
                    {
                        LogManager.LogDebug($"[Agente {AgentControl.PlayerId}] Chegamos na votação!");
                        _reachedVotingTime = true;
                    }
                    
                    if (!_votingTimer.IsRunning && !_isDecidingToVote) 
                    {
                        LogManager.LogDebug($"[Agente {AgentControl.PlayerId}] Iniciando decisão de voto...");
                        float reactionTime = GetReactionTime() + RandomizerExtensions.GetSecureRandomFloat(0, 4);
                        LogManager.LogDebug($"[Agente {AgentControl.PlayerId}] Tempo de decisão de voto: {reactionTime}");
                        _votingTimer.StartDelay(reactionTime);
                        _isDecidingToVote = true;
                    }

                    if (_isDecidingToVote)
                    {
                        if (_votingTimer.Consume())
                        {
                            LogManager.LogDebug($"[Agente {AgentControl.PlayerId}] Tomando decisão de voto!");
                            var memories = GetMemories();
                            var playerIDWithHighestSuspiciousPercentage = byte.MaxValue;
                            float highestSuspiciousPercentage = 0;
                            
                            foreach (var memory in memories)
                            {
                                if (memory.SuspiciusPercentage > highestSuspiciousPercentage)
                                {
                                    highestSuspiciousPercentage = memory.SuspiciusPercentage;
                                    playerIDWithHighestSuspiciousPercentage = memory.PlayerId;
                                }
                            }

                            LogManager.LogDebug($"[Agente {AgentControl.PlayerId}] {(playerIDWithHighestSuspiciousPercentage == byte.MaxValue ? "Votou skip" : "Votou em " + Utils.Players.GetPlayerByPlayerId(playerIDWithHighestSuspiciousPercentage).Data.PlayerName)}");

                            var result = SafeVote(playerIDWithHighestSuspiciousPercentage);
                            if (result == VoteRpcEnums.SUCCESS)
                            {
                                _hasVoted = true;
                            }
                            LogManager.LogDebug($"[Agente {AgentControl.PlayerId}] Resultado do voto: {result}");
                            _isDecidingToVote = false; 
                            _votingTimer.Stop();
                        }
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

                _isDecidingToVote = false; 
                _votingTimer.Stop();
                _hasVoted = false;

                if (!_endMeetingTimer.IsRunning && !_isDecidingToMove)
                {
                    float baseReaction = GetReactionTime();
                    _endMeetingTimer.StartDelay(baseReaction);
                    _isDecidingToMove = true;
                }

                if (_isDecidingToMove)
                {
                    if (_endMeetingTimer.Consume())
                    {
                        SetState(AgentState.Calculating);
                        _isDecidingToMove = false;
                        _endMeetingTimer.Stop();
                    }
                }
                
            }
        }
    }
}