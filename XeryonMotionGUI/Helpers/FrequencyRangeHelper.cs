using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        // Log the incoming value
        Debug.WriteLine($"[UpdateFrequency] Called with freqParam.Value = {freqParam.Value}");

        if (freqParam.Value < 100000)
        {
            freqParam.Min = 80000;
            freqParam.Max = 90000;
            Debug.WriteLine($"[UpdateFrequency] freqParam.Value < 100000 => setting Min=80000, Max=90000");
        }
        else
        {
            freqParam.Min = 160000;
            freqParam.Max = 175000;
            Debug.WriteLine($"[UpdateFrequency] freqParam.Value >= 100000 => setting Min=160000, Max=175000");
        }

        // Log the final assigned range
        Debug.WriteLine($"[UpdateFrequency] Final freqParam.Min={freqParam.Min}, freqParam.Max={freqParam.Max}");
    }
}