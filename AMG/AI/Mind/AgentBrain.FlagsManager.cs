using AMG.Enums.AgentEnums;
using System.Collections.Generic;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private readonly List<AgentFlagsEnum> Flags = [];

        public void AddFlags(params AgentFlagsEnum[] flags)
        {
            Flags.AddRange(flags);
        }

        public void RemoveFlags(params AgentFlagsEnum[] flags)
        {
            foreach(AgentFlagsEnum flag in flags)
            {
                Flags.Remove(flag);
            }
        }

        public List<AgentFlagsEnum> GetFlags()
        {
            return Flags;
        }

        public bool HaveFlag(AgentFlagsEnum flag)
        {
            return Flags.Contains(flag);
        }

        // Utilities

        public bool IsSuper => HaveFlag(AgentFlagsEnum.SUPER);
        public bool IsUnfair => HaveFlag(AgentFlagsEnum.UNFAIR);
    }
}
