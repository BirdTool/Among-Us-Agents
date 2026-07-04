using System.Collections.Generic;
using UnityEngine;

namespace AMG.Utilities
{
    public static partial class Utils
    {
        public static class Sabotages
        {
            // This method has not been tested yet! It should work, I hope
            // This method has not been tested yet! It should work, I hope
            // This method has not been tested yet! It should work, I hope
            // This method has not been tested yet! It should work, I hope
            public static List<Vector3> GetActiveSabotageLocations(ShipStatus shipStatus)
            {
                List<Vector3> locations = [];
                List<TaskTypes> targetTasks = [];

                if (IsReactorSabotaged(shipStatus))
                {
                    if (IsPolusMap) targetTasks.Add(TaskTypes.ResetSeismic);
                    else targetTasks.Add(TaskTypes.ResetReactor);
                }

                if (IsOxygenSabotaged(shipStatus)) targetTasks.Add(TaskTypes.RestoreOxy);
                if (IsCommsSabotaged(shipStatus)) targetTasks.Add(TaskTypes.FixComms);
                if (IsElectricalSabotaged(shipStatus)) targetTasks.Add(TaskTypes.FixLights);

                if (targetTasks.Count == 0) return locations;

                var allConsoles = UnityEngine.Object.FindObjectsOfType<Console>();

                foreach (var console in allConsoles)
                {
                    if (console.ValidTasks == null) continue;

                    foreach (var validTask in console.ValidTasks)
                    {
                        if (targetTasks.Contains(validTask.taskType))
                        {
                            locations.Add(console.transform.position);
                            break;
                        }
                    }
                }

                return locations;
            }

            public static bool IsReactorSabotaged(ShipStatus shipStatus)
            {
                if (IsPolusMap)
                {
                    return shipStatus.Systems[SystemTypes.Laboratory].Cast<ReactorSystemType>().IsActive;
                }

                if (IsAirshipMap)
                {
                    return shipStatus.Systems[SystemTypes.HeliSabotage].Cast<HeliSabotageSystem>().IsActive;
                }

                return shipStatus.Systems[SystemTypes.Reactor].Cast<ReactorSystemType>().IsActive;
            }

            public static bool IsOxygenSabotaged(ShipStatus shipStatus)
            {
                if (IsSkeldMap || IsMiraHQMap || IsDleksMap)
                {
                    return shipStatus.Systems[SystemTypes.LifeSupp].Cast<LifeSuppSystemType>().IsActive;
                }
                return false;
            }

            public static bool IsCommsSabotaged(ShipStatus shipStatus)
            {
                if (IsMiraHQMap || IsFungleMap)
                {
                    return shipStatus.Systems[SystemTypes.Comms].Cast<HqHudSystemType>().IsActive;
                }

                return shipStatus.Systems[SystemTypes.Comms].Cast<HudOverrideSystemType>().IsActive;
            }

            public static bool IsElectricalSabotaged(ShipStatus shipStatus)
            {
                if (!IsFungleMap && !IsPolusMap)
                {
                    return shipStatus.Systems[SystemTypes.Electrical].Cast<SwitchSystem>().IsActive;
                }
                return false;
            }

            public static int SabotagesCount(ShipStatus shipStatus)
            {
                int count = 0;
                if (IsReactorSabotaged(shipStatus)) count++;
                if (IsOxygenSabotaged(shipStatus)) count++;
                if (IsCommsSabotaged(shipStatus)) count++;
                if (IsElectricalSabotaged(shipStatus)) count++;
                return count;
            }

            public static void RepairSabotages(ShipStatus shipStatus)
            {
                if (IsReactorSabotaged(shipStatus)) shipStatus.RpcUpdateSystem(SystemTypes.Reactor, (byte)16);
                if (IsOxygenSabotaged(shipStatus)) shipStatus.RpcUpdateSystem(SystemTypes.LifeSupp, (byte)16);
                if (IsCommsSabotaged(shipStatus)) shipStatus.RpcUpdateSystem(SystemTypes.Comms, (byte)16);
                if (IsElectricalSabotaged(shipStatus)) RepairLights(shipStatus);
            }

            public static void RepairLights(ShipStatus shipStatus)
            {
                if (IsPolusMap || IsFungleMap) return;

                var elecSys = shipStatus.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();

                for (var i = 0; i < 5; i++)
                {
                    var switchMask = 1 << (i & 0x1F);

                    if ((elecSys.ActualSwitches & switchMask) != (elecSys.ExpectedSwitches & switchMask))
                    {
                        shipStatus.RpcUpdateSystem(SystemTypes.Electrical, (byte)i);
                    }
                }
            }
        }
    }
}
