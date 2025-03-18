using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Windows.Devices.Enumeration;
using XeryonMotionGUI.Activation;
using XeryonMotionGUI.Classes;
using XeryonMotionGUI.Contracts.Services;
using XeryonMotionGUI.Core.Contracts.Services;
using XeryonMotionGUI.Core.Services;
using XeryonMotionGUI.Helpers;
using XeryonMotionGUI.Models;
using XeryonMotionGUI.Notifications;
using XeryonMotionGUI.Services;
using XeryonMotionGUI.ViewModels;
using XeryonMotionGUI.Views;

namespace XeryonMotionGUI;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    private DeviceWatcher deviceWatcher;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetPriorityClass(IntPtr hProcess, uint dwPriorityClass);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetCurrentProcess();

    // Real-time priority class
    private const uint REALTIME_PRIORITY_CLASS = 0x100;

    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host
    {
        get;
    }

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();
    private AppNotificationManager notificationManager;

    public static UIElement? AppTitlebar { get; set; }

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddSingleton<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers
            services.AddSingleton<IActivationHandler, AppNotificationActivationHandler>();

            // Services
            services.AddSingleton<IAppNotificationService, AppNotificationService>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddSingleton<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            services.AddTransient<DemoBuilderViewModel>();
            services.AddTransient<DemoBuilderPage>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<SettingsPage>();
            services.AddSingleton<MotionViewModel>();
            services.AddSingleton<MotionPage>();
            services.AddSingleton<ParametersViewModel>();
            services.AddSingleton<ParametersPage>();
            services.AddSingleton<HardwareViewModel>();
            services.AddSingleton<HardwarePage>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainPage>();
            services.AddSingleton<ShellPage>();
            services.AddSingleton<ShellViewModel>();

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));

            services.AddSingleton<ShellViewModel>();
            services.AddTransient<SettingsViewModel>();
        }).
        Build();

        App.GetService<IAppNotificationService>().Initialize();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

       // App.GetService<IAppNotificationService>().Show(string.Format("AppNotificationSamplePayload".GetLocalized(), AppContext.BaseDirectory));

        await App.GetService<IActivationService>().ActivateAsync(args);

        base.OnLaunched(args);

        // 2) Attempt to set real-time priority
        TrySetRealTimePriority();
        //StartDeviceWatcher();

    }

    private void TrySetRealTimePriority()
    {
        try
        {
            IntPtr hProcess = GetCurrentProcess();
            bool success = SetPriorityClass(hProcess, REALTIME_PRIORITY_CLASS);
            if (!success)
            {
                int errorCode = Marshal.GetLastWin32Error();
                Debug.WriteLine($"Failed to set real-time priority. Win32 Error={errorCode}");
            }
            else
            {
                Debug.WriteLine("Process set to REALTIME_PRIORITY_CLASS successfully.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception while setting real-time priority: {ex.Message}");
        }
    }

    private async Task CheckForControllers()
    {
        //NotifyIsBusy(true);
        await Task.Delay(010);

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

        //NotiyIsBusy(false);
    }

    private void StartDeviceWatcher()
    {
        Debug.WriteLine("Starting DeviceWatcher in App.xaml.cs...");
        string selector = "System.Devices.InterfaceClassGuid:=\"{A5DCBF10-6530-11D2-901F-00C04FB951ED}\""; // USB GUID

        deviceWatcher = DeviceInformation.CreateWatcher(selector);
        deviceWatcher.Added += DeviceWatcher_Added;
        deviceWatcher.Removed += DeviceWatcher_Removed;
        deviceWatcher.Updated += DeviceWatcher_Updated;
        deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
        deviceWatcher.Stopped += DeviceWatcher_Stopped;

        deviceWatcher.Start();
    }

    private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
    {
        Debug.WriteLine($"(App) Device with COM port detected: {args.Name}, {args.Id}");
        if (sender.Status == DeviceWatcherStatus.EnumerationCompleted)
        {
            // We’re on a background thread, so dispatch to the UI thread if needed
            MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                CheckForControllers();
            });
        }
    }

    private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        Debug.WriteLine($"(App) Device removed: {args.Id}");
        MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            CheckForControllers();
        });
    }

    private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        Debug.WriteLine($"(App) Device updated: {args.Id}");
        MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            CheckForControllers();
        });
    }

    private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
    {
        Debug.WriteLine("(App) Enumeration completed. Checking for controllers...");
        MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            CheckForControllers();
        });
    }

    private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
    {
        Debug.WriteLine("(App) DeviceWatcher stopped.");
    }
}
