using AMG.AI.Mind.LLMAgentBrain;

namespace AMG.Interfaces
{
    public interface ILLMAssistence
    {
        LLMAgentBrain Brain { get; }

        void Update();
        string Guidance(IGuidance guidance);
    }
}