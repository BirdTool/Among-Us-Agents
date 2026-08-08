using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AMG.AI.Mind;
using AMG.Interfaces;
using AMG.Patches.RoundPatches;
using AmongUs.GameOptions;
using InnerNet;
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
        internal static ISabotage CurrentSabotage { get; set; } = null;

        public static Action<ISabotage> OnSabotageStarted;
        public static Action OnSabotageEnded;

        /// <summary>Fired whenever any door opens or closes. Consumers (e.g. path cache) subscribe to invalidate stale state.</summary>
        public static Action OnDoorStateChanged;

        internal static bool IsImpostorRole(RoleTypes role) => role == RoleTypes.Impostor || role == RoleTypes.Shapeshifter || role == RoleTypes.Viper || role == RoleTypes.Phantom;
        internal static bool IsCrewmateRole(RoleTypes role) => !IsImpostorRole(role);

        internal static int RemainingKills => Players.AllAliveCrewmates.Count() - Players.AllAliveImpostors.Count();

        public static int TotalTasks
        {
            get
            {
                int total = 0;
                foreach (var player in Players.AllCrewmates)
                    foreach (var task in player.myTasks)
                        total++;

                return total;
            }
        }

        public static int RemainingTasks
        {
            get
            {
                int remaining = 0;
                foreach (var player in Players.AllCrewmates)
                    foreach (var task in player.myTasks)
                        if (!task.IsComplete)
                            remaining++;

                return remaining;
            }
        }

        public static int CompletedTasks
        {
            get
            {
                int completed = 0;
                foreach (var player in Players.AllCrewmates)
                    foreach (var task in player.myTasks)
                        if (task.IsComplete)
                            completed++;

                return completed;
            }
        }

        public static float? SecondsSinceShipStart => IsShip ? Time.time - RoundPatches.ShipStartedAt : null;

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

        public static SystemTypes GetPlayerRoom(PlayerControl targetPlayer)
        {
            if (targetPlayer == null || ShipStatus.Instance == null)
            {
                return SystemTypes.Hallway;
            }

            Vector2 playerPos = targetPlayer.GetTruePosition();

            foreach (var room in ShipStatus.Instance.AllRooms)
            {
                if (room.roomArea != null && room.roomArea.OverlapPoint(playerPos))
                {
                    return room.RoomId;
                }
            }

            return SystemTypes.Hallway;
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
            return RandomizerExtensions.GetSecureRandomInt(min, max);
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

        public static bool CanSeeTheTarget(Vector2 origin, Vector2 destination, float maxDistance)
        {
            Vector2 raisedOrigin = new(origin.x, origin.y + 0.5f);
            Vector2 raisedDest = new(destination.x, destination.y + 0.5f);

            float actualDistance = Vector2.Distance(raisedOrigin, raisedDest);
            if (actualDistance > maxDistance) return false;

            Vector2 direction = (raisedDest - raisedOrigin).normalized;

            RaycastHit2D hit = Physics2D.Raycast(raisedOrigin, direction, actualDistance, Constants.ShadowMask);

            return hit.collider == null;
        }

        /// <summary>
        /// Call this whenever a door opens or closes so that path-cache consumers
        /// (e.g. Pathfinder) know to invalidate their cached routes.
        /// </summary>
        public static void NotifyDoorStateChanged()
        {
            OnDoorStateChanged?.Invoke();
        }

        /// <summary>
        /// Returns true when every door in <paramref name="room"/> is closed.
        /// Always reads live door state — no caching — so it is safe to call
        /// every frame from movement and parallel-decision checks.
        /// </summary>
        public static bool IsRoomClosed(SystemTypes room)
        {
            if (ShipStatus.Instance == null) return false;

            bool hasDoor = false;
            foreach (var door in ShipStatus.Instance.AllDoors)
            {
                if (door.Room != room) continue;
                hasDoor = true;
                if (door.IsOpen) return false; // Any open door → room accessible
            }

            // True only if at least one door exists AND none were open
            return hasDoor;
        }

        public static List<AgentBrain> GetAllBrains()
        {
            return [.. UnityEngine.Object.FindObjectsOfType<AgentBrain>()];
        }

        public static float GetDisturbTime(float delayTime, float multiplier)
        {
            if (multiplier <= 0f)
                return delayTime;

            float minDelay = Mathf.Max(0.05f, delayTime - (multiplier * 0.5f));

            float maxDelay = delayTime + multiplier;

            return RandomizerExtensions.GetSecureRandomFloat(minDelay, maxDelay);
        }
    }
}
