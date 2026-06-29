using AMG.AI.Mind;
using AMG.AI.Tools;
using AMG.Utilities;
using AmongUs.GameOptions;
using InnerNet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AMG.AI.Control
{
    public static class AgentManager
    {
        public static readonly List<AgentListData> Agents = [];
        public static bool RecycleDummies = true;

        private static readonly List<string> FirstNames = new()
        {
            "James", "John", "Robert", "Michael", "William", "David", "Richard", "Joseph",
            "Charles", "Thomas", "Mary", "Patricia", "Jennifer", "Linda", "Elizabeth",
            "Barbara", "Susan", "Jessica", "Sarah", "Karen", "Crewmate", "Impostor",
            "Cristiano", "Luna", "Luar", "Lua", "Léo", "Leonardo", "Cassilhas"
        };

        private static readonly List<string> Surnames = new()
        {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
            "Rodriguez", "Martinez", "Carsion", "Doe", "Silva", "Santos", "Oliveira", "Toretto",
            "Santos", "Máfia", "Giuseppe", "Morteiro", "Besta", "Gigante", "Giant", "Sol"
        };

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
            CreateAgentInternal(name, false);
        }

        public static void GenerateRandomAgent()
        {
            string randomName = GenerateUniqueRandomName();
            CreateAgentInternal(randomName, true);
        }

        private static void CreateAgentInternal(string name, bool randomizeCosmetics)
        {
            if (AmongUsClient.Instance == null || AmongUsClient.Instance.PlayerPrefab == null) return;

            PlayerControl dummie = null;
            if (RecycleDummies)
            {
                foreach ( PlayerControl player in PlayerControl.AllPlayerControls )
                {
                    if (!player.isDummy) continue;
                    dummie = player;
                }
            }

            PlayerControl agentComponent = dummie ?? Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);
            agentComponent.isDummy = false;

            if (agentComponent.myTasks == null)
                agentComponent.myTasks = new Il2CppSystem.Collections.Generic.List<PlayerTask>();

            agentComponent.PlayerId = (byte)(100 + Agents.Count);
            agentComponent.NetId = (uint)(100 + Agents.Count);

            ClientData localClient = AmongUsClient.Instance.GetClient(AmongUsClient.Instance.ClientId);
            if (localClient != null)
                GameData.Instance.AddPlayer(agentComponent, localClient);

            TaskAssignment.AssignTasks(agentComponent);

            var playerInfo = GameData.Instance.GetPlayerById(agentComponent.PlayerId);
            if (playerInfo != null)
            {
                playerInfo.PlayerName = name;

                if (randomizeCosmetics)
                {
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
                else
                {
                    playerInfo.DefaultOutfit.ColorId = 1;
                }
            }

            agentComponent.RpcSetRole(RoleTypes.Crewmate);

            var pInfo = GameData.Instance?.GetPlayerById(agentComponent.PlayerId);
            LogManager.LogDebug($"[AgentCreate] Bot PlayerId={agentComponent.PlayerId}, IsImpostor={pInfo?.Role?.IsImpostor}, Tasks count={pInfo?.Tasks?.Count}");

            if (PlayerControl.LocalPlayer != null)
            {
                Vector3 currentPos = PlayerControl.LocalPlayer.transform.position;
                agentComponent.transform.position = currentPos;
                agentComponent.NetTransform?.SnapTo(currentPos);
            }

            AgentData agentData = new() { Name = name };
            AddAgent(agentComponent, agentData);
            agentComponent.gameObject.AddComponent<AgentBrain>();
            var brain = agentComponent.gameObject.GetComponent<AgentBrain>();
            brain.MapGameTasksToAILogic();
            LogManager.Log($"[AI Agents] Agente '{name}' instanciado e pronto para a ação!");
        }

        public static void ClearAllAgents()
        {
            foreach (var agent in Agents)
            {
                if (agent.Control != null && agent.Control.gameObject != null)
                {
                    Object.Destroy(agent.Control.gameObject);
                }
            }

            Agents.Clear();
            LogManager.Log("[AI Agents] Partida encerrada/abandonada. Todos os agentes foram deletados.");
        }

        public static string GenerateUniqueRandomName()
        {
            string firstName = FirstNames[Utils.GetRandomInt(0, FirstNames.Count - 1)];
            string surname = Surnames[Utils.GetRandomInt(0, Surnames.Count - 1)];
            string baseName = $"{firstName} {surname}";

            string finalName = baseName;
            int repetitionCount = 1;

            while (Agents.Any(a => a.Data != null && a.Data.Name == finalName))
            {
                finalName = $"{baseName} {repetitionCount}";
                repetitionCount++;
            }

            return finalName;
        }

        public static AgentData GetAgentData(PlayerControl agent)
        {
            var agentInfo = Agents.FirstOrDefault(x => x.Control == agent);
            return agentInfo?.Data;
        }

        public static AgentData GetAgentData(byte id)
        {
            var agentInfo = Agents.FirstOrDefault(x => x.Control.PlayerId == id);
            return agentInfo?.Data;
        }

        private static string GetRandomHat()
        {
            if (HatManager.Instance == null || HatManager.Instance.AllHats == null) return "";

            var allHatsArray = HatManager.Instance.AllHats.TryCast<Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<HatData>>();
            if (allHatsArray != null)
            {
                if (allHatsArray.Length == 0) return "";
                return allHatsArray[Utils.GetRandomInt(0, allHatsArray.Length - 1)].name;
            }

            var allHatsList = HatManager.Instance.AllHats.TryCast<Il2CppSystem.Collections.Generic.List<HatData>>();
            if (allHatsList != null)
            {
                if (allHatsList.Count == 0) return "";
                return allHatsList[Utils.GetRandomInt(0, allHatsList.Count - 1)].name;
            }

            return "";
        }

        private static string GetRandomSkin()
        {
            if (HatManager.Instance == null || HatManager.Instance.AllSkins == null) return "";

            var allSkinsArray = HatManager.Instance.AllSkins.TryCast<Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<SkinData>>();
            if (allSkinsArray != null)
            {
                if (allSkinsArray.Length == 0) return "";
                return allSkinsArray[Utils.GetRandomInt(0, allSkinsArray.Length - 1)].name;
            }

            var allSkinsList = HatManager.Instance.AllSkins.TryCast<Il2CppSystem.Collections.Generic.List<SkinData>>();
            if (allSkinsList != null)
            {
                if (allSkinsList.Count == 0) return "";
                return allSkinsList[Utils.GetRandomInt(0, allSkinsList.Count - 1)].name;
            }

            return "";
        }

        private static string GetRandomVisor()
        {
            if (HatManager.Instance == null || HatManager.Instance.AllVisors == null) return "";

            var allVisorsArray = HatManager.Instance.AllVisors.TryCast<Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<VisorData>>();
            if (allVisorsArray != null)
            {
                if (allVisorsArray.Length == 0) return "";
                return allVisorsArray[Utils.GetRandomInt(0, allVisorsArray.Length - 1)].name;
            }

            var allVisorsList = HatManager.Instance.AllVisors.TryCast<Il2CppSystem.Collections.Generic.List<VisorData>>();
            if (allVisorsList != null)
            {
                if (allVisorsList.Count == 0) return "";
                return allVisorsList[Utils.GetRandomInt(0, allVisorsList.Count - 1)].name;
            }

            return "";
        }
    }

    public class AgentListData
    {
        public PlayerControl Control;
        public AgentData Data;
    }
}