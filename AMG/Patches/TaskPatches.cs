using AMG.AI.Tools;
using AMG.Utilities;
using HarmonyLib;

namespace AMG.Patches
{
    [HarmonyPatch]
    public static class TaskPatches
    {
        [HarmonyPatch(typeof(HudManager), nameof(HudManager.CoTaskComplete))]
        [HarmonyPrefix]
        public static bool SuppressBanner(HudManager __instance)
        {
            if (TaskTracker.SuppressNextBanner)
            {
                TaskTracker.SuppressNextBanner = false;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(TaskPanelBehaviour), nameof(TaskPanelBehaviour.SetTaskText))]
        [HarmonyPrefix]
        public static bool FilterTaskPanel(TaskPanelBehaviour __instance, ref string str)
        {
            if (TaskTracker.PlayerOwnTaskIds.Count == 0) return true;

            try
            {
                var snapshot = PlayerControl.LocalPlayer.myTasks.ToArray();
                var lines = new System.Text.StringBuilder();
                foreach (var task in snapshot)
                {
                    if (!TaskTracker.IsAgentTask(task.Id))
                    {
                        string color = task.IsComplete ? "#00FF00" : "#FFFFFF";
                        lines.AppendLine($"<color={color}>{task.TaskType}</color>");
                    }
                }
                str = lines.ToString();
            }
            catch (System.Exception ex)
            {
                LogManager.LogDebug($"[TaskPatch] Erro no FilterTaskPanel: {ex.Message}");
                return true;
            }

            return true;
        }
    }
}
