using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AMG.Utilities
{
    public static partial class Utils
    {
        // The rooms of the Skeld. Other SystemTypes (Sabotage, Ventilation...) are not rooms a body can be in.
        private static readonly SystemTypes[] SkeldRooms =
        [
            SystemTypes.Hallway, SystemTypes.Storage, SystemTypes.Cafeteria, SystemTypes.Reactor, SystemTypes.UpperEngine,
            SystemTypes.Nav, SystemTypes.Admin, SystemTypes.Electrical, SystemTypes.LifeSupp, SystemTypes.Shields,
            SystemTypes.MedBay, SystemTypes.Security, SystemTypes.Weapons, SystemTypes.LowerEngine, SystemTypes.Comms,
        ];

        // Names people use in chat that are not the game's name for the room. Lowercase, words separated by one space.
        private static readonly Dictionary<string, SystemTypes> RoomAliases = new()
        {
            ["o2"] = SystemTypes.LifeSupp,
            ["life support"] = SystemTypes.LifeSupp,
            ["upper"] = SystemTypes.UpperEngine,
            ["lower"] = SystemTypes.LowerEngine,
            ["med"] = SystemTypes.MedBay,
            ["navigation"] = SystemTypes.Nav,
        };

        // Every spelling that names a room: its game name ("MedBay", "med bay", "medbay") and the aliases above.
        private static readonly Dictionary<string, SystemTypes> RoomNames = BuildRoomNames();

        private static Dictionary<string, SystemTypes> BuildRoomNames()
        {
            var names = new Dictionary<string, SystemTypes>();
            foreach (var room in SkeldRooms)
            {
                string joined = room.ToString().ToLowerInvariant();
                string spaced = Regex.Replace(room.ToString(), "(?<=[a-z])(?=[A-Z])", " ").ToLowerInvariant();
                names[joined] = room;
                names[spaced] = room;
            }

            foreach (var alias in RoomAliases)
                names[alias.Key] = alias.Value;

            return names;
        }

        // Finds the room a text is talking about: "o2" -> LifeSupp, "upper engine" -> UpperEngine, "navigation" -> Nav.
        // When the text names more than one room, the first one in the text wins. false = the text names no room.
        public static bool TryGetRoomFromText(string text, out SystemTypes room)
        {
            room = default;
            if (string.IsNullOrWhiteSpace(text)) return false;

            // " word word " padded, so every name is matched as whole words
            string padded = " " + Regex.Replace(text.ToLowerInvariant(), "[^a-z0-9]+", " ").Trim() + " ";

            int bestIndex = int.MaxValue;
            string bestName = null;
            foreach (var name in RoomNames.Keys)
            {
                int index = padded.IndexOf(" " + name + " ", StringComparison.Ordinal);
                if (index < 0) continue;

                // same position: the longer name is the more specific one ("upper engine" over "upper")
                if (index < bestIndex || (index == bestIndex && name.Length > bestName.Length))
                {
                    bestIndex = index;
                    bestName = name;
                }
            }

            if (bestName == null) return false;

            room = RoomNames[bestName];
            return true;
        }
    }
}
