using System.Collections.Generic;
using AMG.InternalRpc;
using AMG.InternalRpc.Calls;
using AMG.Models.InternalRpcModels;
using AmongUs.GameOptions;
using Epic.OnlineServices.CustomInvites;
using HarmonyLib;

namespace AMG.Patches
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    public static class ReceiveRolesPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerControl __instance, byte callId, Hazel.MessageReader reader)
        {
            switch (callId)
            {
                case InternalRpcHelper.SendRolesRpcId:
                    SendRolesRpc.Call(reader);
                    return false;
            }

            return true;
        }
    }
}