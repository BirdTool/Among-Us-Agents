using AMG.AI.LLM;
using AMG.AI.Services.ChatService.Llm;
using AMG.AI.Services.MessagesService;
using AMG.Utilities;
using AMG.Utilities.KeyDown;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace AMG;

[BepInAutoPlugin]
[BepInProcess("Among Us.exe")]
public partial class AMGPlugin : BasePlugin
{
    public Harmony Harmony { get; } = new(Id);
    public static AMGPlugin Plugin;
    public new static ManualLogSource Log;
    
    public static KeyDownManager KeyDownManager;

    public ConfigEntry<string> ConfigName { get; private set; }

    public static ConfigEntry<string> LlmPersona;

    public override void Load()
    {
        ModPaths.Initialize();
        LogManager.TransferLogsToAllLogs();

        Log = base.Log;
        Plugin = this;

        ConfigName = Config.Bind("Fake", "Name", ":>");

        var menuObject = new GameObject("AMGManager");
        Object.DontDestroyOnLoad(menuObject);
        menuObject.hideFlags = HideFlags.HideAndDontSave;

        Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<AMG.UI.ModUpdater>();
        menuObject.AddComponent<AMG.UI.ModUpdater>();

        LlmPersona = Config.Bind("AMG.LLM",
                                "Persona",
                                "",
                                "Extra persona for the LLM agents that speak in chat. Set it in game with .persona <text>.");
        AMG.AI.Services.ChatService.Llm.LlmChatService.CustomPersona = LlmPersona.Value;

        Harmony.PatchAll();

        LlmApiKeyLoader.LoadKeys();
        MessageServiceClass.StartListening();
        LlmCommandRegistry.RegisterAll(typeof(AMGPlugin).Assembly);
        // MessageServiceClass.OnReceive += OnMessageReceived;
    }

    /*
    private void OnMessageReceived(PrivateMessageModel message)
    {
        LogManager.LogInfo($"[Messages] Received new message from {message.Sender}: {message.Content}");

        var self = Utils.Players.LocalPlayer;
        var selfBrain = self?.GetComponent<StructuredAgentBrain>();
        if (selfBrain != null)
        {
            if (selfBrain.LlmMeeting != null && ChatMode.IsLlm)
            {
                selfBrain.LlmMeeting.AddDirective(message.Content);
            }
            else
            {
                LogManager.LogWarning("[Messages] Message not delivered: the local agent is not in LLM mode.");
            }
        }
        else
        {
            Utils.AddChatPrivate($"Message from the AI: {message.Content}");
        }
    }
    */
}