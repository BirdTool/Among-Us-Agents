using AMG.Interfaces;
using AMG.Models.Signals;
using AMG.Utilities;

namespace AMG.AI.Mind.LLMAgentBrain
{
    public partial class LLMAgentBrain
    {
        public void SignalReceive(ISignalClass signal)
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

        private void OnKillSignalReceived(KillSignal signal)
        {
            var AMGKiller = AMGPlayers.GetPlayer(signal.Killer);
            var AMGVictim = AMGPlayers.GetPlayer(signal.Target);
            AddSawKiling(AMGKiller, AMGVictim);
        }

        private void OnVentSignalReceived(VentSignal signal)
        {
            var AMGVentPlayer = AMGPlayers.GetPlayer(signal.Player);
            AddSawPlayerVenting(signal.IsLeaving, AMGVentPlayer);
        }
    }
}