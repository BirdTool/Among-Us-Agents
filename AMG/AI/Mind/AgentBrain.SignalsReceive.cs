using AMG.Enums;
using AMG.Interfaces;
using AMG.Models.Signals;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private void OnSignalReceived(ISignalClass signal)
        {
            switch (signal)
            {
                case KillSignal killSignal:
                    OnKillSignalReceived(killSignal);
                    break;
                case VentSignal ventSignal:
                    OnVentSignalReceived(ventSignal);
                    break;
            }
        }

        public void SignalReceive(ISignalClass signal) => OnSignalReceived(signal);
    }
}