using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Dispatching;
using Windows.Devices.Enumeration;
using System.Collections.ObjectModel;
using XeryonMotionGUI.Classes; // Assumes Controller, Axis, etc.
using XeryonMotionGUI.Helpers;
using System.Reflection.Emit; // Assumes ControllerType, ControllerIdentificationResult, etc.

namespace XeryonMotionGUI.Views
{
    public sealed partial class HardwarePage : Page
    {
        public ObservableCollection<Controller> FoundControllers => Controller.FoundControllers;
        private DeviceWatcher deviceWatcher;

        public HardwarePage()
        {
            this.InitializeComponent();
            DataContext = this;
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
            StartDeviceWatcher();
        }

        #region DeviceWatcher Setup

        private void StartDeviceWatcher()
        {
            string selector = "System.Devices.InterfaceClassGuid:=\"{A5DCBF10-6530-11D2-901F-00C04FB951ED}\""; // USB GUID

            deviceWatcher = DeviceInformation.CreateWatcher(selector);
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            deviceWatcher.Stopped += DeviceWatcher_Stopped;

            Debug.WriteLine("Starting DeviceWatcher...");
            deviceWatcher.Start();
        }

        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            if (args.Name.Contains("COM") || args.Id.Contains("COM"))
            {
                Debug.WriteLine($"Device with COM Port Detected: {args.Name} - ID: {args.Id}");
                if (deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted)
                {
                    DispatcherQueue.TryEnqueue(() => CheckForControllers());
                }
            }
        }

        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Debug.WriteLine($"Device removed: {args.Id}");
            DispatcherQueue.TryEnqueue(() => CheckForControllers());
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Debug.WriteLine($"Device updated: {args.Id}");
            DispatcherQueue.TryEnqueue(() => CheckForControllers());
        }

        private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            Debug.WriteLine("Device enumeration completed. Calling CheckForControllers...");
            DispatcherQueue.TryEnqueue(() => CheckForControllers());
        }

        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            Debug.WriteLine("DeviceWatcher stopped.");
        }

        void CheckForControllersButton_Click(object sender, RoutedEventArgs e)
        {
            _ = CheckForControllers();
        }

        #endregion

        #region Main Function and Local Identification Function

        private async Task CheckForControllers()
        {
            await Task.Delay(10);
            RefreshProgressBar.Visibility = Visibility.Visible;
            string[] ports = SerialPort.GetPortNames();

            // Remove non-running controllers from the FoundControllers collection.
            for (int i = Controller.FoundControllers.Count - 1; i >= 0; i--)
            {
                if (!Controller.FoundControllers[i].Running)
                {
                    Controller.FoundControllers.RemoveAt(i);
                }
            }

            foreach (var portName in ports)
            {
                // Configure and open the serial port.
                using (var port = new SerialPort(portName)
                {
                    BaudRate = 115200,
                    ReadTimeout = 500  // Adjust as needed.
                })
                {
                    try
                    {
                        port.Open();

                        // Call the separate identification function.
                        ControllerIdentificationResult idResult = GetControllerIdentificationResult(port);
                        if (idResult.Type == ControllerType.Unknown)
                        {
                            Debug.WriteLine($"{portName} did not respond with a valid SRNO. Skipping.");
                            continue;
                        }
                        Debug.WriteLine($"{portName} identified as {idResult.FriendlyName} with {idResult.AxisCount} axis.");

                        // If no label was returned, assign default letters.
                        // For single-axis controllers, leave the label empty.
                        // For multi-axis controllers, assign letters "A", "B", "C", etc.
                        if (string.IsNullOrEmpty(idResult.Label))
                        {
                            if (idResult.AxisCount == 1)
                            {
                                idResult.Label = ""; // No prefix for single-axis.
                            }
                            else
                            {
                                idResult.Label = new string("ABCDEFGHIJKLMNOPQRSTUVWXYZ".Take(idResult.AxisCount).ToArray());
                            }
                        }

                        // Create the Controller instance.
                        var controller = new Controller("DefaultController", idResult.AxisCount, idResult.FriendlyName)
                        {
                            Port = port,
                            Type = idResult.FriendlyName,
                            Name = idResult.Name,
                            FriendlyPort = portName,
                            Serial = idResult.Serial,
                            Soft = idResult.Soft,
                            AxisCount = idResult.AxisCount,
                            Label = idResult.Label,
                            Status = "Connect"
                        };

                        // Create an Axis object for each axis using the corresponding character from controller.Label.
                        controller.Axes = new System.Collections.ObjectModel.ObservableCollection<Axis>();
                        for (int i = 0; i < idResult.AxisCount; i++)
                        {
                            // If the label is empty, use an empty prefix; otherwise, use the corresponding character.
                            string axisLetter = !string.IsNullOrEmpty(controller.Label) && controller.Label.Length > i
                                                  ? controller.Label[i].ToString()
                                                  : "";
                            var axis = new Axis(controller, "Placeholder", axisLetter);
                            controller.Axes.Add(axis);
                        }

                        // Send commands to refresh info and then read the full response.
                        port.Write("INFO=1");
                        port.Write("POLI=25");
                        await Task.Delay(100);
                        var response = port.ReadExisting();
                        response = string.Join("\n", response.Split('\n').TakeLast(120));
                        Debug.WriteLine("Response from controller: " + response);
                        port.Write("INFO=0");
                        await Task.Delay(100);
                        port.DiscardInBuffer();

                        // For each axis, use the AxisIdentifier helper to extract parameters.
                        foreach (var axis in controller.Axes)
                        {
                            var axisResult = AxisIdentifier.IdentifyAxis(port, axis.AxisLetter, response);
                            axis.Name = axisResult.Name;
                            axis.Resolution = axisResult.Resolution;
                            axis.Type = axisResult.AxisType;
                            axis.Range = axisResult.Range;
                            axis.Linear = axisResult.Linear;
                            axis.FriendlyName = axisResult.FriendlyName;
                            axis.StepSize = axisResult.StepSize;
                        }

                        port.Write("INFO=7");
                        port.Close();

                        Controller.FoundControllers.Add(controller);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing port {portName}: {ex.Message}");
                    }
                    // The using block ensures the port is closed if not needed further.
                }
            }
            RefreshProgressBar.Visibility = Visibility.Collapsed;
        }


        /// <summary>
        /// This function identifies the controller type, axis count, and sets some friendly names.
        /// </summary>
        /// <param name="port">An opened SerialPort.</param>
        /// <returns>A ControllerIdentificationResult containing identification details.</returns>
        private ControllerIdentificationResult GetControllerIdentificationResult(SerialPort port)
        {
            var result = new ControllerIdentificationResult();

            try
            {
                port.DiscardInBuffer();
                port.Write("INFO=1");
                port.Write("POLI=25");
                System.Threading.Thread.Sleep(100);

                // Read the available response.
                string fullResponse = port.ReadExisting();
                // Optionally, take the last several lines.
                string[] responseLines = fullResponse.Split('\n');
                string infoResponse = string.Join("\n", responseLines.TakeLast(12));

                // Read a line that should contain SRNO.
                string srnoResponse = port.ReadLine();
                if (!srnoResponse.Contains("SRNO"))
                {
                    Debug.WriteLine("No SRNO response found.");
                    result.Type = ControllerType.Unknown;
                    return result;
                }
                result.SRNOResponse = srnoResponse;

                // Attempt to get the AXES response.
                string axesResponse = "";
                try
                {
                    port.DiscardInBuffer();
                    port.WriteLine("AXES=?");
                    axesResponse = port.ReadLine();
                }
                catch (TimeoutException)
                {
                    Debug.WriteLine("AXES command timed out; assuming XD_OEM.");
                }

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
                    if (rawLabel.Length == 1)
                    {
                        result.Label = "";
                    }
                    else
                    {
                        result.Label = rawLabel;
                    }
                    Debug.WriteLine($"Label: {result.Label}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error reading label: " + ex.Message);
                    result.Label = "";
                }

                if (string.IsNullOrEmpty(axesResponse) || !axesResponse.Contains("AXES"))
                {
                    // No AXES response; assume OEM controller.
                    result.Type = ControllerType.XD_OEM;
                    result.AxisCount = 1;
                    result.Name = "XD-OEM Single Axis Controller";
                    result.FriendlyName = "XD-OEM";
                }
                else
                {
                    // Parse the axis count.
                    var match = Regex.Match(axesResponse, @"AXES[:=](\d+)");
                    if (match.Success)
                    {
                        result.AxisCount = int.Parse(match.Groups[1].Value);
                    }
                    else
                    {
                        result.AxisCount = 1;
                    }

                    // Determine type based on axis count.
                    if (result.AxisCount == 1)
                    {
                        result.Type = ControllerType.XD_C;
                        result.Name = "XD-C Single Axis Controller";
                        result.FriendlyName = "XD-C";
                    }
                    else if (result.AxisCount <= 6)
                    {
                        result.Type = ControllerType.XD_M;
                        result.Name = "XD-M Multi Axis Controller";
                        result.FriendlyName = "XD-M";
                    }
                    else
                    {
                        result.Type = ControllerType.XD_19;
                        result.Name = "XD-19 Multi Axis Controller";
                        result.FriendlyName = "XD-19";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error identifying controller: {ex.Message}");
                result.Type = ControllerType.Unknown;
            }

            return result;
        }

        /// <summary>
        /// Queries LLIM and HLIM from the device and computes the axis range.
        /// </summary>
        /// <param name="port">An opened SerialPort.</param>
        /// <param name="axis">The axis to update.</param>
        private void QueryAxisParameters(SerialPort port, Axis axis)
        {
            // Query LLIM.
            port.Write("LLIM=?");
            string llimResponse = port.ReadLine().Replace("LLIM=", "").Trim();
            Debug.WriteLine("LLIM: " + llimResponse);

            // Query HLIM.
            port.Write("HLIM=?");
            string hlimResponse = port.ReadLine().Replace("HLIM=", "").Trim();
            Debug.WriteLine("HLIM: " + hlimResponse);

            // Convert responses to double.
            double llim = Convert.ToDouble(llimResponse);
            double hlim = Convert.ToDouble(hlimResponse);

            // Ensure resolution is nonzero.
            if (axis.Resolution == 0)
            {
                axis.Resolution = 1000; // Default value; adjust as needed.
                Debug.WriteLine("Axis resolution was 0; defaulting to 1000.");
            }

            // Calculate the axis range.
            axis.Range = Math.Round((hlim - llim) * axis.Resolution / 1000000.0, 2);
            Debug.WriteLine("Axis Range set to: " + axis.Range);
        }

        #endregion

        #region UI Animations (Unchanged)

        private void AnimateItemsIn()
        {
            foreach (var item in AvailableControllersList.Items)
            {
                var container = AvailableControllersList.ContainerFromItem(item) as UIElement;
                if (container != null)
                {
                    Storyboard storyboard = Resources["StaggeredFadeInAnimation"] as Storyboard;
                    if (storyboard != null)
                    {
                        storyboard.BeginTime = TimeSpan.FromMilliseconds(100 * AvailableControllersList.Items.IndexOf(item));
                        Storyboard.SetTarget(storyboard, container);
                        storyboard.Begin();
                    }
                }
            }
        }

        private void Border_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var border = sender as Border;
            var hoverInStoryboard = this.Resources["HoverInStoryboard"] as Storyboard;
            if (hoverInStoryboard != null && border != null)
            {
                hoverInStoryboard.Stop();
                Storyboard.SetTarget(hoverInStoryboard, border);
                hoverInStoryboard.Begin();
            }
        }

        private void Border_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var border = sender as Border;
            var hoverOutStoryboard = this.Resources["HoverOutStoryboard"] as Storyboard;
            if (hoverOutStoryboard != null && border != null)
            {
                hoverOutStoryboard.Stop();
                Storyboard.SetTarget(hoverOutStoryboard, border);
                hoverOutStoryboard.Begin();
            }
        }

        #endregion

        private (bool isXeryon, string response) CheckIfXeryon(string portName)
        {
            SerialPort serialPort = new SerialPort(portName);
            string response = string.Empty;
            try
            {
                Debug.WriteLine("Checking for: " + portName);
                serialPort.BaudRate = 115200;
                serialPort.ReadTimeout = 2000;
                serialPort.Open();
                serialPort.Write("INFO=1");
                serialPort.Write("POLI=25");
                System.Threading.Thread.Sleep(100);
                response = serialPort.ReadExisting();
                response = string.Join("\n", response.Split('\n').TakeLast(6));
                Debug.WriteLine("Response from controller: " + response);
                bool isXeryon = response.Contains("SRNO");
                Debug.WriteLine(portName + (isXeryon ? " Is Xeryon" : " Is NOT Xeryon"));
                return (isXeryon, response);
            }
            catch (Exception)
            {
                Debug.WriteLine(portName + " Is NOT Xeryon");
                return (false, response);
            }
            finally
            {
                if (serialPort.IsOpen)
                    serialPort.Close();
            }
        }
    }
}
