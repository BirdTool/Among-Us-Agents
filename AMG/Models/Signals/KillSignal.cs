using AMG.Enums;
using AMG.Interfaces;
using System;

namespace AMG.Models.Signals
{
    public class KillSignal(PlayerControl killer, PlayerControl target) : ISignalClass
    {
        public SignalsEnum Signal => SignalsEnum.KILL;
        
        public PlayerControl Killer { get;} = killer;
        public PlayerControl Target { get;} = target;
    }
}
