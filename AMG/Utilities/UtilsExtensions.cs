using System;
using System.Linq;
using AMG.AI.Navigation;
using AMG.Interfaces;
using InnerNet;
using UnityEngine;

namespace AMG.Utilities
{
    public static class UtilsExtensions
    {
        public static Waypoint GetClosestNode(this Vector2 position)
        {
            return Pathfinder.GetClosestNode(position);
        }

        public static Waypoint GetClosestNode(this Vector3 position)
        {
            return Pathfinder.GetClosestNode(position);
        }


        public static bool CompleteSabotage(this ISabotage sabotage, ShipStatus shipStatus)
        {
            if (!sabotage.IsAllStepsCompleted()) return false;

            Utils.Sabotages.RepairSabotages(shipStatus);

            return true;
        }

        public static bool IsAllStepsCompleted(this ISabotage sabotage)
        {
            var steps = sabotage.GetSteps();
            return steps.All(s => s.IsCompleted);
        }

        public static ClientData? GetClient(this PlayerControl player)
        {
            if (AmongUsClient.Instance == null || player == null)
                return null;

            try
            {
                foreach (var client in AmongUsClient.Instance.allClients)
                {
                    if (client == null) continue;
                    if (client.Character == null) continue;

                    if (client.Character.PlayerId == player.PlayerId)
                        return client;
                }
            }
            catch (InvalidOperationException)
            {
                var snapshot = AmongUsClient.Instance.allClients.ToArray();
                foreach (var client in snapshot)
                {
                    if (client == null) continue;
                    if (client.Character == null) continue;

                    if (client.Character.PlayerId == player.PlayerId)
                        return client;
                }
            }

            return null;
        }

        public static int GetClientId(this PlayerControl player)
        {
            if (player == null)
                return -1;

            var client = player.GetClient();
            if (client == null)
                return -1;

            return client.Id;
        }
    }
}
