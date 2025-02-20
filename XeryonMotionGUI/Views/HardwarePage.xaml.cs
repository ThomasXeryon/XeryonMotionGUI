using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
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
using XeryonMotionGUI.Contracts.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.AppNotifications;
using XeryonMotionGUI.Services;

namespace XeryonMotionGUI.Views
{
    public sealed partial class HardwarePage : Page
    {
        public ObservableCollection<Controller> FoundControllers => Controller.FoundControllers;
        private DeviceWatcher deviceWatcher;
        private readonly IAppNotificationService _notificationService;


        public HardwarePage()
        {
            this.InitializeComponent();
            DataContext = this;
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
            //StartDeviceWatcher();
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

            // 1) Gather current enumerated COM ports
            string[] ports = SerialPort.GetPortNames();

            // 2) Mark running controllers as not running if their port is missing
            foreach (var ctrl in Controller.FoundControllers)
            {
                if (ctrl.Running && !ports.Contains(ctrl.FriendlyPort))
                {
                    Debug.WriteLine($"Port {ctrl.FriendlyPort} no longer present => marking {ctrl.Name} as not running.");
                    ctrl.Running = false;
                    ctrl.Status = "Idle"; // or "Disconnected"
                }
            }

            // 3) Enumerate each port, try identifying
            foreach (var portName in ports)
            {
                using (var tempPort = new SerialPort(portName)
                {
                    BaudRate = 115200,
                    ReadTimeout = 200 // Adjust as needed
                })
                {
                    try
                    {
                        tempPort.Open();

                        // Attempt to identify the controller on this port
                        ControllerIdentificationResult idResult = ControllerIdentifier.GetControllerIdentificationResult(tempPort);
                        if (idResult.Type == ControllerType.Unknown)
                        {
                            Debug.WriteLine($"{portName} did not respond with a valid SRNO. Skipping.");
                            continue;
                        }

                        Debug.WriteLine($"{portName} identified as {idResult.FriendlyName} with {idResult.AxisCount} axis/axes.");

                        // If no label was returned, assign default letters
                        if (string.IsNullOrEmpty(idResult.Label))
                        {
                            if (idResult.AxisCount == 1)
                            {
                                idResult.Label = ""; // single axis => no prefix
                            }
                            else
                            {
                                // e.g. 'ABCD' for 4 axes
                                idResult.Label = new string("ABCDEFGHIJKLMNOPQRSTUVWXYZ"
                                                            .Take(idResult.AxisCount).ToArray());
                            }
                        }

                        // Check if there's an existing controller for this port
                        var existing = Controller.FoundControllers.FirstOrDefault(c => c.FriendlyPort == portName);
                        if (existing != null)
                        {
                            if (!existing.Running)
                            {
                                // If we have it but it's not running => do auto reconnect
                                Debug.WriteLine($"Auto reconnecting to {portName}...");
                                tempPort.Close(); // we won't need this tempPort now
                                existing.ReconnectController();
                                continue;
                            }
                            else
                            {
                                // If already running => skip
                                Debug.WriteLine($"Skipping duplicate controller on {portName} (already running).");
                                tempPort.Close();
                                continue;
                            }
                        }

                        // Otherwise, create a brand-new Controller
                        var controller = new Controller("DefaultController", idResult.AxisCount, idResult.FriendlyName)
                        {
                            Port = tempPort,
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

                        // Create an Axis for each axis
                        controller.Axes = new ObservableCollection<Axis>();
                        for (int i = 0; i < idResult.AxisCount; i++)
                        {
                            string axisLetter = (!string.IsNullOrEmpty(controller.Label) && controller.Label.Length > i)
                                                  ? controller.Label[i].ToString() : "";
                            var axis = new Axis(controller, "Placeholder", axisLetter);
                            controller.Axes.Add(axis);
                        }

                        // (Optional) read a bit more info
                        tempPort.Write("INFO=1");
                        tempPort.Write("POLI=25");
                        await Task.Delay(100);
                        var response = tempPort.ReadExisting();
                        response = string.Join("\n", response.Split('\n').TakeLast(120));
                        Debug.WriteLine("Response from controller on " + portName + ": " + response);
                        tempPort.Write("INFO=0");
                        await Task.Delay(100);
                        tempPort.DiscardInBuffer();

                        // For each axis, identify axis details
                        foreach (var axis in controller.Axes)
                        {
                            var axisResult = Helpers.AxisIdentifier.IdentifyAxis(tempPort, axis.AxisLetter, response);
                            axis.Name = axisResult.Name;
                            axis.Resolution = axisResult.Resolution;
                            axis.Type = axisResult.AxisType;
                            axis.Range = axisResult.Range;
                            axis.Linear = axisResult.Linear;
                            axis.FriendlyName = axisResult.FriendlyName;
                            axis.StepSize = axisResult.StepSize;
                        }

                        tempPort.Write("INFO=4");
                        tempPort.Close();

                        Controller.FoundControllers.Add(controller);
                        // Suppose you just discovered a new controller: 
                        //  - 'controller.FriendlyName' = "Xeryon Device"
                        //  - 'controller.FriendlyPort' = "COM3"

                        // 1) Build the XML string for the toast
                        // Suppose 'controller.FriendlyPort' = "COM3" and 'controller.FriendlyName' = "XeryonDevice"
                        string toastXml = $@"
<toast>
    <visual>
        <binding template='ToastGeneric'>
            <text>New Controller Found</text>
            <text>Discovered {controller.FriendlyName} on port {controller.FriendlyPort}</text>
        </binding>
    </visual>
    <actions>
        <!-- The button to connect this controller -->
        <action 
            activationType='foreground' 
            arguments='action=ConnectController&amp;port={controller.FriendlyPort}'
            content='Connect' />
    </actions>
</toast>
";

                        // Show the toast
                        App.GetService<IAppNotificationService>().Show(toastXml);

                    }
                    catch (FileNotFoundException ex)
                    {
                        Debug.WriteLine($"Error processing port {portName}: {ex.Message}");

                        var existing = Controller.FoundControllers
                            .FirstOrDefault(c => c.FriendlyPort == portName);
                        if (existing != null && existing.Running)
                        {
                            existing.Running = false;
                            existing.Status = "Idle";
                            Debug.WriteLine($"Marked controller on {portName} as Idle due to FileNotFoundException.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing port {portName}: {ex.Message}");
                    }
                }
            }

            // 4) Now remove controllers whose port is *still* not found (and not running)
            //    so they are cleared from FoundControllers entirely.
            var toRemove = Controller.FoundControllers
                .Where(ctrl => !ctrl.Running && !ports.Contains(ctrl.FriendlyPort))
                .ToList();

            foreach (var deadCtrl in toRemove)
            {
                Debug.WriteLine($"Removing controller {deadCtrl.Name} on {deadCtrl.FriendlyPort}, since port not present & not running.");
                Controller.FoundControllers.Remove(deadCtrl);
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




