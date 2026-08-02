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

            public static PlayerColorsEnum? GetPlayerColor(PlayerControl player)
            {
                var raw = player.Data.ColorName;
                if (string.IsNullOrEmpty(raw)) return null;

                var cleaned = raw.Trim().Trim('(', ')').Trim().ToLowerInvariant();

                if (colorMap.TryGetValue(cleaned, out PlayerColorsEnum color))
                    return color;

                return null;
            }

            public static PlayerColorsEnum? GetPlayerColor(string colorName)
            {
                var cleaned = colorName.Trim().Trim('(', ')').Trim().ToLowerInvariant();
                
                if (colorMap.TryGetValue(cleaned, out PlayerColorsEnum color))
                    return color;

                return null;
            }
        }
    }
}