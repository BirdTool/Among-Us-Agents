using HarmonyLib;
using UnityEngine;
using System;

namespace AMG.AI.UI
{
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
    public static class HudManagerStartPatch
    {
        public static void Postfix(HudManager __instance)
        {
            if (__instance.UseButton == null) return;
            Navigation.WaypointManager.LoadWaypoints();

            GameObject buttonObj = UnityEngine.Object.Instantiate(__instance.UseButton.gameObject, __instance.UseButton.transform.parent);
            buttonObj.name = "SpawnAgentButton";

            buttonObj.transform.position = __instance.UseButton.transform.position + new Vector3(-1.5f, 0, 0);

            var actionButton = buttonObj.GetComponent<ActionButton>();
            if (actionButton != null)
            {
                UnityEngine.Object.Destroy(actionButton);
            }

            var passiveButton = buttonObj.GetComponent<PassiveButton>();
            if (passiveButton != null)
            {
                passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();

                passiveButton.OnClick.AddListener((Action)(() =>
                {
                    Debug.Log("[AI Agents] Botão visual clicado. Instanciando Agente...");
                    Control.AgentManager.AddAgent("Agente AI");
                }));
            }

            var renderer = buttonObj.GetComponent<SpriteRenderer>();
            if (renderer != null && __instance.ReportButton != null && __instance.ReportButton.graphic != null)
            {
                renderer.sprite = __instance.ReportButton.graphic.sprite;
            }

            buttonObj.SetActive(true);
        }
    }
}