using AMG.AI.Debug;
using AMG.Utilities;
using AMG.Utilities.KeyDown;
using UnityEngine;

namespace AMG.KeyDownActions
{
    [KeyPressManager.Register(KeyCode.F3)]
    public static class ToggleVisionEspKey
    {
        public static void Execute()
        {
            AgentVisionESP.IsActive = !AgentVisionESP.IsActive;
            LogManager.LogDebug($"[AI GPS] Vision ESP: {(AgentVisionESP.IsActive ? "LIGADO" : "DESLIGADO")}");
        }
    }
 
    [KeyPressManager.Register(KeyCode.F4)]
    public static class DumpUnityLayersKey
    {
        public static void Execute()
        {
            AgentVisionESP.DumpUnityLayersToFile();
        }
    }
}