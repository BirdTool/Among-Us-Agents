using AMG.Enums.AgentEnums;
using AMG.Utilities;
using System.Linq;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private void UpdateMeetingState()
        {
            if (myAgent.Data.IsDead) return;
            if (Utils.IsMeeting)
            {
                SetState(AgentState.OnMeeting);
                // Test only
                if (Utils.IsMeetingVoting && !MeetingHud.Instance.DidVote(myAgent.PlayerId))
                {
                    var candidates = Utils.Players.AllAlivePlayerNotMe;
                    int cnt = candidates.Count();
                    if (cnt > 0)
                    {
                        var randomPlayerToVote = candidates.ElementAt(RandomizerExtensions.GetSecureRandomInt(0, cnt));

                        MeetingHud.Instance.CmdCastVote(myAgent.PlayerId, randomPlayerToVote.PlayerId);
                    }
                }
                return;
            }
            else
            {
                SetState(AgentState.Wandering);
            }
        }
    }
}
