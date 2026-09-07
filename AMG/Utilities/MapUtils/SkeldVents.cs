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
        public static Vent LowerEngineVent => GetByName("LEngineVent");
        public static Vent UpperEngineVent => GetByName("REngineVent");
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