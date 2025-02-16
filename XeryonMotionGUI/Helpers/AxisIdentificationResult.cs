using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text.RegularExpressions;
using XeryonMotionGUI.Classes;

namespace XeryonMotionGUI.Helpers
{
    public static class StageCountsTable
    {
        private static readonly Dictionary<string, double> countsPerRev = new Dictionary<string, double>()
    {
        // Rotary stages – keys match those used in StageDictionary.
        { "XRTU_25_109", 57600 },
        { "XRTU_25_49", 144000 },
        { "XRTU_25_19", 360000 },
        { "XRTU_25_3", 1843200 },
        { "XRTU_30_109", 57600 },
        { "XRTU_30_49", 144000 },
        { "XRTU_30_19", 360000 },
        { "XRTU_30_3", 1843200 },
        { "XRTU_40_109", 86400 },
        { "XRTU_40_49", 135000 },
        { "XRTU_40_19", 345600 },
        { "XRTU_40_3", 2764800 },
        { "XRTU_60_109", 64800 },
        { "XRTU_60_49", 129600 },
        { "XRTU_60_19", 324000 },
        { "XRTU_60_3", 2073600 },
        { "XRTA", 57600 },
        { "XRTU_40_73_OLD", 86400 },
        { "XRTU_30_109_OLD", 57600 },
        { "XRTU_40_3_OLD", 1800000 }
    };

        public static double GetCounts(string stageKey)
        {
            if (countsPerRev.TryGetValue(stageKey, out double value))
                return value;
            return 0; // or throw an exception if preferred
        }
    }



    /// <summary>
    /// Contains the identification details for an axis.
    /// </summary>
    public class AxisIdentificationResult
    {
        public double LLIM { get; set; } = 0;
        public double HLIM { get; set; } = 0;
        public int Resolution { get; set; } = 0;
        public double Range { get; set; } = 0;
        public string AxisType { get; set; } = "";
        public string Name { get; set; } = "";
        public bool Linear { get; set; } = true;
        public string FriendlyName { get; set; } = "";
        public double StepSize { get; set; } = 0;
    }

    /// <summary>
    /// Provides a method for identifying axis parameters from a device via a SerialPort.
    /// </summary>
    /// 
    public static class AxisIdentifier
    {
        /// <summary>
        /// Adjusts the reported resolution based on the axis type.
        /// For XLS series, if reported value is slightly off, we adjust it.
        /// For XRT series, we use a different mapping.
        /// For XLA and XRTA series, we leave the value unchanged.
        /// </summary>
        /// <param name="axisType">The axis type string (e.g., "XLS1", "XRT1", "XLA1", etc.).</param>
        /// <param name="reportedResolution">The resolution as reported by the controller.</param>
        /// <returns>The adjusted (real) resolution.</returns>
        private static int AdjustResolution(string axisType, int reportedResolution)
        {
            // Normalize the axis type.
            string type = axisType.ToUpperInvariant();
            Debug.WriteLine($"[AdjustResolution] AxisType: {axisType}, ReportedResolution: {reportedResolution}");

            // For XLA and XRTA series, just return the reported value.
            if (type.StartsWith("XLA") || type.StartsWith("XRTA"))
            {
                Debug.WriteLine($"[AdjustResolution] Returning reported resolution for {type}: {reportedResolution}");
                return reportedResolution;
            }

            // For XLS series (e.g. XLS1, XLS3):
            if (type.StartsWith("XLS"))
            {
                if (reportedResolution == 1251) return 1250;
                if (reportedResolution == 1250) return 1250;
                if (reportedResolution == 313) return 312;
                if (reportedResolution == 312) return 312;
                if (reportedResolution == 5) return 5;
                if (reportedResolution == 1) return 1;
                return reportedResolution;
            }

            // For XRT series:
            if (type.StartsWith("XRT"))
            {
                if (!type.StartsWith("XRT3"))
                {
                    if (reportedResolution == 110) return 109;
                    if (reportedResolution == 109) return 109;
                    if (reportedResolution == 73) return 109;
                    if (reportedResolution == 50) return 49;
                    if (reportedResolution == 49) return 49;
                    if (reportedResolution == 47) return 49;
                    if (reportedResolution == 20) return 19;
                    if (reportedResolution == 19) return 19;
                    if (reportedResolution == 18) return 18;
                    if (reportedResolution == 4) return 3;
                    if (reportedResolution == 3) return 3;
                    if (reportedResolution == 2) return 3;
                    return reportedResolution;
                }
                else // XRT3 series
                {
                    return reportedResolution;
                }
            }

            Debug.WriteLine($"[AdjustResolution] No mapping for {type}. Returning reported resolution: {reportedResolution}");
            return reportedResolution;
        }

