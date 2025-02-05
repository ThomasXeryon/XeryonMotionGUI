using System;
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
        /// Identifies the axis parameters by querying LLIM and HLIM with the appropriate axis-letter prefix,
        /// then parses the provided full response for the device info corresponding to the given axis letter.
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

            try
            {
                // --- Query LLIM ---
                port.DiscardInBuffer();
                port.Write(prefix + "LLIM=?");
                System.Threading.Thread.Sleep(50); // Give the device time to respond
                string llimData = port.ReadExisting();
                // Try to get the line that starts with the prefix.
                var llimLine = llimData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                       .FirstOrDefault(line => line.Trim().StartsWith(prefix + "LLIM"));
                // If not found, try without the prefix.
                if (llimLine == null)
                {
                    llimLine = llimData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                       .FirstOrDefault(line => line.Trim().StartsWith("LLIM"));
                }
                if (llimLine != null)
                {
                    string llimNumber = Regex.Match(llimLine, @"[-+]?\d+(\.\d+)?").Value;
                    result.LLIM = Convert.ToDouble(llimNumber);
                    Debug.WriteLine($"{(string.IsNullOrEmpty(prefix) ? "" : prefix)}LLIM: {llimNumber}");
                }
                else
                {
                    Debug.WriteLine($"{(string.IsNullOrEmpty(prefix) ? "" : prefix)}LLIM line not found.");
                }

                // --- Query HLIM ---
                port.DiscardInBuffer();
                port.Write(prefix + "HLIM=?");
                System.Threading.Thread.Sleep(50);
                string hlimData = port.ReadExisting();
                var hlimLine = hlimData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                       .FirstOrDefault(line => line.Trim().StartsWith(prefix + "HLIM"));
                // If not found, try without the prefix.
                if (hlimLine == null)
                {
                    hlimLine = hlimData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                       .FirstOrDefault(line => line.Trim().StartsWith("HLIM"));
                }
                if (hlimLine != null)
                {
                    string hlimNumber = Regex.Match(hlimLine, @"[-+]?\d+(\.\d+)?").Value;
                    result.HLIM = Convert.ToDouble(hlimNumber);
                    Debug.WriteLine($"{(string.IsNullOrEmpty(prefix) ? "" : prefix)}HLIM: {hlimNumber}");
                }
                else
                {
                    Debug.WriteLine($"{(string.IsNullOrEmpty(prefix) ? "" : prefix)}HLIM line not found.");
                }

                // If the device does not supply a resolution, assign a default.
                if (result.Resolution == 0)
                {
                    result.Resolution = 1000; // Default value; adjust if needed.
                    Debug.WriteLine("Axis resolution was not provided; defaulting to 1000.");
                }

                // --- Calculate the Axis Range ---
                result.Range = Math.Round((result.HLIM - result.LLIM) * result.Resolution / 1000000.0, 2);
                Debug.WriteLine("Calculated Axis Range: " + result.Range);

                // --- Parse Device Info from the Full Response ---
                // Build a regex pattern using the prefix.
                string pattern = $@"{Regex.Escape(prefix)}SOFT=\d+\s+(?:{Regex.Escape(prefix)})?(.*?)\s+{Regex.Escape(prefix)}STAT=";
                var devMatch = Regex.Match(response, pattern, RegexOptions.Singleline | RegexOptions.Multiline);
                if (devMatch.Success)
                {
                    string devInfo = devMatch.Groups[1].Value.Trim();
                    Debug.WriteLine($"{prefix}Device Info: {devInfo}");
                    // Expecting something like "XLS1=313"
                    var parts = devInfo.Split('=');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int res))
                    {
                        result.AxisType = parts[0].Trim();
                        result.Resolution = res; // Overwrite default if device provides resolution.
                        Debug.WriteLine($"{prefix}Axis Type set to: {result.AxisType}, Resolution set to: {result.Resolution}");
                    }
                    else
                    {
                        result.AxisType = "XLS";
                        Debug.WriteLine($"{prefix}Axis Type defaulted to: {result.AxisType}");
                    }
                }
                else
                {
                    Debug.WriteLine($"No device info match found for axis {axisLetter} in response.");
                    result.AxisType = "XLS";
                }

                // Set additional default parameters.
                result.Name = "DefaultAxis";
                result.FriendlyName = "Test Axis";
                result.Linear = true;
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
