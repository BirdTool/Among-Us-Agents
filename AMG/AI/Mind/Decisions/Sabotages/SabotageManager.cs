using AMG.Interfaces;
using AMG.Utilities;

namespace AMG.AI.Mind.Decisions.Sabotages
{
    public static class SabotageManager
    {
        public static ISabotage GetActiveManualSabotage()
        {
            var ship = ShipStatus.Instance;
            if (ship == null) return null;

            if (Utils.Sabotages.SabotagesCount(ship) >= 2)
            {
                // There's a hacker among us
                Utils.Sabotages.RepairSabotages(ship);
                return null;
            }

            var map = (MapNames)Utils.GetCurrentMapID();

            return map switch
            {
                MapNames.Skeld => SkeldSabotageHandler(ship),
                _ => null,
            };
        }

        private static ISabotage SkeldSabotageHandler(ShipStatus ship)
        {
            if (Utils.Sabotages.IsReactorSabotaged(ship)) return new SkeldReactorSabotage();
            if (Utils.Sabotages.IsOxygenSabotaged(ship)) return new SkeldO2Sabotage();
            if (Utils.Sabotages.IsElectricalSabotaged(ship)) return new SkeldLightsSabotage();
            if (Utils.Sabotages.IsCommsSabotaged(ship)) return new SkeldCommsSabotage();
            return null;
        }
    }
}