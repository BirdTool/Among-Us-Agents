using System.Linq;
using AMG.AI.Control;
using HarmonyLib;
using UnityEngine;

namespace AMG.Patches
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Awake))]
    public static class PlayerControl_Awake_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerControl __instance)
        {
            var real = AgentManager.RealLocalPlayer;
            if (real == null) return;

            if (PlayerControl.LocalPlayer != null
                && PlayerControl.LocalPlayer != real
                && AgentManager.Agents.Any(a => a.Control == PlayerControl.LocalPlayer))
            {
                PlayerControl.LocalPlayer = real;

                if (Camera.main != null)
                {
                    var cam = Camera.main.GetComponent<FollowerCamera>();
                    if (cam != null) cam.SetTarget(real);
                }
            }
        }
    }
}