        /// <summary>
        /// Identifies the axis parameters by querying LLIM and HLIM with the appropriate axis-letter prefix,
        /// then parses the provided full response for the device info corresponding to the given axis letter.
        /// Also writes debug info on how long each command took.
        /// </summary>
        /// <param name="port">An open SerialPort used for communicating with the device.</param>
        /// <param name="axisLetter">
        /// The axis letter (e.g., "X" or "Y"). Commands are prefixed with this letter (e.g., "X:LLIM=?").
        /// </param>
        /// <param name="response">
        /// The full response from the controller (for example, the last 120 lines) that contains blocks for each axis.
        /// </param>
        /// <returns>An AxisIdentificationResult containing the gathered parameters.</returns>
        public static AxisIdentificationResult IdentifyAxis(SerialPort port, string axisLetter, string response)
        {
            var result = new AxisIdentificationResult();
            // Build the command prefix.
            string prefix = string.IsNullOrEmpty(axisLetter) ? "" : axisLetter + ":";

            Debug.WriteLine($"[IdentifyAxis] Using prefix: '{prefix}' for axis letter: '{axisLetter}'");

            try
            {
                // --- Query LLIM ---
                port.DiscardInBuffer();
                port.ReadTimeout = 2000;
                Stopwatch sw = Stopwatch.StartNew();
                port.Write(prefix + "LLIM=?");
                string llimData = "";
                int maxWaitMs = 1000; // maximum wait time in ms
                while (sw.ElapsedMilliseconds < maxWaitMs)
                {
                    llimData = port.ReadExisting();
                    if (!string.IsNullOrWhiteSpace(llimData))
                    {
                        break;
                    }
                    System.Threading.Thread.Sleep(10);
                }
                sw.Stop();
                Debug.WriteLine($"[IdentifyAxis] {prefix}LLIM query took {sw.ElapsedMilliseconds} ms");
                Debug.WriteLine($"[IdentifyAxis] Raw LLIM data: '{llimData}'");

                var llimLine = llimData
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(line => line.Trim().StartsWith(prefix + "LLIM") || line.Trim().StartsWith("LLIM"));

                if (llimLine != null)
                {
                    string llimNumber = Regex.Match(llimLine, @"[-+]?\d+(\.\d+)?").Value;
                    result.LLIM = Convert.ToDouble(llimNumber);
                    Debug.WriteLine($"{prefix}LLIM: {llimNumber}");
                }
                else
                {
                    Debug.WriteLine($"{prefix}LLIM line not found.");
                }

                // --- Query HLIM ---
                port.DiscardInBuffer();
                sw.Restart();
                port.Write(prefix + "HLIM=?");
                string hlimData = "";
                while (sw.ElapsedMilliseconds < maxWaitMs)
                {
                    hlimData = port.ReadExisting();
                    if (!string.IsNullOrWhiteSpace(hlimData))
                    {
                        break;
                    }
                    System.Threading.Thread.Sleep(10);
                }
                sw.Stop();
                Debug.WriteLine($"[IdentifyAxis] {prefix}HLIM query took {sw.ElapsedMilliseconds} ms");
                Debug.WriteLine($"[IdentifyAxis] Raw HLIM data: '{hlimData}'");

                var hlimLine = hlimData
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(line => line.Trim().StartsWith(prefix + "HLIM") || line.Trim().StartsWith("HLIM"));

                if (hlimLine != null)
                {
                    string hlimNumber = Regex.Match(hlimLine, @"[-+]?\d+(\.\d+)?").Value;
                    result.HLIM = Convert.ToDouble(hlimNumber);
                    Debug.WriteLine($"{prefix}HLIM: {hlimNumber}");
                }
                else
                {
                    Debug.WriteLine($"{prefix}HLIM line not found.");
                }

                // --- Parse Device Info from the Full Response ---
                // Example pattern to find "X:SOFT=123  X:STAT=" or so.
                string pattern = $@"{Regex.Escape(prefix)}SOFT=\d+\s+(?:{Regex.Escape(prefix)})?(.*?)\s+{Regex.Escape(prefix)}STAT=";
                Debug.WriteLine($"[IdentifyAxis] Device info regex pattern: {pattern}");
                sw.Restart();
                var devMatch = Regex.Match(response, pattern, RegexOptions.Singleline | RegexOptions.Multiline);
                sw.Stop();
                Debug.WriteLine($"[IdentifyAxis] Device info query took {sw.ElapsedMilliseconds} ms");

                string devInfo = "";
                if (devMatch.Success)
                {
                    devInfo = devMatch.Groups[1].Value.Trim();
                    Debug.WriteLine($"{prefix}Device Info extracted: '{devInfo}'");

                    // Expecting something like "XLS1=313" or "XRTU_25_19=109" etc.
                    var parts = devInfo.Split('=');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int res))
                    {
                        // e.g. "XLS1" or "XRTU_25_19"
                        string rawType = parts[0].Trim();
                        // Adjust the resolution if needed
                        int adjustedResolution = AdjustResolution(rawType, res);

                        Debug.WriteLine($"{prefix}Raw AxisType: {rawType}, Adjusted Resolution: {adjustedResolution}");

                        result.AxisType = rawType;              // We'll store the raw type here
                        result.Resolution = adjustedResolution; // final resolution
                    }
                    else
                    {
                        Debug.WriteLine($"{prefix}Could not parse device info properly, defaulting to XLS");
                        result.AxisType = "XLS";
                        result.Resolution = 312;  // some default
                    }
                }
                else
                {
                    // If no match, default to XLS as fallback
                    Debug.WriteLine($"No device info match found for axis {axisLetter} in response. Defaulting to XLS.");
                    result.AxisType = "XLS";
                    result.Resolution = 312;
                }

