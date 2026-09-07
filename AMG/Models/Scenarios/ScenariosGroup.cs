using System.Collections.Generic;
using AMG.Interfaces;

namespace AMG.Models.Scenarios
{
    public static class ScenariosGroup
    {
        public static List<IScenario> All => [
            new InHallwayAdminKillScenario()
        ];
    }
}