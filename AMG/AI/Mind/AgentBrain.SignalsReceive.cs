using AMG.Enums;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private void OnSignalReceived(SignalsEnum signal)
        {
            switch (signal)
            {
                case SignalsEnum.KILL:
                    break;
                case SignalsEnum.VENT:
                    break;
                case SignalsEnum.SHAPESHIFTER_ABILITY:
                    break;
                case SignalsEnum.PHANTOM_ABILITY:
                    break;
            }
        }

        public void SignalReceive(SignalsEnum signal) => OnSignalReceived(signal);
    }
}