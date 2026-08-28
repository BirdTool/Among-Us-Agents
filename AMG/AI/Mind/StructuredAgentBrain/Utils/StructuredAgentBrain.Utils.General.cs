using AMG.Enums.AgentEnums;
using AMG.Utilities;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        public float ReactionTime => Utils.GetDisturbTime(delayTime, delayDisturb);

        public void SetState(AgentState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                _updateTags.TryGetValue(newState, out var tag);
                if (tag != null)
                {
                    ReplaceNameTag(tag);
                }
            }
        }
    }
}
