using AmongUs.GameOptions;
using InnerNet;
using Sentry.Internal.Extensions;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace AMG.Utilities
{
    public static partial class Utils
    {
        internal static ReferenceDataManager ReferenceDataManager = DestroyableSingleton<ReferenceDataManager>.Instance; // Useful for getting full lists of all the Among Us cosmetics IDs
        internal static SabotageSystemType SabotageSystem => ShipStatus.Instance.Systems[SystemTypes.Sabotage].Cast<SabotageSystemType>();
        internal static bool IsShip => ShipStatus.Instance;
        internal static bool IsLobby => AmongUsClient.Instance && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Joined && !IsFreePlay;
        internal static bool IsOnlineGame => AmongUsClient.Instance && AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame;
        internal static bool IsLocalGame => AmongUsClient.Instance && AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame;
        internal static bool IsFreePlay => AmongUsClient.Instance && AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay;
        internal static bool IsPlayer => PlayerControl.LocalPlayer;
        internal static bool IsHost => AmongUsClient.Instance && AmongUsClient.Instance.AmHost;
        internal static bool IsInGame => AmongUsClient.Instance && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started && IsPlayer;
        internal static bool IsMeeting => MeetingHud.Instance;
        internal static bool IsMeetingVoting => IsMeeting && MeetingHud.Instance.state is MeetingHud.VoteStates.Voted or MeetingHud.VoteStates.NotVoted;
        internal static bool IsMeetingProceeding => IsMeeting && MeetingHud.Instance.state is MeetingHud.VoteStates.Proceeding;
        internal static bool IsExiling => ExileController.Instance && !(IsAirshipMap && SpawnInMinigame.Instance.isActiveAndEnabled);
        internal static bool IsAnySabotageActive => ShipStatus.Instance && SabotageSystem.AnyActive;
        internal static bool IsNormalGame => GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.Normal;
        internal static bool IsHideNSeek => GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.HideNSeek;
        internal static bool IsSkeldMap => (MapNames)GetCurrentMapID() == MapNames.Skeld;
        internal static bool IsMiraHQMap => (MapNames)GetCurrentMapID() == MapNames.MiraHQ;
        internal static bool IsPolusMap => (MapNames)GetCurrentMapID() == MapNames.Polus;
        internal static bool IsDleksMap => (MapNames)GetCurrentMapID() == MapNames.Dleks; // Skeld but inverted
        internal static bool IsAirshipMap => (MapNames)GetCurrentMapID() == MapNames.Airship;
        internal static bool IsFungleMap => (MapNames)GetCurrentMapID() == MapNames.Fungle;

        internal static bool IsImpostorRole(RoleTypes role) => role == RoleTypes.Impostor || role == RoleTypes.Shapeshifter || role == RoleTypes.Viper || role == RoleTypes.Phantom;
        internal static bool IsCrewmateRole(RoleTypes role) => !IsImpostorRole(role);

        public static byte GetCurrentMapID()
        {
            // Works for the tutorial
            if (IsFreePlay)
            {
                return (byte)AmongUsClient.Instance.TutorialMapId;
            }

            // Works for local / online games
            if (GameOptionsManager.Instance?.currentGameOptions != null)
            {
                return GameOptionsManager.Instance.currentGameOptions.MapId;
            }

            // Defaults to byte.MaxValue if the current map ID is unavailable
            return byte.MaxValue;
        }

        // Gets SystemType of the room the player is currently in
        public static SystemTypes GetCurrentRoom()
        {
            return HudManager.Instance.roomTracker.LastRoom.RoomId;
        }

        public static KeyCode StringToKeycode(string keyCodeStr)
        {

            if (!string.IsNullOrEmpty(keyCodeStr)) // Empty strings are automatically invalid
            {
                try
                {
                    // Case-insensitive parse of UnityEngine.KeyCode to check if string is valid
                    KeyCode keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), keyCodeStr, true);

                    return keyCode;

                }

                catch { }
            }

            return KeyCode.Delete; // If string is invalid, return Delete as the default key
        }

        public static void ShowPopup(string text)
        {
            var popup = UnityEngine.Object.Instantiate(DiscordManager.Instance.discordPopup, Camera.main!.transform);

            var background = popup.transform.Find("Background").GetComponent<SpriteRenderer>();
            var size = background.size;
            size.x *= 2.5f;
            background.size = size;

            popup.TextAreaTMP.fontSizeMin = 2;
            popup.Show(text);
        }

        public static bool IsRealHost()
        {
            if (AmongUsClient.Instance == null || PlayerControl.LocalPlayer == null)
                return false;

            return AmongUsClient.Instance.ClientId == AmongUsClient.Instance.HostId;
        }

        public static void OpenConfigFile()
        {
            var configFilePath = AMGPlugin.Plugin.Config.ConfigFilePath;
            var configEditor = AMGPlugin.ConfigEditor.Value;

            if (!string.IsNullOrWhiteSpace(configEditor))
            {
                if (File.Exists(configFilePath))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = configEditor,
                            Arguments = configFilePath,
                            UseShellExecute = true
                            //Verb = "edit"
                        });
                    }
                    catch (Exception ex)
                    {
                        AMGPlugin.Log.LogError(ex.Message);
                    }
                }
                else
                {
                    AMGPlugin.Log.LogError("Configuration file does not exist");
                }
            }
            else
            {
                AMGPlugin.Log.LogError("Configuration editor not specified");
            }
        }

        public static RoleBehaviour GetBehaviourByRoleType(RoleTypes roleType)
        {
            return RoleManager.Instance.AllRoles.ToArray().First(r => r.Role == roleType);
        }

        public static RoleBehaviour GetBehaviourByTeamType(RoleTeamTypes roleTeamType)
        {
            RoleTypes roleType = (RoleTypes)Enum.Parse(typeof(RoleTypes), roleTeamType.ToString(), true);
            RoleBehaviour role = GetBehaviourByRoleType(roleType);

            return role;
        }

        public static string PlatformTypeToString(Platforms platform)
        {
            return platform switch
            {
                Platforms.StandaloneEpicPC => "Epic Games",
                Platforms.StandaloneSteamPC => "Steam",
                Platforms.StandaloneMac => "Mac",
                Platforms.StandaloneWin10 => "Microsoft Store",
                Platforms.StandaloneItch => "Itch.io",
                Platforms.IPhone => "iPhone / iPad",
                Platforms.Android => "Android",
                Platforms.Switch => "Nintendo Switch",
                Platforms.Xbox => "Xbox",
                Platforms.Playstation => "PlayStation",
                (Platforms)112 => "Starlight",
                _ => "Unknown"
            };
        }

        // Gets the name for a specified player's role as a string
        // Strings are automatically translated
        public static string GetRoleName(NetworkedPlayerInfo playerData)
        {
            var translatedRole = DestroyableSingleton<TranslationController>.Instance.GetString(playerData.Role.StringName, Il2CppSystem.Array.Empty<Il2CppSystem.Object>());
            if (translatedRole != "STRMISS") return translatedRole;

            translatedRole = DestroyableSingleton<TranslationController>.Instance.GetString(GetBehaviourByTeamType(playerData.Role.TeamType).StringName, Il2CppSystem.Array.Empty<Il2CppSystem.Object>());
            return translatedRole;
        }
        public static void AdjustResolution()
        {
            ResolutionManager.ResolutionChanged.Invoke((float)Screen.width / Screen.height, Screen.width, Screen.height, Screen.fullScreen);
        }

        public static int GetRandomInt(int min, int max)
        {
            return UnityEngine.Random.Range(min, max);
        }

        public static bool ExecuteProbability(double chance)
        {
            chance = Math.Clamp(chance, 0, 1);
            return RandomizerExtensions.GetSecureRandomInt(0, 100) < chance * 100;
        }

        public static bool ExecuteProbability(float chance)
        {
            chance = Math.Clamp(chance, 0, 1);
            return RandomizerExtensions.GetSecureRandomInt(0, 100) < chance * 100;
        }

        public static bool ExecuteProbability(int chance)
        {
            chance = Math.Clamp(chance, 0, 100);
            return RandomizerExtensions.GetSecureRandomInt(0, 100) < chance;
        }

        public static bool CanSeeTheTarget(Vector2 origin, Vector2 destination, float distance)
        {
            Vector2 direction = (destination - origin).normalized;

            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, distance);

            foreach (var hit in hits)
            {
                if (hit.collider.isTrigger) continue;

                if (hit.collider.gameObject.GetComponent<PlayerControl>() != null) continue;

                return false;
            }

            return true;
        }
    }
}