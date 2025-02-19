using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeryonMotionGUI.Classes;

namespace XeryonMotionGUI.Helpers;
public static class FrequencyRangeHelper
{
    /// <summary>
    /// Adjusts the Min/Max of the given Parameter (FREQ or FRQ2),
    /// depending on whether its Value is below or above 100,000.
    /// </summary>
    public static void UpdateFrequency(Parameter freqParam)
    {
        // If freq < 100,000 => Min=80k, Max=90k
        // else => Min=160k, Max=170k
        if (freqParam.Value < 100000)
        {
            freqParam.Min = 80000;
            freqParam.Max = 90000;
        }
        else
        {
            freqParam.Min = 160000;
            freqParam.Max = 170000;
        }
    }
}