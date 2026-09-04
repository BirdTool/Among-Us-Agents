using System.Collections.Generic;
using AMG.InternalRpc.Calls;
using AMG.Utilities;
using HarmonyLib;

namespace AMG.Patches
{
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    public static class RoleManager_SelectRoles_Patch
    {
        public static bool Prefix(RoleManager __instance)
        {
            if (!Utils.IsRealHost()) return true;
            SendRolesRpc._roles.RemoveAll(pr => pr.Player == null);
            if (SendRolesRpc._roles.Count <= 0) return true;

            foreach (var pr in SendRolesRpc._roles)
            {
                pr.Player.RpcSetRole(pr.Role);
            }

            SendRolesRpc._roles.Clear();
            return true;
        }
    }
}