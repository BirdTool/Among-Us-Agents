using System.Collections.Generic;
using AMG.Enums;

namespace AMG.Utilities
{
    public static partial class Utils
    {
        public static class Colours
        {
            internal static Dictionary<string, PlayerColorsEnum> colorMap = new()
            {
                { "red", PlayerColorsEnum.Red },
                { "blue", PlayerColorsEnum.Blue },
                { "green", PlayerColorsEnum.Green },
                { "pink", PlayerColorsEnum.Pink },
                { "orange", PlayerColorsEnum.Orange },
                { "yellow", PlayerColorsEnum.Yellow },
                { "black", PlayerColorsEnum.Black },
                { "white", PlayerColorsEnum.White },
                { "purple", PlayerColorsEnum.Purple },
                { "brown", PlayerColorsEnum.Brown },
                { "cyan", PlayerColorsEnum.Cyan },
                { "lime", PlayerColorsEnum.Lime },
                { "gray", PlayerColorsEnum.Gray },
                { "maroon", PlayerColorsEnum.Maroon },
                { "banana", PlayerColorsEnum.Banana },
                { "tan", PlayerColorsEnum.Tan },
                { "rose", PlayerColorsEnum.Rose },
                { "coral", PlayerColorsEnum.Coral },
            };

            public static PlayerColorsEnum? GetPlayerColour(PlayerControl player)
            {
                var raw = player.Data.ColorName;
                if (string.IsNullOrEmpty(raw)) return null;

                var cleaned = CleanColour(raw);

                if (colorMap.TryGetValue(cleaned, out PlayerColorsEnum color))
                    return color;

                return null;
            }

            public static PlayerColorsEnum? GetPlayerColour(string colorName)
            {
                var cleaned = CleanColour(colorName);
                
                if (colorMap.TryGetValue(cleaned, out PlayerColorsEnum color))
                    return color;

                return null;
            }

            public static string ColourOf(PlayerControl player) => CleanColour(player.Data.ColorName);
            public static string ColourOf(string colourName) => CleanColour(colourName);

            private static string CleanColour(string colourName)
            {
                return colourName.Trim().Trim('(', ')').Trim().ToLowerInvariant();
            }
        }
    }
}