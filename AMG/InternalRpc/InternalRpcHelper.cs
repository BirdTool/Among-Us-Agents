using System;
using System.Collections.Generic;
using System.Linq;
using AMG.Models.InternalRpcModels;
using Hazel;

namespace AMG.InternalRpc
{
    public static class InternalRpcHelper
    {
        public const byte SendRolesRpcId = 85;

        public static void SendRolesToHost(List<PlayerRoles> roles)
        {
            var hostClient = AmongUsClient.Instance.GetHost();
            if (hostClient == null) return;

            Hazel.MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId,
                SendRolesRpcId,
                Hazel.SendOption.Reliable,
                hostClient.Id 
            );

            writer.Write((byte)roles.Count);

            foreach (var pr in roles)
            {
                writer.Write(pr.Player.PlayerId);
                writer.Write((byte)pr.Role);
            }

            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
}