using HarmonyLib;
using AMG.AI.Control;
using AMG.AI.Mind;
using System.Linq;

namespace AMG.AI.Patches
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CmdCastVote))]
    public static class AntiFreeplayVotePatch
    {
        public static bool Prefix(byte playerId, byte suspectIdx)
        {
            bool isAgent = AgentManager.Agents.Any(x => x.Control != null && x.Control.PlayerId == playerId);

            if (isAgent)
            {
                if (!AgentBrain.IsAuthorizedToVote)
                {
                    return false;
                }
            }

            return true;
        }
    }
}