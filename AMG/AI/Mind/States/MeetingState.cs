using AMG.Enums.AgentEnums;
using AMG.Utilities;
using AMG.AI.Tools;
using System.Linq;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private CooldownTimer _voteTimer = new CooldownTimer();
        private bool _isVoteTimerStarted = false;

        public static bool IsAuthorizedToVote = false;

        private void UpdateMeetingState()
        {
            if (myAgent.Data.IsDead) return;

            if (Utils.IsMeeting || Utils.IsExiling)
            {
                SetState(AgentState.OnMeeting);

                if (Utils.IsMeetingVoting && !MeetingHud.Instance.DidVote(myAgent.PlayerId))
                {
                    if (!_isVoteTimerStarted)
                    {
                        float humanReactionTime = RandomizerExtensions.GetSecureRandomFloat(3f, 8f);
                        _voteTimer.StartDelay(humanReactionTime);
                        _isVoteTimerStarted = true;
                    }

                    if (_voteTimer.Consume())
                    {
                        var candidates = Utils.Players.AllAlivePlayerNotMe;
                        int cnt = candidates.Count();
                        if (cnt > 0)
                        {
                            var randomPlayerToVote = candidates.ElementAt(RandomizerExtensions.GetSecureRandomInt(0, cnt));

                            IsAuthorizedToVote = true;
                            MeetingHud.Instance.CmdCastVote(myAgent.PlayerId, randomPlayerToVote.PlayerId);
                            IsAuthorizedToVote = false;
                        }
                    }
                }
                else if (!Utils.IsMeetingVoting)
                {
                    _isVoteTimerStarted = false;
                }

                return;
            }
            else
            {
                SetState(AgentState.Wandering);
                _isVoteTimerStarted = false;
            }
        }
    }
}