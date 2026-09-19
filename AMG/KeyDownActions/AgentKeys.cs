using System.Collections.Generic;
using AMG.AI.Control;
using AMG.AI.Control.AgentController;
using AMG.AI.Mind;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Navigation;
using AMG.Utilities;
using AMG.Utilities.KeyDown;
using UnityEngine;

namespace AMG.KeyDownActions
{
    [KeyPressManager.Register(KeyCode.M)]
    public static class LogPositionKey
    {
        public static void Execute()
        {
            var currentPos = PlayerControl.LocalPlayer.transform.position;
            LogManager.Log($"[Vector2] Current Position: x: {currentPos.x}, y: {currentPos.y}");
        }
    }

    [KeyPressManager.Register(KeyCode.G)]
    public static class CommandAllAgentsKey
    {
        public static void Execute()
        {
            LogManager.LogDebug("[AI Command] Chamando todos os agentes!");

            Vector2 myPosition = PlayerControl.LocalPlayer.transform.position;
            Waypoint target = Pathfinder.GetClosestNode(myPosition);

            foreach (var agent in AgentManager.Agents)
            {
                var brain = agent.Control.GetComponent<StructuredAgentBrain>();
                if (brain == null) continue;

                Waypoint start = Pathfinder.GetClosestNode(agent.Control.transform.position);
                List<Waypoint> path = Pathfinder.FindPath(start, target, out _);
                brain.CommandGoToPath(path);
            }
        }
    }

    [KeyPressManager.Register(KeyCode.T)]
    public static class ToggleImpostorKey
    {
        public static void Execute()
        {
            AgentManager.WillBeImpostor = !AgentManager.WillBeImpostor;
            LogManager.LogDebug($"[AI Manager] Will be impostor: {AgentManager.WillBeImpostor}");
        }
    }

    [KeyPressManager.Register(KeyCode.K)]
    public static class SetCalculatingKey
    {
        public static void Execute()
        {
            AgentsCommander.SetAllAgentAsCalculating();
        }
    }

    [KeyPressManager.Register(KeyCode.J)]
    public static class GoToClosestVentKey
    {
        public static void Execute()
        {
            AgentsCommander.MakeAllAgentsDoTask();
        }
    }

    [KeyPressManager.Register(KeyCode.N)]
    public static class SpawnRealPlayerAgentKey
    {
        public static void Execute()
        {
            if (AmongUsClient.Instance == null || AmongUsClient.Instance.PlayerPrefab == null) return;

            PlayerControl agentComponent = Utils.Players.LocalPlayer;
            AgentData agentData = new() { Name = agentComponent.Data.PlayerName };
            AgentManager.AddAgent(agentComponent, agentData);

            agentComponent.gameObject.AddComponent<StructuredAgentBrain>();
            var brain = agentComponent.gameObject.GetComponent<StructuredAgentBrain>();

            AgentController.AgentControlsRealPlayer = true;
            brain.MapArtificialTasks();
            brain.MapGameTasksToAILogic();
        }
    }
}