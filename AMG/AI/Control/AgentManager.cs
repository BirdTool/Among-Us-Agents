using AMG.AI.Mind;
using AMG.Utilities;
using InnerNet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AMG.AI.Control
{
    public static class AgentManager
    {
        public static readonly List<AgentListData> Agents = new List<AgentListData>();

        public static void AddAgent(PlayerControl agent, AgentData data)
        {
            if (Agents.Any(x => x.Control == agent)) return;
            Agents.Add(new AgentListData
            {
                Control = agent,
                Data = data
            });
        }

        public static void AddAgent(string name)
        {
            if (AmongUsClient.Instance == null || AmongUsClient.Instance.PlayerPrefab == null) return;

            PlayerControl agentComponent = Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);
            AgentData agentData = new() { Name = name };

            agentComponent.PlayerId = (byte)(100 + Agents.Count);
            agentComponent.NetId = (uint)(100 + Agents.Count);

            // agentComponent.isDummy = true;

            ClientData localClient = AmongUsClient.Instance.GetClient(AmongUsClient.Instance.ClientId);
            GameData.Instance.AddPlayer(agentComponent, localClient);

            var playerInfo = GameData.Instance.GetPlayerById(agentComponent.PlayerId);
            if (playerInfo != null)
            {
                playerInfo.PlayerName = name;
                playerInfo.DefaultOutfit.ColorId = 1;
            }

            if (PlayerControl.LocalPlayer != null)
            {
                Vector3 currentPos = PlayerControl.LocalPlayer.transform.position;
                agentComponent.transform.position = currentPos;

                if (agentComponent.NetTransform != null)
                {
                    agentComponent.NetTransform.SnapTo(currentPos);
                }
            }

            AddAgent(agentComponent, agentData);
            agentComponent.gameObject.AddComponent<AgentBrain>();
            Debug.Log($"[AI Agents] Agente '{name}' instanciado e pronto para a ação!");
        }

        public static void GenerateRandomAgent()
        {
            string name = $"Agent_{Agents.Count}";

            if (AmongUsClient.Instance == null || AmongUsClient.Instance.PlayerPrefab == null) return;

            PlayerControl agentComponent = Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);
            AgentData agentData = new() { Name = name };

            agentComponent.PlayerId = (byte)(100 + Agents.Count);
            agentComponent.NetId = (uint)(100 + Agents.Count);

            var playerInfo = GameData.Instance.GetPlayerById(agentComponent.PlayerId);
            if (playerInfo != null)
            {
                playerInfo.PlayerName = name;

                playerInfo.DefaultOutfit.ColorId = Utils.GetRandomInt(0, 17);

                string randomHat = GetRandomHat();
                playerInfo.DefaultOutfit.HatId = randomHat;
                playerInfo.Object.RpcSetHat(randomHat);

                string randomSkin = GetRandomSkin();
                playerInfo.DefaultOutfit.SkinId = randomSkin;
                playerInfo.Object.RpcSetSkin(randomSkin);

                string randomVisor = GetRandomVisor();
                playerInfo.DefaultOutfit.VisorId = randomVisor;
                playerInfo.Object.RpcSetVisor(randomVisor);
            }

            if (PlayerControl.LocalPlayer != null)
            {
                Vector3 currentPos = PlayerControl.LocalPlayer.transform.position;
                agentComponent.transform.position = currentPos;

                if (agentComponent.NetTransform != null)
                {
                    agentComponent.NetTransform.SnapTo(currentPos);
                }
            }

            AddAgent(agentComponent, agentData);
        }

        private static string GetRandomHat()
        {
            var hats = new List<string>
            {
                "hat_0", "hat_1", "hat_2", "hat_3", "hat_4", "hat_5", "hat_6", "hat_7",
                "hat_8", "hat_9", "hat_10", "hat_11", "hat_12", "hat_13", "hat_14",
                "hat_police", "hat_top", "hat_cap", "hat_crown", "hat_knight", "hat_banana",
                "hat_egg", "hat_santa", "hat_pumpkin", "hat_snowman", "hat_viking"
            };
            return hats[Utils.GetRandomInt(0, hats.Count - 1)];
        }

        private static string GetRandomSkin()
        {
            var skins = new List<string>
            {
                "skin_0", "skin_1", "skin_2", "skin_3", "skin_4", "skin_5", "skin_6",
                "skin_astronaut", "skin_captain", "skin_mechanic", "skin_military",
                "skin_police", "skin_sheriff", "skin_trooper", "skin_winter"
            };
            return skins[Utils.GetRandomInt(0, skins.Count - 1)];
        }

        private static string GetRandomVisor()
        {
            var visors = new List<string>
            {
                "visor_0", "visor_1", "visor_2", "visor_3", "visor_4", "visor_5",
                "visor_cool", "visor_eyebrow", "visor_glasses", "visor_monocle",
                "visor_sunglasses", "visor_tophat"
            };
            return visors[Utils.GetRandomInt(0, visors.Count - 1)];
        }
    }

    public class AgentListData
    {
        public PlayerControl Control;
        public AgentData Data;
    }
}