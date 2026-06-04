using System.Collections.Generic;
using AMG.Enums;

namespace AMG.Utilities
{
    public static partial class Utils
    {
        public static class Colors
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

            internal static PlayerColorsEnum? GetPlayerColor(PlayerControl player)
            {
                if (colorMap.TryGetValue(player.Data.ColorName.ToLower(), out PlayerColorsEnum color))
                    return color;

                return null;
            }

            internal static PlayerColorsEnum? GetPlayerColor(string colorName)
            {
                if (colorMap.TryGetValue(colorName.ToLower(), out PlayerColorsEnum color))
                    return color;

                return null;
            }
        }
    }
}