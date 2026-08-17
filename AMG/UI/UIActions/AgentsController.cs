using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using AMG.AI.Control;
using AMG.AI.Mind;
using AMG.AI.Tools;
using AMG.UI.Elements;
using AMG.Utilities;
using AMG.Utilities.MapUtils;
using AmongUs.GameOptions;
using InnerNet;
using Steamworks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AMG.UI.UIActions;

public static class AgentsController
{
    public static int AgentsCount { get; set; } = 1;
    public static int ImpostorCount { get; set; } = 2;

    public static List<byte> DefaultAvaibleColours { get; } = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17];

    public static void SpawnAgents()
    {
        if (!Utils.IsFreePlay) return;
        List<byte> avaibleColours = [.. DefaultAvaibleColours];

        for (int i = 0; i < AgentsCount; i++)
        {
            byte randomColour = avaibleColours.GetRandomItemSecureOrDefault((byte)255);
            if (randomColour == (byte)255)
            {
                avaibleColours = [.. DefaultAvaibleColours];
                i--;
                continue;
            }
            avaibleColours.Remove(randomColour);
        
            CreateAgent(randomColour);
        }

        for (int i = 0; i < ImpostorCount; i++)
        {
            var randomAgent = AgentManager.Agents.Where(a => !a.Control.Data.Role.IsImpostor).ToList().GetRandomItemSecureOrDefault();
            randomAgent?.Control.RpcSetRole(RoleTypes.Impostor);
        }

        TableSpawnLocations.MakeAllAgentsSpawnAtTable();
    }

    private static void CreateAgent(byte colour)
    {
        if (AmongUsClient.Instance == null || AmongUsClient.Instance.PlayerPrefab == null) return;

        PlayerControl agentComponent = null;

        if (AgentManager.RecycleDummies)
        {
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!player.isDummy) continue;

                if (AgentManager.Agents.Any(a => a.Control == player)) continue;

                agentComponent = player;
                break;
            }
        }

        bool isRecycled = agentComponent != null;
        if (!isRecycled)
        {
            agentComponent = Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);
            agentComponent.PlayerId = (byte)(100 + AgentManager.Agents.Count);
            agentComponent.NetId = (uint)(100 + AgentManager.Agents.Count);

            ClientData localClient = AmongUsClient.Instance.GetClient(AmongUsClient.Instance.ClientId);
            if (localClient != null)
                GameData.Instance.AddPlayer(agentComponent, localClient);
        }

        agentComponent.isDummy = false;

        agentComponent.myTasks ??= new Il2CppSystem.Collections.Generic.List<PlayerTask>();

        var playerInfo = GameData.Instance.GetPlayerById(agentComponent.PlayerId);

        var name = AgentManager.GenerateUniqueRandomName();
        if (playerInfo != null)
        {
            playerInfo.PlayerName = name;
            playerInfo.DefaultOutfit.ColorId = colour;

            playerInfo.Object.RpcSetColor(colour);

            string randomHat = AgentManager.GetRandomHat();
            playerInfo.DefaultOutfit.HatId = randomHat;
            playerInfo.Object.RpcSetHat(randomHat);

            string randomSkin = AgentManager.GetRandomSkin();
            playerInfo.DefaultOutfit.SkinId = randomSkin;
            playerInfo.Object.RpcSetSkin(randomSkin);

            string randomVisor = AgentManager.GetRandomVisor();
            playerInfo.DefaultOutfit.VisorId = randomVisor;
            playerInfo.Object.RpcSetVisor(randomVisor);
        }

        var pInfo = GameData.Instance?.GetPlayerById(agentComponent.PlayerId);

        if (PlayerControl.LocalPlayer != null)
        {
            Vector3 currentPos = PlayerControl.LocalPlayer.transform.position;
            agentComponent.transform.position = currentPos;
            agentComponent.NetTransform?.SnapTo(currentPos);
        }

        agentComponent.SetColor(playerInfo.DefaultOutfit.ColorId);

        AgentData agentData = new() { Name = name };
        AgentManager.AddAgent(agentComponent, agentData);
        agentComponent.gameObject.AddComponent<AgentBrain>();
        var brain = agentComponent.gameObject.GetComponent<AgentBrain>();

        TaskAssignment.AssignTasks(agentComponent);
        brain.MapGameTasksToAILogic();
    }
}