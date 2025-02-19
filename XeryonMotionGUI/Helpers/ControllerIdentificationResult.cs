using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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

    // Maps the enum values to display strings with hyphens.
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

    // Contains the identification details for a controller.
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

    public static class ControllerIdentifier
    {
        /// <summary>
        /// Identifies the controller type, axis count, and sets friendly names.
        /// It first sends a reset (RSET), waits 50ms, clears the input buffer, then continues.
        /// Timing information is logged to Debug.
        /// </summary>
        /// <param name="port">An opened SerialPort to the controller.</param>
        /// <returns>A ControllerIdentificationResult containing identification details.</returns>
        public static ControllerIdentificationResult GetControllerIdentificationResult(SerialPort port)
        {
            var result = new ControllerIdentificationResult();
            var sw = new Stopwatch();

            try
            {
                // --- Initial INFO/POLI and full response read ---
                port.DiscardInBuffer();
                sw.Restart();
                port.Write("INFO=1");
                port.Write("POLI=25");
                System.Threading.Thread.Sleep(100);
                string fullResponse = port.ReadExisting();
                sw.Stop();
                Debug.WriteLine($"[ControllerIdentifier] INFO=1/POLI=25 and full response read took: {sw.ElapsedMilliseconds} ms");

                // (Optional processing of fullResponse)
                string[] responseLines = fullResponse.Split('\n');
                string infoResponse = string.Join("\n", responseLines.TakeLast(12));

                // --- Read the SRNO line ---
                sw.Restart();
                string srnoResponse = port.ReadLine();
                sw.Stop();
                Debug.WriteLine($"[ControllerIdentifier] Reading SRNO line took: {sw.ElapsedMilliseconds} ms");
                if (!srnoResponse.Contains("SRNO"))
                {
                    Debug.WriteLine("No SRNO response found.");
                    result.Type = ControllerType.Unknown;
                    return result;
                }
                result.SRNOResponse = srnoResponse;

                // --- Send RSET and wait for first message ---
                sw.Restart();
                port.Write("RSET");
                port.ReadTimeout = 5000;
                System.Threading.Thread.Sleep(50); // Wait 50ms after RSET
                port.DiscardInBuffer();
                string firstMessage = "";
                try
                {
                    firstMessage = port.ReadLine();
                    Debug.WriteLine("[ControllerIdentifier] First message after RSET: " + firstMessage);
                }
                catch (TimeoutException)
                {
                    Debug.WriteLine("[ControllerIdentifier] Timeout waiting for first message after RSET.");
                }
                sw.Stop();
                Debug.WriteLine($"[ControllerIdentifier] RSET and first message took: {sw.ElapsedMilliseconds} ms");



                // --- Refresh state with INFO=0/POLI=25 ---
                port.Write("INFO=0");
                port.Write("POLI=25");
                System.Threading.Thread.Sleep(100);
                port.ReadTimeout = 200;

                // --- Read AXES response ---
                sw.Restart();
                string axesResponse = "";
                try
                {
                    port.DiscardInBuffer();
                    port.WriteLine("AXES=?");
                    axesResponse = port.ReadLine();
                    Debug.WriteLine("[ControllerIdentifier] AXES response: " + axesResponse);
                }
                catch (TimeoutException)
                {
                    Debug.WriteLine("[ControllerIdentifier] AXES command timed out; assuming XD_OEM.");
                }
                sw.Stop();
                Debug.WriteLine($"[ControllerIdentifier] AXES query took: {sw.ElapsedMilliseconds} ms");

                // --- Parse SRNO and SOFT from the SRNO response ---
                var serMatch = Regex.Match(srnoResponse, @"SRNO=(\d+)");
                var softMatch = Regex.Match(srnoResponse, @"SOFT=(\d+)");
                if (serMatch.Success)
                {
                    result.Serial = serMatch.Groups[1].Value;
                }
                if (softMatch.Success)
                {
                    result.Soft = softMatch.Groups[1].Value;
                }

                // --- Read the label ---
                sw.Restart();
                try
                {
                    port.DiscardInBuffer();
                    port.WriteLine("LABL=?");
                    string rawLabel = port.ReadLine().Replace("LABL=", "").Trim();
                    int colonPos = rawLabel.IndexOf(':');
                    if (colonPos >= 0)
                    {
                        rawLabel = rawLabel.Substring(colonPos + 1);
                    }
                    // If the label is only 1 character, treat it as empty.
                    result.Label = rawLabel.Length == 1 ? "" : rawLabel;
                    Debug.WriteLine($"[ControllerIdentifier] Label: {result.Label}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ControllerIdentifier] Error reading label: {ex.Message}");
                    result.Label = "";
                }
                sw.Stop();
                Debug.WriteLine($"[ControllerIdentifier] LABL query took: {sw.ElapsedMilliseconds} ms");

                // --- Determine controller type and axis count ---
                if (string.IsNullOrEmpty(axesResponse) || !axesResponse.Contains("AXES"))
                {
                    result.Type = ControllerType.XD_OEM;
                    result.AxisCount = 1;
                    result.Name = "XD-OEM";
                    result.FriendlyName = "XD-OEM Single Axis Controller";
                }
                else
                {
                    var match = Regex.Match(axesResponse, @"AXES[:=](\d+)");
                    result.AxisCount = match.Success ? int.Parse(match.Groups[1].Value) : 1;

                    if (result.AxisCount == 1)
                    {
                        result.Type = ControllerType.XD_C;
                        result.Name = "XD-C";
                        result.FriendlyName = "XD-C Single Axis Controller";
                    }
                    else if (result.AxisCount <= 6)
                    {
                        result.Type = ControllerType.XD_M;
                        result.Name = "XD-M";
                        result.FriendlyName = "XD-M Multi Axis Controller";
                    }
                    else
                    {
                        result.Type = ControllerType.XD_19;
                        result.Name = "XD-19";
                        result.FriendlyName = "XD-19 Multi Axis Controller";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ControllerIdentifier] Error identifying controller: {ex.Message}");
                result.Type = ControllerType.Unknown;
            }

            return result;
        }
    }
}
