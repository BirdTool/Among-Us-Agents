using AMG.InternalRpc;
using AMG.Utilities;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AMG;

[BepInAutoPlugin]
[BepInProcess("Among Us.exe")]
public partial class AMGPlugin : BasePlugin
{
    public Harmony Harmony { get; } = new(Id);
    public static AMGPlugin Plugin;
    public new static ManualLogSource Log;

    public static readonly string version = "0.0.3";

    public ConfigEntry<string> ConfigName { get; private set; }

    public static ConfigEntry<string> MenuKeybind;
    public static ConfigEntry<string> MenuHtmlColor;
    public static ConfigEntry<bool> MenuOpenOnMouse;
    public static ConfigEntry<bool> MenuKeepSubwindowsOpen;

    public override void Load()
    {
        Log = base.Log;
        Plugin = this;

        ConfigName = Config.Bind("Fake", "Name", ":>");

        var menuObject = new GameObject("AMGManager");
        Object.DontDestroyOnLoad(menuObject);
        menuObject.hideFlags = HideFlags.HideAndDontSave;

        Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<AMG.UI.MenuUI>();
        menuObject.AddComponent<AMG.UI.MenuUI>();

        MenuKeybind = Config.Bind("AMG.GUI",
                               "Keybind",
                               "Delete",
                               "The keyboard key used to toggle the GUI on and off. List of supported keycodes: https://docs.unity3d.com/Packages/com.unity.tiny@0.16/api/Unity.Tiny.Input.KeyCode.html");

        MenuHtmlColor = Config.Bind("AMG.GUI",
                                "Color",
                                "",
                                "A custom color for your AMG GUI. Supports html color codes");

        MenuOpenOnMouse = Config.Bind("AMG.GUI",
                                "OpenOnMouse",
                                false,
                                "When enabled, the AMG GUI will always be opened at the current mouse position");

        MenuKeepSubwindowsOpen = Config.Bind("AMG.GUI",
                                "KeepSubwindowsOpen",
                                false,
                                "When enabled, closing the AMG GUI will not automatically close its subwindows");

        Harmony.PatchAll();

        LogManager.TransferLogsToAllLogs();
    }
}
