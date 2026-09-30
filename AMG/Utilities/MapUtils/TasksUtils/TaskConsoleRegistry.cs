using System.Collections.Generic;
using System.Linq;
using AMG.AI.Navigation;
using UnityEngine;

namespace AMG.Utilities.MapUtils.TasksUtils
{
    public enum TaskOrderKind
    {
        SingleStep,
        Ordered
    }

    public class TaskConsoleInfo(uint consoleId, TaskTypes taskType, SystemTypes room, Vector2 position)
    {
        public uint ConsoleId { get; } = consoleId;
        public TaskTypes TaskType { get; } = taskType;
        public SystemTypes Room { get; } = room;
        public Vector2 Position { get; } = position;
    }

    public static class TaskConsoleRegistry
    {
        private static readonly Dictionary<TaskTypes, List<TaskConsoleInfo>> _consolesByType = [];
        private static readonly List<TaskConsoleInfo> _allConsoles = [];
        private static readonly Dictionary<Waypoint, List<TaskConsoleInfo>> _consolesByWaypoint = [];
        private const float AgentRadiusMargin = 0.35f;
        private static readonly HashSet<TaskTypes> _commonTaskTypes = [];

        private static readonly HashSet<TaskTypes> _singleStepExceptions =
        [
            TaskTypes.ClearAsteroids,
            TaskTypes.StartReactor,
            TaskTypes.CleanO2Filter
        ];

        public static bool IsInitialized { get; private set; } = false;

        public static void Initialize()
        {
            _consolesByType.Clear();
            _allConsoles.Clear();
            _consolesByWaypoint.Clear(); // faltava limpar também
            _commonTaskTypes.Clear();
            IsInitialized = false;

            var allConsoles = UnityEngine.Object.FindObjectsOfType<Console>();
            if (allConsoles == null || allConsoles.Length == 0)
            {
                LogManager.LogDebug("[TaskConsoleRegistry] Nenhum Console encontrado em cena, abortando inicialização");
                return;
            }

            if (ShipStatus.Instance?.CommonTasks != null)
                foreach (var template in ShipStatus.Instance.CommonTasks)
                    if (template != null) _commonTaskTypes.Add(template.TaskType);

            var byType = new Dictionary<TaskTypes, List<Console>>();
            foreach (var console in allConsoles)
            {
                if (console?.TaskTypes == null) continue;
                foreach (var taskType in console.TaskTypes)
                {
                    if (!byType.TryGetValue(taskType, out var list))
                        byType[taskType] = list = [];
                    list.Add(console);
                }
            }

            int consolesWithoutWaypoint = 0;

            foreach (var (taskType, consoles) in byType)
            {
                var infos = new List<TaskConsoleInfo>();
                foreach (var console in consoles)
                {
                    Vector2 pos = console.transform.position;
                    var info = new TaskConsoleInfo((uint)console.ConsoleId, taskType, console.Room, pos);
                    infos.Add(info);
                    _allConsoles.Add(info);

                    var waypoint = pos.GetClosestNode();
                    if (waypoint == null)
                    {
                        consolesWithoutWaypoint++;
                        continue;
                    }

                    if (!_consolesByWaypoint.TryGetValue(waypoint, out var atWaypoint))
                        _consolesByWaypoint[waypoint] = atWaypoint = [];
                    atWaypoint.Add(info);
                }

                _consolesByType[taskType] = infos;
                LogManager.LogDebug($"[TaskConsoleRegistry] {taskType}: {infos.Count} consoles cacheados " +
                    $"(ids: {string.Join(",", infos.Select(i => i.ConsoleId))})");
            }

            if (consolesWithoutWaypoint > 0)
                LogManager.LogDebug($"[TaskConsoleRegistry] {consolesWithoutWaypoint} consoles sem waypoint próximo encontrado (verificar malha de navegação)");

            IsInitialized = true;
        }

        public static bool IsCommonTaskType(TaskTypes type) => _commonTaskTypes.Contains(type);

        public static TaskOrderKind GetOrderKind(TaskTypes type)
        {
            if (_singleStepExceptions.Contains(type)) return TaskOrderKind.SingleStep;
            if (!_consolesByType.TryGetValue(type, out var consoles) || consoles.Count <= 1) return TaskOrderKind.SingleStep;
            return TaskOrderKind.Ordered;
        }

        public static List<TaskConsoleInfo> GetConsoles(TaskTypes type)
            => _consolesByType.TryGetValue(type, out var list) ? list : [];

        public static bool IsNearAnyTask(Vector2 position)
        {
            var waypoint = position.GetClosestNode();
            if (waypoint == null) return false;

            foreach (var candidate in GetCandidateWaypoints(waypoint))
                if (_consolesByWaypoint.ContainsKey(candidate))
                    return true;

            return false;
        }


        private static IEnumerable<Waypoint> GetCandidateWaypoints(Waypoint origin)
        {
            yield return origin;
            foreach (var neighbor in origin.Neighbors)
                yield return neighbor;
        }

        public static (TaskConsoleInfo console, TaskTypes taskType) GetNearestConsoleAnyType(Vector2 position)
        {
            var waypoint = position.GetClosestNode();
            if (waypoint == null) return (null, default);

            TaskConsoleInfo best = null;
            float bestDist = float.MaxValue;

            foreach (var candidate in GetCandidateWaypoints(waypoint))
            {
                if (!_consolesByWaypoint.TryGetValue(candidate, out var infos)) continue;
                foreach (var info in infos)
                {
                    float dist = Vector2.Distance(position, info.Position);
                    if (dist < bestDist) { bestDist = dist; best = info; }
                }
            }

            return best != null ? (best, best.TaskType) : (null, default);
        }

        public static TaskConsoleInfo GetNearestConsole(TaskTypes type, Vector2 position)
        {
            if (!_consolesByType.TryGetValue(type, out var consoles)) return null;

            TaskConsoleInfo best = null;
            float bestDist = float.MaxValue;

            foreach (var info in consoles)
            {
                float dist = Vector2.Distance(position, info.Position);
                if (dist < bestDist) { bestDist = dist; best = info; }
            }

            return best;
        }

        public static (TaskConsoleInfo console, TaskTypes taskType, float distance) DebugGetClosestConsoleIgnoringRadius(Vector2 position)
        {
            TaskConsoleInfo best = null;
            TaskTypes bestType = default;
            float bestDist = float.MaxValue;

            foreach (var (taskType, consoles) in _consolesByType)
            {
                foreach (var info in consoles)
                {
                    float dist = Vector2.Distance(position, info.Position);
                    if (dist < bestDist) { bestDist = dist; best = info; bestType = taskType; }
                }
            }

            return (best, bestType, best != null ? bestDist : -1f);
        }
    }
}