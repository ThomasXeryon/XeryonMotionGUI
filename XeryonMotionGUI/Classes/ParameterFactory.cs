using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeryonMotionGUI.Classes;
public static class ParameterFactory
{
    public static ObservableCollection<Parameter> CreateParameters()
    {
        return new ObservableCollection<Parameter>
            {
                new(0, 1, 0.01, 0.01, "Zone 1 Size:"),
                new(0, 1, 0.01, 0.1, "Zone 2 Size:"),
                new (0, 185000, 1000, 85000, "Zone 1 Frequency:"),
                new (0, 185000, 1000, 83000, "Zone 2 Frequency:"),
                new (0, 200, 5, 90, "Zone 1 Proportional:"),
                new (0, 200, 5, 45, "Zone 2 Proportional:"),
                new (0, 200, 2, 4, "Position Tolerance:"),
                new (0, 400, 5, 200, "Speed:"),
                new (0, 64400, 1000, 32000, "Acceleration:"),
                new (0, 1500, 100, 0, "Mass:"),
                new (0, 1, 1, 1, "Amplitude Control:"),
                new (-200, 0, 1, -100, "Left Soft Limit:"),
                new (0, 200, 1, 100, "Right Soft Limit:"),
                new (0, 1, 1, 1, "Phase Correction:"),
                new (0, 1000, 1, 50, "Error Limit:")
            };
    }
}