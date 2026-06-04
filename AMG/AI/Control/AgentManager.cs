using AMG.AI.Mind;
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
    }

    public class AgentListData
    {
        public PlayerControl Control;
        public AgentData Data;
    }
}