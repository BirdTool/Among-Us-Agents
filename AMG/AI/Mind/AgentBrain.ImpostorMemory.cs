namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        public ImpostorMemory ImpostorMemory = new();
    }

    public class ImpostorMemory
    {
        public int FakedTasks { get; private set; } = 0;
        public int Kills { get; private set; } = 0;

        public void IncrementFakedTasks() { FakedTasks++; }

        public void IncrementKills() { Kills++; }

        public void Reset()
        {
            Kills = 0;
            FakedTasks = 0;
        }
    }
}
