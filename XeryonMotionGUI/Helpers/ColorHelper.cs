using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.Storage;



namespace XeryonMotionGUI.Helpers;
public static class ColorHelper
{
    public static bool TryParseColor(string colorString, out Color color)
    {
        if (!string.IsNullOrEmpty(colorString) && colorString.Length == 8 &&
            byte.TryParse(colorString.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out var a) &&
            byte.TryParse(colorString.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var r) &&
            byte.TryParse(colorString.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var g) &&
            byte.TryParse(colorString.Substring(6, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            color = Color.FromArgb(a, r, g, b);
            return true;
        }

        color = default;
        return false;
    }
}