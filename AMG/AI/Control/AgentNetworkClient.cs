using AMG.Utilities;
using InnerNet;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.AI.Control
{
    public static class AgentNetworkClient
    {
        public static Dictionary<uint, AmongUsClient> ActiveNetworkBots = [];

        public static ClientData CreateNewAgentNetworkClient(string botName)
        {
            string targetIp = AmongUsClient.Instance.networkAddress;
            int targetPort = AmongUsClient.Instance.networkPort;
            int gameId = AmongUsClient.Instance.GameId;

            GameObject botNetworkObj = new($"BotNetworkClient_{botName}");
            GameObject.DontDestroyOnLoad(botNetworkObj);

            AmongUsClient botClient = botNetworkObj.AddComponent<AmongUsClient>();

            botClient.networkAddress = targetIp;
            botClient.networkPort = targetPort;
            botClient.mode = AmongUsClient.Instance.mode;
            botClient.NetworkMode = AmongUsClient.Instance.NetworkMode;
            botClient.GameId = gameId;
            botClient.ClientId = Utils.GetRandomInt(1000, 99999);

            string spoofedFriendCode = GenerateSpoofedFriendCode();

            LogManager.Log($"[AgentNetwork] Injetando bot {botName} [{spoofedFriendCode}] em {targetIp}:{targetPort}");

            /*
            string myRealCode = AccountManager.Instance.CheckFriendCodeAndUpdateVisuals;
            AccountManager.Instance.ShortUserCode = spoofedFriendCode;
            */

            botClient.Connect(botClient.mode, targetIp);

            /*
            AccountManager.Instance.ShortUserCode = myRealCode;
            */

            ActiveNetworkBots.Add((uint)ActiveNetworkBots.Count + 100, botClient);

            return AmongUsClient.Instance.GetClient(botClient.ClientId);
        }

        private static string GenerateSpoofedFriendCode()
        {
            string[] prefixes = ["photofore", "roundindex", "clumpvast", "ghostly", "brave"];
            string prefix = prefixes.GetRandomItemSecure();
            int number = Utils.GetRandomInt(1000, 9999);

            return $"{prefix}#{number}";
        }
    }
}