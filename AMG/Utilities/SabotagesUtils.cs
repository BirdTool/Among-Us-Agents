using System.Collections.Generic;
using System.Linq;
using Cpp2IL.Core.Extensions;
using UnityEngine;

namespace AMG.Utilities
{
    public static partial class Utils
    {
        public static class Sabotages
        {
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

            public static bool IsPlayerOccupyingLocation(Vector2 location, float threshold = 0.6f)
            {
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player == null || player.Data == null || player.Data.IsDead) continue;

                    // Skip our agents, since their state is already managed by the AI logic
                    if (player.GetComponent<AMG.AI.Mind.AgentBrain>() != null) continue;

                    if (Vector2.Distance(player.transform.position, location) <= threshold)
                    {
                        return true;
                    }
                }
                return false;
            }

            public static bool IsO2ConsoleCompleted(ShipStatus shipStatus, int consoleId)
            {
                if (shipStatus == null || !IsOxygenSabotaged(shipStatus)) return false;

                try
                {
                    var lifeSupp = shipStatus.Systems[SystemTypes.LifeSupp].Cast<LifeSuppSystemType>();
                    
                    var prop = typeof(LifeSuppSystemType).GetProperty("CompletedConsoles", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    var field = typeof(LifeSuppSystemType).GetField("CompletedConsoles", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    
                    object completedConsoles = null;
                    if (prop != null) completedConsoles = prop.GetValue(lifeSupp);
                    else if (field != null) completedConsoles = field.GetValue(lifeSupp);

                    if (completedConsoles != null)
                    {
                        var containsMethod = completedConsoles.GetType().GetMethod("Contains", new[] { typeof(int) }) ?? 
                                             completedConsoles.GetType().GetMethod("Contains");

                        if (containsMethod != null)
                        {
                            return (bool)containsMethod.Invoke(completedConsoles, new object[] { consoleId });
                        }
                    }
                }
                catch 
                {
                    // Ignore reflection errors and fallback to false
                }

                return false;
            }
        }
    }
}
