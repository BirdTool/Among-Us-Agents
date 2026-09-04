using System.Collections.Generic;
using AMG.Models.InternalRpcModels;
using AmongUs.GameOptions;

namespace AMG.InternalRpc.Calls
{
    public static class SendRolesRpc
    {
        public static List<PlayerRoles> _roles = [];
        
        public static void Call(Hazel.MessageReader reader)
        {
            var receivedRoles = new List<PlayerRoles>();
            byte count = reader.ReadByte();

            for (int i = 0; i < count; i++)
            {
                byte playerId = reader.ReadByte();
                RoleTypes role = (RoleTypes)reader.ReadByte();

                var playerControl = GameData.Instance.GetPlayerById(playerId)?.Object;
                if (playerControl != null)
                    receivedRoles.Add(new PlayerRoles(playerControl, role));
            }
            
            _roles = receivedRoles;
        }
    }
}