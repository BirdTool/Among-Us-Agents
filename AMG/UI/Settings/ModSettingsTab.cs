using System;
using System.Collections.Generic;
using AMG.AI.Services.ChatService;
using AMG.AI.Services.ChatService.Llm;
using AMG.AI.Services.MessagesService;
using AMG.Data;
using AMG.Enums.ChatServiceEnums;
using AMG.Enums.LLMEnums;
using AMG.UI.UIActions;
using AMG.Utilities;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AMG.UI.Settings
{
    [HarmonyPatch]
    public static class ModSettingsTab
    {
        private const string TabName = "AMG";

        [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
        [HarmonyPostfix]
        public static void Start_Postfix(OptionsMenuBehaviour __instance)
        {
            if (__instance.DisableMouseMovement == null) return;

            var tab = CreateTab(__instance);
            Fill(new SettingsItems(__instance, tab.Content.transform));
            PlaceTabs(__instance);
        }

        private static void Fill(SettingsItems items)
        {
            items.Stepper("Agents", 1, 15, () => AgentsController.AgentsCount, value => AgentsController.AgentsCount = value);
            items.Stepper("Impostors", 1, 3, () => AgentsController.ImpostorCount, value => AgentsController.ImpostorCount = value);

            items.Cycle("Identity", Enum.GetNames(typeof(PrivateMessageSenderEnum)),
                () => (int)GlobalVariables.MyIdentity, index => GlobalVariables.MyIdentity = (PrivateMessageSenderEnum)index);

            items.Cycle("Chat mode", Enum.GetNames(typeof(ChatModeEnum)),
                () => (int)ChatMode.Current, index => ChatMode.Current = (ChatModeEnum)index);

            items.Cycle("LLM effort", Enum.GetNames(typeof(LlmEffortEnum)),
                () => (int)LlmChatService.DefaultEffort, index => LlmChatService.DefaultEffort = (LlmEffortEnum)index);

            items.Toggle("LLM dry run", () => ChatMode.LlmDryRun, state => ChatMode.LlmDryRun = state);
            items.Toggle("Chat debug", () => ChatMode.ChatDebug, state => ChatMode.ChatDebug = state);

            // spawning only works in freeplay: out of it the button is grey
            items.Button("Spawn agents", AgentsController.SpawnAgents, () => Utils.IsFreePlay);
        }

        private static TabGroup CreateTab(OptionsMenuBehaviour menu)
        {
            // the last tab of the game is the template of ours
            var template = menu.Tabs[^1];
            var tab = Object.Instantiate(template, template.transform.parent);

            tab.name = $"{TabName}Button";
            foreach (var translator in tab.GetComponentsInChildren<TextTranslatorTMP>(true))
                Object.Destroy(translator);
            tab.GetComponentInChildren<TextMeshPro>(true)?.SetText(TabName);
            tab.gameObject.SetActive(true);

            var content = new GameObject($"{TabName}Tab");
            content.SetActive(false);
            content.transform.SetParent(tab.Content.transform.parent);
            content.transform.localPosition = Vector3.zero;
            content.transform.localScale = Vector3.one;
            tab.Content = content;

            var tabs = new List<TabGroup>(menu.Tabs) { tab };
            menu.Tabs = tabs.ToArray();

            int index = menu.Tabs.Length - 1;
            var button = tab.GetComponent<PassiveButton>();
            button.OnClick = new();
            button.OnClick.AddListener((Action)(() =>
            {
                tab.Rollover.SetEnabledColors();
                menu.OpenTabGroup(index);
            }));

            return tab;
        }

        // The extra tab needs room: spread the tab buttons evenly.
        private static void PlaceTabs(OptionsMenuBehaviour menu)
        {
            const float spacing = 0.6f;
            const float width = 1.0f;
            float y = PlayerControl.LocalPlayer == null ? 0f : 2.5f;

            int active = 0;
            foreach (var tab in menu.Tabs)
                if (tab.gameObject.activeInHierarchy) active++;

            if (active == 0) return;

            float total = (active - 1) * spacing + active * width;
            float x = -total / 2f + width / 2f;

            int index = 0;
            foreach (var tab in menu.Tabs)
            {
                if (!tab.gameObject.activeInHierarchy) continue;

                tab.transform.localPosition = new Vector3(x + index * (width + spacing), y, -1f);
                index++;
            }
        }
    }
}
