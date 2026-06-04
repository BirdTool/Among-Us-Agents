using AMG.Utilities;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Reactor;
using UnityEngine;

namespace AMG;

[BepInAutoPlugin]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
public partial class AMGPlugin : BasePlugin
{
    public Harmony Harmony { get; } = new(Id);
    public static AMGPlugin Plugin;
    public new static ManualLogSource Log;


    public ConfigEntry<string> ConfigName { get; private set; }
    public static bool IsPanicked = false;
    public static bool InStealthMode = false;

    public static ConfigEntry<string> MenuKeybind;
    public static ConfigEntry<string> MenuHtmlColor;
    public static ConfigEntry<bool> MenuOpenOnMouse;
    public static ConfigEntry<bool> MenuKeepSubwindowsOpen;
    public static ConfigEntry<string> SpoofLevel;
    public static ConfigEntry<string> SpoofPlatform;
    public static ConfigEntry<bool> SpoofDeviceId;
    public static ConfigEntry<bool> NoTelemetry;
    public static ConfigEntry<string> GuestFriendCode;
    public static ConfigEntry<bool> GuestMode;
    public static ConfigEntry<bool> AutoLoadProfile;
    public static ConfigEntry<string> ConfigEditor;

    public static string ReaperVersion { get; private set; } = "1.0.0";

    public override void Load()
    {
        Log = base.Log;
        Plugin = this;

        ConfigName = Config.Bind("Fake", "Name", ":>");

        var menuObject = new GameObject("ReaperMenuManager");
        Object.DontDestroyOnLoad(menuObject);

        MenuKeybind = Config.Bind("ReaperMenu.GUI",
                               "Keybind",
                               "Delete",
                               "The keyboard key used to toggle the GUI on and off. List of supported keycodes: https://docs.unity3d.com/Packages/com.unity.tiny@0.16/api/Unity.Tiny.Input.KeyCode.html");

        MenuHtmlColor = Config.Bind("MalumMenu.GUI",
                                "Color",
                                "",
                                "A custom color for your MalumMenu GUI. Supports html color codes");

        MenuOpenOnMouse = Config.Bind("MalumMenu.GUI",
                                "OpenOnMouse",
                                false,
                                "When enabled, the MalumMenu GUI will always be opened at the current mouse position");

        MenuKeepSubwindowsOpen = Config.Bind("MalumMenu.GUI",
                                "KeepSubwindowsOpen",
                                false,
                                "When enabled, closing the MalumMenu GUI will not automatically close its subwindows");

        AutoLoadProfile = Config.Bind("MalumMenu.Profile",
                                "AutoLoadProfile",
                                false,
                                "When enabled, your saved keybind and toggle profile will be automatically loaded at game startup");

        ConfigEditor = Config.Bind("MalumMenu.Config",
                                "ConfigEditor",
                                "notepad.exe",
                                "The program used to open the config file when using the Open Config toggle. Can be any executable, but using a text editor is recommended");

        Harmony.PatchAll();

        LogManager.TransferLogsToAllLogs();
    }

    /*
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    public static class ExamplePatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            __instance.cosmetics.nameText.text = PluginSingleton<ReaperMenuPlugin>.Instance.ConfigName.Value;
        }
    }
    */
}
