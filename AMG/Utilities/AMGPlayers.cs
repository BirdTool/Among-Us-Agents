using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

namespace AMG.Utilities
{
    public static class AMGPlayers
    {
        private static List<AMGPlayerData> Players { get; set; } = [];
        private static readonly List<MeetingResult> _meetings = [];

        public static IReadOnlyList<MeetingResult> Meetings => _meetings;

        public static List<AMGPlayerData> SetAllPlayers()
        {
            Players.Clear();
            _meetings.Clear();
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player != null && player.Data != null)
                {
                    Players.Add(new AMGPlayerData(player));
                }
            }
            return Players;
        }

        public static List<AMGPlayerData> AlivePlayers => [.. Players.Where(p => p.IsAlive)];
        public static List<AMGPlayerData> DeadPlayers => [.. Players.Where(p => p.IsDead)];
        public static List<AMGPlayerData> AllPlayers => Players;

        public static List<AMGPlayerData> Crewmates => [.. Players.Where(p => !IsImpostorRole(p.CurrentRole))];
        public static List<AMGPlayerData> Impostors => [.. Players.Where(p => IsImpostorRole(p.CurrentRole))];

        public static bool ConfirmEjects
            => GameOptionsManager.Instance != null
               && GameOptionsManager.Instance.CurrentGameOptions.GetBool(BoolOptionNames.ConfirmImpostor);

        public static MeetingResult RecordMeetingResult(NetworkedPlayerInfo exiled, bool tie)
        {
            bool confirm = ConfirmEjects;
            var result = new MeetingResult
            {
                Number = _meetings.Count + 1,
                Tie = tie,
                EjectedId = !tie && exiled != null ? exiled.PlayerId : null,
                ConfirmEjects = confirm,
                EjectedWasImpostor = confirm && !tie && exiled != null && exiled.Role != null ? exiled.Role.IsImpostor : null,
            };
            _meetings.Add(result);

            if (result.EjectedId is byte id)
            {
                var data = GetPlayer(id);
                if (data != null)
                {
                    data.HasBeenEjected = true;
                    data.EjectedInMeeting = result.Number;
                    data.EjectedWasImpostor = result.EjectedWasImpostor;
                }
            }
            return result;
        }

        public static AMGPlayerData GetPlayer(byte playerId) => Players.FirstOrDefault(p => p.PlayerId == playerId);
        public static AMGPlayerData GetPlayer(PlayerControl player) => player == null ? null : GetPlayer(player.PlayerId);

        private static bool IsImpostorRole(RoleTypes role)
        {
            return role == RoleTypes.Impostor || role == RoleTypes.Viper || role == RoleTypes.Shapeshifter || role == RoleTypes.Phantom || role == RoleTypes.ImpostorGhost;
        }
    }

    public class MeetingResult
    {
        public int Number { get; init; }
        public bool Tie { get; init; }
        public byte? EjectedId { get; init; }
        public bool ConfirmEjects { get; init; }
        public bool? EjectedWasImpostor { get; init; }
    }

    public class AMGPlayerData
    {
        private RoleTypes _lastKnownRole;

        public AMGPlayerData(PlayerControl player)
        {
            PlayerId = player.PlayerId;
            PlayerName = player.Data.PlayerName;
            PlayerColour = CleanColour(player.Data.ColorName);
            ClientId = player.GetClientId();
            FirstRole = player.Data.Role != null ? player.Data.Role.Role : RoleTypes.Crewmate;
            _lastKnownRole = FirstRole;
        }

        public byte PlayerId { get; }
        public string PlayerName { get; }
        // the game writes colours as "(Rose)": stored without the parentheses
        public string PlayerColour { get; }
        public int ClientId { get; }
        public RoleTypes FirstRole { get; }

        // The PlayerControl is destroyed when the player leaves, so it is looked up each time instead of stored.
        public PlayerControl Player => Utils.Players.GetPlayerByPlayerId(PlayerId);

        public NetworkedPlayerInfo Data => GameData.Instance?.GetPlayerById(PlayerId);

        public RoleTypes CurrentRole
        {
            get
            {
                if (Data != null && Data.Role != null) _lastKnownRole = Data.Role.Role;
                return _lastKnownRole;
            }
        }

        public bool Left => Data == null || Data.Disconnected;
        public bool IsAlive => Data != null && !Data.IsDead && !Data.Disconnected;
        public bool IsDead => Data != null && Data.IsDead;

        public bool HasBeenEjected { get; set; } = false;
        public int? EjectedInMeeting { get; set; }
        // only filled when "Confirm Ejects" was on
        public bool? EjectedWasImpostor { get; set; }

        // What EVERY player at the table knows about this player: safe to give to the LLM.
        public string PublicStatus
        {
            get
            {
                if (Left) return IsDead || HasBeenEjected ? "left the game (already dead)" : "left the game (disconnected, out of the game)";
                if (!IsDead) return "alive";
                if (!HasBeenEjected) return "dead (killed)";

                string meeting = EjectedInMeeting.HasValue ? $" in meeting #{EjectedInMeeting}" : string.Empty;
                return EjectedWasImpostor switch
                {
                    true => $"dead (ejected{meeting}, WAS an Impostor)",
                    false => $"dead (ejected{meeting}, was NOT an Impostor)",
                    _ => $"dead (ejected{meeting}, role not revealed)",
                };
            }
        }

        private static string CleanColour(string c)
        {
            c = c?.Trim().Trim('(', ')').Trim();
            return string.IsNullOrEmpty(c) ? "Unknown" : c;
        }
    }
}
