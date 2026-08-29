using System;
using System.Collections.Generic;

namespace AMG.AI.Control.AgentController
{
    public partial class AgentController
    {
        public readonly List<Func<bool>> OnReachedTheCurrentPath = [];
        protected Action OnStuckedInPath = null;
    }
}