using System.Collections.Generic;
using AMG.Utilities;
using UnityEngine;
using System.Linq;

namespace AMG.AI.Navigation
{
    public static class VentManager
    {
        private static Vent[] _allVents;
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;
            _allVents = UnityEngine.Object.FindObjectsOfType<Vent>();
            _initialized = true;
            LogManager.LogDebug($"[VentManager] Initialized with {_allVents.Length} vents.");
        }

        public static void Reset()
        {
            _allVents = null;
            _initialized = false;
        }

        public static Vent[] GetAllVents()
        {
            if (!_initialized) Initialize();
            return _allVents;
        }

        public static Vent GetVentById(int id)
        {
            var vents = GetAllVents();
            foreach (var vent in vents)
            {
                if (vent.Id == id) return vent;
            }
            return null;
        }

        public static List<Vent> GetConnectedVents(Vent vent)
        {
            var connected = new List<Vent>();
            if (vent == null) return connected;

            if (vent.Left != null) connected.Add(vent.Left);
            if (vent.Right != null) connected.Add(vent.Right);
            if (vent.Center != null) connected.Add(vent.Center);

            return connected;
        }

        public static Vent GetSafeConnectedVent(Vent startVent, PlayerControl agent)
        {
            var connectedVents = GetConnectedVents(startVent);
            if (connectedVents.Count == 0) return null;

            var safeVents = new List<Vent>();

            foreach (var vent in connectedVents)
            {
                // Check if any alive players are near the vent
                var nearbyPlayers = AgentPerception.GetNearbyPlayers(
                    vent.transform.position,
                    4.5f,
                    agent,
                    aliveOnly: true,
                    checkObstruction: true
                );

                if (nearbyPlayers.Count == 0)
                {
                    safeVents.Add(vent);
                }
            }

            if (safeVents.Count > 0)
            {
                return safeVents.GetRandomItemSecureOrDefault();
            }

            return null; // No completely safe vent
        }

        public static Vent GetNearestVent(Vector2 position)
        {
            var vents = GetAllVents();
            if (vents == null || vents.Length == 0) return null;

            Vent nearest = null;
            float minDistance = float.MaxValue;

            foreach (var vent in vents)
            {
                float dist = Vector2.Distance(position, vent.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = vent;
                }
            }

            return nearest;
        }
    }
}
