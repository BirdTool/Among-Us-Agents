using AMG.Enums;
using AMG.Interfaces;
using System;

namespace AMG.Models.Signals
{
    public class VentSignal(PlayerControl player, bool isLeaving) : ISignalClass
    {
        public SignalsEnum Signal => IsLeaving ? SignalsEnum.VENT_LEAVE : SignalsEnum.VENT_ENTER;
        
        public PlayerControl Player { get; } = player;

        public bool IsLeaving { get; } = isLeaving;
    }
}
