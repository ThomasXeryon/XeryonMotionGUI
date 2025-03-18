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
using Windows.Storage.Pickers;
using Windows.Storage;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

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
        #endregion

        private void CheckForControllersButton_Click(object sender, RoutedEventArgs e)
        {
            _ = CheckForControllers();
        }

        #region Main Function and Identification
        private async Task CheckForControllers()
        {
            RefreshProgressBar.Visibility = Visibility.Visible;
            await Task.Delay(10);

            // 1) Gather current enumerated COM ports
            string[] ports = SerialPort.GetPortNames();

            // 2) Remove controllers whose port is no longer present
            var toRemove = Controller.FoundControllers
                .Where(ctrl => !ports.Contains(ctrl.FriendlyPort))
                .ToList();

            foreach (var deadCtrl in toRemove)
            {
                Debug.WriteLine($"Removing controller {deadCtrl.Name} on {deadCtrl.FriendlyPort}, since port not present.");
                Controller.FoundControllers.Remove(deadCtrl);
            }

            // 3) Enumerate each port, try identifying
            foreach (var portName in ports)
            {
                await Task.Delay(1);
                using (var tempPort = new SerialPort(portName)
                {
                    BaudRate = 115200,
                    ReadTimeout = 200
                })
                {
                    try
                    {
                        tempPort.Open();
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
                                idResult.Label = "";
                            else
                                idResult.Label = new string("ABCDEFGHIJKLMNOPQRSTUVWXYZ"
                                                            .Take(idResult.AxisCount).ToArray());
                        }

                        // Check if there's an existing controller for this port
                        var existing = Controller.FoundControllers.FirstOrDefault(c => c.FriendlyPort == portName);
                        if (existing != null)
                        {
                            // Already known: skip adding a duplicate
                            Debug.WriteLine($"Skipping duplicate controller on {portName}. Controller is already known.");
                            tempPort.Close();
                            continue;
                        }

                        // Otherwise, create a new Controller
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

                        // Optional extra info
                        tempPort.Write("INFO=1");
                        tempPort.Write("POLI=25");
                        await Task.Delay(100);
                        var response = tempPort.ReadExisting();
                        response = string.Join("\n", response.Split('\n').TakeLast(120));
                        Debug.WriteLine("Response from controller on " + portName + ": " + response);
                        tempPort.Write("INFO=0");
                        await Task.Delay(100);
                        tempPort.DiscardInBuffer();

                        // Identify axis details
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
                            await Task.Delay(1);
                        }

                        tempPort.Write("INFO=4");
                        tempPort.Close();

                        // Add to FoundControllers
                        Controller.FoundControllers.Add(controller);

                        // Show a toast (optional)
                        string toastXml = $@"
<toast>
    <visual>
        <binding template='ToastGeneric'>
            <text>New Controller Found</text>
            <text>Discovered {controller.FriendlyName} on port {controller.FriendlyPort}</text>
        </binding>
    </visual>
    <actions>
        <action 
            activationType='foreground' 
            arguments='action=ConnectController&amp;port={controller.FriendlyPort}'
            content='Connect' />
    </actions>
</toast>";
                        App.GetService<IAppNotificationService>().Show(toastXml);
                    }
                    catch (FileNotFoundException ex)
                    {
                        Debug.WriteLine($"Error processing port {portName}: {ex.Message}");
                        // If anything fails here, we just skip. We don’t set to Idle or keep them around.
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
            // Example utility if you need to check specifically for Xeryon device
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

        #region Hover Animations
        private void Border_PointerEntered(object sender, PointerRoutedEventArgs e)
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

        private void Border_PointerExited(object sender, PointerRoutedEventArgs e)
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

        #region Right-Click + Popup for CAN Controllers

        // This method is called when the user right-clicks somewhere in the ListView.
        private void AvailableControllersList_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var listView = sender as ListView;
            if (listView != null && listView.ContextFlyout is MenuFlyout flyout)
            {
                // Show the flyout at the pointer position
                var options = new FlyoutShowOptions
                {
                    Position = e.GetPosition(listView),
                    ShowMode = FlyoutShowMode.Standard
                };
                flyout.ShowAt(listView, options);
            }
        }

        // Clicked "Add CAN Controller" in the context menu
        private void AddCANController_Click(object sender, RoutedEventArgs e)
        {
            // Populate the rod length options each time
            RodLengthComboBox.Items.Clear();
            for (int length = 45; length <= 185; length += 10)
            {
                RodLengthComboBox.Items.Add(new ComboBoxItem { Content = length.ToString() });
            }

            // Select defaults
            ResolutionComboBox.SelectedIndex = 0;
            RodLengthComboBox.SelectedIndex = 0;

            // Open the popup
            CANControllerPopup.IsOpen = true;
        }

        // Browse for .eds file
        private async void UploadEDSFile_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add(".eds");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                Debug.WriteLine($"EDS file selected: {file.Path}");

                // Optional toast
                string toastXml = $@"
<toast>
    <visual>
        <binding template='ToastGeneric'>
            <text>EDS File Uploaded</text>
            <text>File: {file.Name}</text>
        </binding>
    </visual>
</toast>";
                App.GetService<IAppNotificationService>()?.Show(toastXml);
            }
        }

        // Confirm: create or register a new CAN controller, or just store the user’s input
        private void AddCANControllerConfirm_Click(object sender, RoutedEventArgs e)
        {
            string resolutionStr = (ResolutionComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string rodLengthStr = (RodLengthComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (!string.IsNullOrEmpty(resolutionStr) && !string.IsNullOrEmpty(rodLengthStr))
            {
                // Create a new 'CAN' style controller as an example
                double resolution = double.Parse(resolutionStr);
                double rodLength = double.Parse(rodLengthStr);

                var controller = new Controller("CANController", 1, "CAN")
                {
                    Type = "CAN",
                    Name = "CAN_CTRL",
                    FriendlyPort = "CAN0",
                    Serial = $"CAN_{DateTime.Now.Ticks}",
                    Soft = "1.0",
                    AxisCount = 1,
                    FriendlyName = "CAN Controller",
                    Status = "Connect",
                    Label = "A"
                };

                // Single axis, just as a demonstration
                controller.Axes = new ObservableCollection<Axis>();
                var axis = new Axis(controller, "CAN_Axis", "A")
                {
                    Resolution = (int)resolution,
                    Range = rodLength,
                    Type = "Linear",
                    FriendlyName = "CAN Axis",
                    Linear = true
                };
                controller.Axes.Add(axis);

                // Add to FoundControllers
                Controller.FoundControllers.Add(controller);

                // Optional toast
                string toastXml = $@"
<toast>
    <visual>
        <binding template='ToastGeneric'>
            <text>CAN Controller Added</text>
            <text>Resolution: {resolution}, Rod Length: {rodLength}</text>
        </binding>
    </visual>
</toast>";
                App.GetService<IAppNotificationService>()?.Show(toastXml);
            }

            // Close the popup
            CANControllerPopup.IsOpen = false;
        }

        private void CancelCANController_Click(object sender, RoutedEventArgs e)
        {
            // Just close the popup without doing anything
            CANControllerPopup.IsOpen = false;
        }

        #endregion
    }
}
