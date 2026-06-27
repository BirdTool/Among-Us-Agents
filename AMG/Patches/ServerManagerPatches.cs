using HarmonyLib;

namespace AMG.Patches
{
    [HarmonyPatch]
    public static class ServerManagerPatches
    {
        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
        [HarmonyPostfix]
        public static void MainMenuManager_Start_Postfix()
        {
            var amgRegion = new DnsRegionInfo(
                "127.0.0.1",
                "Servidor AMG",
                (StringNames)0,
                "127.0.0.1",
                22023,
                false
            );

            IRegionInfo regionInterface = amgRegion.Cast<IRegionInfo>();

            ServerManager.Instance.AddOrUpdateRegion(regionInterface);

            ServerManager.Instance.SetRegion(regionInterface);
        }
    }
}