                // -------------- NEW LOGIC --------------
                // Decide if it's an XLA, XLS, or XRT stage based on the result.AxisType string
                string axisTypeUpper = result.AxisType.ToUpperInvariant();

                if (axisTypeUpper.Contains("XLA"))
                {
                    // XLA => linear axis
                    result.Linear = true;
                    result.Name = "XLA";                   // e.g. short label
                    result.FriendlyName = "XLA Linear actuator";    // more descriptive
                }
                else if (axisTypeUpper.Contains("XLS"))
                {
                    // XLS => linear axis
                    result.Linear = true;
                    result.Name = "XLS";
                    result.FriendlyName = "XLS Linear stage";
                }
                else if (axisTypeUpper.Contains("XRT"))
                {
                    // XRT => rotational axis
                    result.Linear = false;
                    result.Name = "XRT";
                    result.FriendlyName = "XRT-U Rotary stage";
                }
                else
                {
                    // Unknown => default assumption linear
                    result.Linear = true;
                    result.Name = axisTypeUpper;
                    result.FriendlyName = axisTypeUpper + " (Unknown)";
                }
                if (result.Linear)
                {
                    // E.g. if LLIM=0, HLIM=50 => range = 50 * res / 1e6 mm
                    result.Range = Math.Round((result.HLIM - result.LLIM) * result.Resolution / 1_000_000.0, 2);
                }
                else
                {
                    // For rotational, range might not be directly computed from LLIM/HLIM
                    // Some people set LLIM=0, HLIM=360 => 360 deg
                    // So we can store it as degrees or just skip
                    double rawDeg = result.HLIM - result.LLIM; // e.g. might be 360
                    result.Range = rawDeg; // or you can skip the mm conversion
                }
                Debug.WriteLine($"[IdentifyAxis] Computed Range = {result.Range}");

                // Additional defaults
                result.Name = result.Name.Trim();  // in case we have extra spaces
                result.FriendlyName = result.FriendlyName.Trim();
                result.StepSize = 1;  // user can override later
                Debug.WriteLine($"[IdentifyAxis] Final => AxisType={result.AxisType}, Name={result.Name}, FriendlyName={result.FriendlyName}, Linear={result.Linear}, Res={result.Resolution}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error identifying axis: " + ex.Message);
            }

            return result;
        }

    }
}
