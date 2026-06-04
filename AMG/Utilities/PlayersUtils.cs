using System.Collections.Generic;
using System.Linq;

namespace AMG.Utilities
{
    public static partial class Utils
    {
        public static class Players
        {
            internal static PlayerControl LocalPlayer => PlayerControl.LocalPlayer;

            internal static Il2CppSystem.Collections.Generic.List<PlayerControl> AllPlayersIl2Cpp => PlayerControl.AllPlayerControls;

            internal static IEnumerable<PlayerControl> AllPlayers
                => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.Data != null);
            internal static IEnumerable<PlayerControl> AllPlayersNotMe => AllPlayers.Where(player => player != LocalPlayer);

            internal static PlayerControl GetPlayerByPlayerId(byte playerId) => AllPlayers.FirstOrDefault(player => player.PlayerId == playerId);

            internal static PlayerControl GetPlayerByNetId(uint netId) => AllPlayers.FirstOrDefault(player => player.NetId == netId);

            internal static PlayerControl GetPlayerByName(string name) => AllPlayers.FirstOrDefault(player => player.Data.PlayerName == name);

            internal static IEnumerable<PlayerControl> AllAlivePlayers => AllPlayers.Where(player => !player.Data.IsDead);
            internal static IEnumerable<PlayerControl> AllDeadPlayers => AllPlayers.Where(player => player.Data.IsDead);

            internal static IEnumerable<PlayerControl> AllAlivePlayerNotMe => AllAlivePlayers.Where(player => player != LocalPlayer);
            internal static IEnumerable<PlayerControl> AllDeadPlayerNotMe => AllDeadPlayers.Where(player => player != LocalPlayer);


            internal static IEnumerable<PlayerControl> AllImpostors => AllPlayers.Where(player => player.Data.Role != null && player.Data.Role.IsImpostor);
            internal static IEnumerable<PlayerControl> AllCrewmates => AllPlayers.Where(player => player.Data.Role != null && !player.Data.Role.IsImpostor);

            internal static IEnumerable<PlayerControl> AllImpostorsNotMe => AllImpostors.Where(player => player != LocalPlayer);
            internal static IEnumerable<PlayerControl> AllCrewmatesNotMe => AllCrewmates.Where(player => player != LocalPlayer);

            internal static IEnumerable<PlayerControl> AllDeadImpostors => AllImpostors.Where(player => player.Data.IsDead);
            internal static IEnumerable<PlayerControl> AllDeadCrewmates => AllCrewmates.Where(player => player.Data.IsDead);

            internal static IEnumerable<PlayerControl> AllAliveImpostors => AllImpostors.Where(player => !player.Data.IsDead);
            internal static IEnumerable<PlayerControl> AllAliveCrewmates => AllCrewmates.Where(player => !player.Data.IsDead);

            internal static IEnumerable<PlayerControl> AllDeadImpostorsNotMe => AllDeadImpostors.Where(player => player != LocalPlayer);
            internal static IEnumerable<PlayerControl> AllDeadCrewmatesNotMe => AllDeadCrewmates.Where(player => player != LocalPlayer);

            internal static IEnumerable<PlayerControl> AllAliveImpostorsNotMe => AllAliveImpostors.Where(player => player != LocalPlayer);
            internal static IEnumerable<PlayerControl> AllAliveCrewmatesNotMe => AllAliveCrewmates.Where(player => player != LocalPlayer);
        }
    }
}