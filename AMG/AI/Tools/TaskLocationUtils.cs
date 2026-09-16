using System.Collections.Generic;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Tools
{
    public static class TaskLocationUtils
    {
        public static List<Vector2> GetSafeTaskLocations(PlayerTask task)
        {
            var locs = new List<Vector2>();

            var normalTask = task.TryCast<NormalPlayerTask>();
            if (normalTask != null)
            {
                try
                {
                    var validPositions = normalTask.FindValidConsolesPositions();
                    if (validPositions != null)
                    {
                        foreach (var pos in validPositions) locs.Add(pos);
                        if (locs.Count > 0)
                        {
                            return locs;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    LogManager.LogDebug($"[TaskLocationUtils] FindValidConsolesPositions falhou: {ex.Message}");
                }
            }

            try
            {
                foreach (var loc in task.Locations) locs.Add(loc);
                if (locs.Count > 0) return locs;
            }
            catch (System.Exception) { }

            try
            {
                if (ShipStatus.Instance != null && ShipStatus.Instance.AllConsoles != null)
                {
                    foreach (var console in ShipStatus.Instance.AllConsoles)
                    {
                        bool hasTaskType = false;
                        foreach (var t in console.TaskTypes)
                        {
                            if (t == task.TaskType) { hasTaskType = true; break; }
                        }
                        if (hasTaskType) locs.Add(console.transform.position);
                    }
                }
            }
            catch (System.Exception) { }

            return locs;
        }
    }
}
