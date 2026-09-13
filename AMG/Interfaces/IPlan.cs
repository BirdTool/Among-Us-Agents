using AMG.AI.Mind;
using AMG.AI.Mind.ReactiveAgentBrain;
using AMG.AI.Mind.StructuredAgentBrain;

namespace AMG.Interfaces
{
    public interface IPlan
    {
        string Name { get; set; }
        bool IsDone { get; set; }
        void Execute(StructuredAgentBrain brain);
    }
}
