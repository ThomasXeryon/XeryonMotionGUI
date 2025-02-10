﻿using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using XeryonMotionGUI.Classes;

namespace XeryonMotionGUI.Helpers
{
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
                    .FirstOrDefault(line => line.Trim().StartsWith(prefix + "LLIM"));
                if (llimLine == null)
                {
                    llimLine = llimData
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault(line => line.Trim().StartsWith("LLIM"));
                }
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
                    .FirstOrDefault(line => line.Trim().StartsWith(prefix + "HLIM"));
                if (hlimLine == null)
                {
                    hlimLine = hlimData
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault(line => line.Trim().StartsWith("HLIM"));
                }
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
                // Build a regex pattern using the prefix.
                string pattern = $@"{Regex.Escape(prefix)}SOFT=\d+\s+(?:{Regex.Escape(prefix)})?(.*?)\s+{Regex.Escape(prefix)}STAT=";
                Debug.WriteLine($"[IdentifyAxis] Device info regex pattern: {pattern}");
                sw.Restart();
                var devMatch = Regex.Match(response, pattern, RegexOptions.Singleline | RegexOptions.Multiline);
                sw.Stop();
                Debug.WriteLine($"[IdentifyAxis] Device info query took {sw.ElapsedMilliseconds} ms");
                if (devMatch.Success)
                {
                    string devInfo = devMatch.Groups[1].Value.Trim();
                    Debug.WriteLine($"{prefix}Device Info extracted: '{devInfo}'");
                    // Expecting something like "XLS1=313"
                    var parts = devInfo.Split('=');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int res))
                    {
                        result.AxisType = parts[0].Trim();
                        result.Resolution = AdjustResolution(result.AxisType, res);
                        Debug.WriteLine($"{prefix}Axis Type set to: {result.AxisType}, Resolution set to: {result.Resolution}");
                    }
                    else
                    {
                        result.AxisType = "XLS";
                        Debug.WriteLine($"{prefix}Axis Type defaulted to: {result.AxisType}");
                    }
                    // Check if the device info contains "XRT". If yes, assume rotary (Linear = false); otherwise, linear.
                    if (devInfo.Contains("XRT"))
                    {
                        result.Linear = false;
                        Debug.WriteLine($"{prefix}Axis set to Rotary (Linear = false).");
                    }
                    else
                    {
                        result.Linear = true;
                        Debug.WriteLine($"{prefix}Axis set to Linear (Linear = true).");
                    }
                }
                else
                {
                    Debug.WriteLine($"No device info match found for axis {axisLetter} in response.");
                    result.AxisType = "XLS";
                }

                // --- Calculate the Axis Range ---
                result.Range = Math.Round((result.HLIM - result.LLIM) * result.Resolution / 1000000.0, 2);
                Debug.WriteLine($"Calculated Axis Range: {result.Range}");

                // Set additional default parameters.
                result.Name = "DefaultAxis";
                result.FriendlyName = "Test Axis";
                result.StepSize = 1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error identifying axis: " + ex.Message);
            }

            return result;
        }
    }
}
