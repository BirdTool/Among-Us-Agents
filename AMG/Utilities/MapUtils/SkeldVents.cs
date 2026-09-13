using System.Linq;

namespace AMG.Utilities.MapUtils
{

    public static class SkeldVents
    {
        public static Vent GetByName(string name)
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllVents == null)
                return null;

            return ShipStatus.Instance.AllVents.FirstOrDefault(v => v != null && v.name == name);
        }

        public static Vent AdminVent => GetByName("AdminVent");
        public static Vent CafeVent => GetByName("CafeVent");
        public static Vent BigYVent => GetByName("BigYVent");
        public static Vent ElecVent => GetByName("ElecVent");
        public static Vent MedVent => GetByName("MedVent");
        public static Vent SecurityVent => GetByName("SecurityVent");
        public static Vent LowerReactorVent => GetByName("ReactorVent");
        public static Vent UpperReactorVent => GetByName("UpperReactorVent");
        public static Vent LowerEngineVent => GetByName("REngineVent");
        public static Vent UpperEngineVent => GetByName("LEngineVent");
        public static Vent WeaponsVent => GetByName("WeaponsVent");
        public static Vent NavVentNorth => GetByName("NavVentNorth");
        public static Vent NavVentSouth => GetByName("NavVentSouth");
        public static Vent ShieldsVent => GetByName("ShieldsVent");

        public static Vent[] GetAll()
        {
            return ShipStatus.Instance?.AllVents ?? System.Array.Empty<Vent>();
        }

        public static Vent[] GetNearby(string ventName)
        {
            var vent = GetByName(ventName);
            if (vent == null || vent.NearbyVents == null)
                return [];

            return [.. vent.NearbyVents.Where(v => v != null && v.Id != vent.Id)];
        }

        public static bool Exists(string name) => GetByName(name) != null;
    }
}

/*
Name: AdminVent
Position: (2.54, -9.96, 6.92)
Id: 0
Vents nearby: BigYVent, CafeVent
Left vent: CafeVent
Right vent: BigYVent
Center vent: null

Name: CafeVent
Position: (4.26, -0.28, 6.80)
Id: 2
Vents nearby: BigYVent, AdminVent
Left vent: AdminVent
Right vent: BigYVent
Center vent: null

Name: NavVentSouth
Position: (16.01, -6.38, 6.92)
Id: 13
Vents nearby: ShieldsVent
Left vent: ShieldsVent
Right vent: null
Center vent: null

Name: NavVentNorth
Position: (16.01, -3.17, 6.92)
Id: 12
Vents nearby: WeaponsVent
Left vent: WeaponsVent
Right vent: null
Center vent: null

Name: WeaponsVent
Position: (8.82, 3.32, 6.92)
Id: 7
Vents nearby: NavVentNorth
Left vent: null
Right vent: NavVentNorth
Center vent: null

Name: ShieldsVent
Position: (9.52, -14.34, 6.80)
Id: 10
Vents nearby: NavVentSouth
Left vent: null
Right vent: NavVentSouth
Center vent: null

Name: BigYVent
Position: (9.38, -6.44, 6.80)
Id: 1
Vents nearby: CafeVent, AdminVent
Left vent: AdminVent
Right vent: CafeVent
Center vent: null

Name: ElecVent
Position: (-9.78, -8.03, 6.80)
Id: 3
Vents nearby: MedVent, SecurityVent
Left vent: SecurityVent
Right vent: MedVent
Center vent: null

Name: ReactorVent
Position: (-20.80, -6.95, 6.92)
Id: 8
Vents nearby: REngineVent
Left vent: null
Right vent: REngineVent
Center vent: null

Name: UpperReactorVent
Position: (-21.88, -3.05, 6.92)
Id: 11
Vents nearby: LEngineVent
Left vent: null
Right vent: LEngineVent
Center vent: null

Name: REngineVent
Position: (-15.25, -13.66, 6.68)
Id: 9
Vents nearby: ReactorVent
Left vent: ReactorVent
Right vent: null
Center vent: null

Name: LEngineVent
Position: (-15.29, 2.52, 6.80)
Id: 4
Vents nearby: UpperReactorVent
Left vent: UpperReactorVent
Right vent: null
Center vent: null

Name: SecurityVent
Position: (-12.53, -6.95, 6.68)
Id: 5
Vents nearby: ElecVent, MedVent
Left vent: MedVent
Right vent: ElecVent
Center vent: null

Name: MedVent
Position: (-10.61, -4.18, 6.80)
Id: 6
Vents nearby: SecurityVent, ElecVent
Left vent: ElecVent
Right vent: SecurityVent
Center vent: null
*/