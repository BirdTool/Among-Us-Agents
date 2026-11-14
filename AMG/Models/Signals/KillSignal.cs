using AMG.Enums;
using AMG.Interfaces;
using System;

namespace AMG.Models.Signals
{
    public class KillSignal(PlayerControl killer, PlayerControl target, bool isDisguised = false, PlayerControl realKiller = null) : ISignalClass
    {
        public SignalsEnum Signal => SignalsEnum.KILL;

        // Who the witnesses SEE as the killer: when the killer is shapeshifted, the player he looks like.
        public PlayerControl Killer { get; } = killer;
        public PlayerControl Target { get; } = target;

        // The killer was disguised (shapeshifted): Killer is the player he looked like, not who really killed.
        public bool IsDisguised { get; } = isDisguised;

        // Who really killed. Only filled while MurderSignals.SendRealKillerForTests is on (testing): agents must not use it.
        public PlayerControl RealKiller { get; } = realKiller;
    }
}
