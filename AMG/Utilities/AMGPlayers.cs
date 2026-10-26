using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

namespace AMG.Utilities
{
    public static class AMGPlayers
    {
        private static List<AMGPlayerData> Players { get; set; } = [];

        public static List<AMGPlayerData> SetAllPlayers()
        {
            Players.Clear();
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player != null)
                {
                    Players.Add(new AMGPlayerData(player));
                }
            }
            return Players;
        }

        public static List<AMGPlayerData> AlivePlayers => [.. Players.Where(p => p.IsAlive)];
        public static List<AMGPlayerData> DeadPlayers => [.. Players.Where(p => p.IsDead)];
        public static List<AMGPlayerData> Crewmates => [.. Players.Where(p => !IsImpostorRole(p.CurrentRole))];
        public static List<AMGPlayerData> Impostors => [.. Players.Where(p => IsImpostorRole(p.CurrentRole))];
        public static List<AMGPlayerData> AllPlayers => Players;

        public static void SetPlayerAsEjected(byte playerId) => GetPlayer(playerId).HasBeenEjected = true;
        
        public static AMGPlayerData GetPlayer(byte playerId) => Players.FirstOrDefault(p => p.PlayerId == playerId);
        public static AMGPlayerData GetPlayer(PlayerControl player) => GetPlayer(player.PlayerId);

        private static bool IsImpostorRole(RoleTypes role)
        {
            return role == RoleTypes.Impostor || role == RoleTypes.Viper || role == RoleTypes.Shapeshifter || role == RoleTypes.Phantom || role == RoleTypes.ImpostorGhost;
        }
    }

    public class AMGPlayerData(PlayerControl player)
    {
        public PlayerControl Player { get; } = player;

        public byte PlayerId { get; } = player.PlayerId;
        public string PlayerName { get; } = player.Data.PlayerName;
        public string PlayerColour { get; } = player.Data.ColorName;
        public int ClientId { get; } = player.GetClientId();
        public RoleTypes FirstRole { get; } = player.Data.Role.Role;

        public NetworkedPlayerInfo Data => GameData.Instance.GetPlayerById(PlayerId);

        public RoleTypes CurrentRole => Data != null && Data.Role != null ? Data.Role.Role : FirstRole;
        public bool Left => Data == null || Data.Disconnected; 
        public bool IsAlive => Data != null && !Data.IsDead && !Data.Disconnected;
        public bool IsDead => Data != null && Data.IsDead; 
        
        public bool HasBeenEjected { get; set; } = false;
    }
}