using AMG.AI.Mind;
using AMG.AI.Mind.ReactiveAgentBrain;
using AMG.AI.Mind.StructuredAgentBrain;

namespace AMG.Interfaces
{
    public interface IPlan
    {
        bool IsDone { get; set; }
        void Execute(StructuredAgentBrain brain);
    }
}
