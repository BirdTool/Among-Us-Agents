using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AMG.Utilities;
using AMG.Utilities.KeyDown;
using UnityEngine;

namespace AMG.KeyDownActions
{
    [KeyPressManager.Register(KeyCode.L)]
    public static class LogTaskMapKey
    {
        public static void Execute()
        {
            try
            {
                var logMap = new StringBuilder();
                logMap.AppendLine("================ MAPA INTEGRAL ================");

                var allConsoles = UnityEngine.Object.FindObjectsOfType<Console>();
                var taskMap = new Dictionary<TaskTypes, List<(int ConsoleId, SystemTypes Room, Vector3 Position)>>();

                foreach (var console in allConsoles)
                {
                    if (console?.TaskTypes == null) continue;

                    foreach (var type in console.TaskTypes)
                    {
                        if (!taskMap.ContainsKey(type))
                            taskMap[type] = [];

                        taskMap[type].Add((console.ConsoleId, console.Room, console.transform.position));
                    }
                }

                foreach (var kvp in taskMap)
                {
                    logMap.AppendLine($"\n>>> TAREFA: {kvp.Key} (Total de locais: {kvp.Value.Count})");

                    foreach (var (consoleId, room, position) in kvp.Value.OrderBy(x => x.ConsoleId))
                    {
                        logMap.AppendLine($"  - Console ID: {consoleId} | Sala: {room} | Pos: ({position.x:F2}, {position.y:F2})");
                    }
                }

                LogManager.Log(logMap.ToString());
            }
            catch (Exception ex)
            {
                LogManager.LogError($"[AI GPS] Erro ao logar tarefas: {ex}");
            }
        }
    }
}