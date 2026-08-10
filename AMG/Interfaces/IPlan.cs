using AMG.AI.Mind;

namespace AMG.Interfaces
{
    public interface IPlan
    {
        bool IsDone { get; set; }
        void Execute(AgentBrain brain);
    }
}
