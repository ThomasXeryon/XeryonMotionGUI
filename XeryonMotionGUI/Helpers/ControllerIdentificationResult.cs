using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Text.RegularExpressions;
using XeryonMotionGUI.Classes;

namespace XeryonMotionGUI.Helpers
{
    // Define the controller types using underscores in identifiers.
    public enum ControllerType
    {
        Unknown,
        XD_OEM,  // single-axis, responds to VOLT=?, no AXES response
        XD_C,    // single-axis, no VOLT?, no AXES response
        XD_M,    // up to 6 axis, responds to AXES=?
        XD_19    // more than 6 axis, responds to AXES=?
    }

    // A helper class to map the enum values to display strings with hyphens.
    public static class ControllerTypeDisplay
    {
        public static string GetDisplayName(ControllerType type)
        {
            switch (type)
            {
                case ControllerType.XD_OEM:
                    return "XD-OEM";
                case ControllerType.XD_C:
                    return "XD-C";
                case ControllerType.XD_M:
                    return "XD-M";
                case ControllerType.XD_19:
                    return "XD-19";
                default:
                    return "Unknown";
            }
        }
    }

    // A helper class to return identification results.
    public class ControllerIdentificationResult
    {
        public ControllerType Type { get; set; } = ControllerType.Unknown;
        public int AxisCount { get; set; } = 0;
        public string SRNOResponse { get; set; } = "";
        public string FriendlyName { get; set; } = "";

        public string Name { get; set; } = "";
        public string Soft { get; set; } = "";

        public string Serial { get; set; } = "";

        public string Label { get; set; } = "";

    }
}
