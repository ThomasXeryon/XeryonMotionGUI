using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Windows.Devices.Enumeration;
using XeryonMotionGUI.Classes;
using XeryonMotionGUI.Helpers;
using Microsoft.UI.Xaml.Media.Animation;

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
            this.NavigationCacheMode = NavigationCacheMode.Required;
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

        private void CheckForControllersButton_Click(object sender, RoutedEventArgs e)
        {
            _ = CheckForControllers();
        }

        #endregion

        #region Main Function and Identification

        private async Task CheckForControllers()
        {
            RefreshProgressBar.Visibility = Visibility.Visible;
            await Task.Delay(10);
            string[] ports = SerialPort.GetPortNames();

            // Remove non-running controllers.
            for (int i = FoundControllers.Count - 1; i >= 0; i--)
            {
                if (!FoundControllers[i].Running)
                {
                    FoundControllers.RemoveAt(i);
                }
            }

            foreach (var portName in ports)
            {
                using (var port = new SerialPort(portName)
                {
                    BaudRate = 115200,
                    ReadTimeout = 500 // Adjust as needed.
                })
                {
                    try
                    {
                        port.Open();

                        // Call the helper method.
                        ControllerIdentificationResult idResult = ControllerIdentifier.GetControllerIdentificationResult(port);
                        if (idResult.Type == ControllerType.Unknown)
                        {
                            Debug.WriteLine($"{portName} did not respond with a valid SRNO. Skipping.");
                            continue;
                        }
                        Debug.WriteLine($"{portName} identified as {idResult.FriendlyName} with {idResult.AxisCount} axis.");

                        // If no label was returned, assign default letters.
                        if (string.IsNullOrEmpty(idResult.Label))
                        {
                            if (idResult.AxisCount == 1)
                            {
                                idResult.Label = ""; // For single-axis, no prefix.
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
                            FriendlyName = idResult.FriendlyName,
                            Label = idResult.Label,
                            Status = "Connect"
                        };

                        // Create an Axis for each axis.
                        controller.Axes = new ObservableCollection<Axis>();
                        for (int i = 0; i < idResult.AxisCount; i++)
                        {
                            string axisLetter = (!string.IsNullOrEmpty(controller.Label) && controller.Label.Length > i)
                                                  ? controller.Label[i].ToString() : "";
                            var axis = new Axis(controller, "Placeholder", axisLetter);
                            controller.Axes.Add(axis);
                        }

                        // Refresh additional info from the controller.
                        port.Write("INFO=1");
                        port.Write("POLI=25");
                        await Task.Delay(100);
                        var response = port.ReadExisting();
                        response = string.Join("\n", response.Split('\n').TakeLast(120));
                        Debug.WriteLine("Response from controller: " + response);
                        port.Write("INFO=0");
                        await Task.Delay(100);
                        port.DiscardInBuffer();

                        // For each axis, use your AxisIdentifier helper to extract parameters.
                        foreach (var axis in controller.Axes)
                        {
                            var axisResult = Helpers.AxisIdentifier.IdentifyAxis(port, axis.AxisLetter, response);
                            axis.Name = axisResult.Name;
                            axis.Resolution = axisResult.Resolution;
                            axis.Type = axisResult.AxisType;
                            axis.Range = axisResult.Range;
                            axis.Linear = axisResult.Linear;
                            axis.FriendlyName = axisResult.FriendlyName;
                            axis.StepSize = axisResult.StepSize;
                        }

                        port.Write("INFO=4");
                        port.Close();

                        FoundControllers.Add(controller);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing port {portName}: {ex.Message}");
                    }
                }
            }
            RefreshProgressBar.Visibility = Visibility.Collapsed;
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
    }
}
