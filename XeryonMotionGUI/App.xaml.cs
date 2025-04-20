using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Windows.Devices.Enumeration;
using Windows.Storage;
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

public partial class App : Application
{
    private DeviceWatcher deviceWatcher;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetPriorityClass(IntPtr hProcess, uint dwPriorityClass);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetCurrentProcess();

    private const uint REALTIME_PRIORITY_CLASS = 0x100;

    public IHost Host
    {
        get;
    }

    public static T GetService<T>() where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }
        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();
    private AppNotificationManager notificationManager;

    public static UIElement? AppTitlebar
    {
        get; set;
    }

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices((context, services) =>
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
                services.AddSingleton<IntroPageViewModel>();
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
            })
            .Build();

        App.GetService<IAppNotificationService>().Initialize();
        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Handle unhandled exceptions if needed
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        // Initialize notification service
        //App.GetService<IAppNotificationService>().Show(string.Format("AppNotificationSamplePayload".GetLocalized(), AppContext.BaseDirectory));

        // Activate the app using the activation service
        await App.GetService<IActivationService>().ActivateAsync(args);

        // Set real-time priority
        TrySetRealTimePriority();

        // Navigate to MainPage
        var navigationService = App.GetService<INavigationService>();
        navigationService.NavigateTo(typeof(HardwareViewModel).FullName, args.Arguments);

        // Activate the window (fullscreen will be handled in ShellPage)
        App.MainWindow.Activate();
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
        await Task.Delay(10);

        string[] ports = SerialPort.GetPortNames();

        foreach (var ctrl in Controller.FoundControllers)
        {
            if (ctrl.Running && !ports.Contains(ctrl.FriendlyPort))
            {
                Debug.WriteLine($"Port {ctrl.FriendlyPort} no longer present => marking {ctrl.Name} as not running.");
                ctrl.Running = false;
                ctrl.Status = "Idle";
            }
        }

        foreach (var portName in ports)
        {
            using (var tempPort = new SerialPort(portName) { BaudRate = 115200, ReadTimeout = 200 })
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

                    if (string.IsNullOrEmpty(idResult.Label))
                    {
                        idResult.Label = idResult.AxisCount == 1 ? "" : new string("ABCDEFGHIJKLMNOPQRSTUVWXYZ".Take(idResult.AxisCount).ToArray());
                    }

                    var existing = Controller.FoundControllers.FirstOrDefault(c => c.FriendlyPort == portName);
                    if (existing != null)
                    {
                        if (!existing.Running)
                        {
                            Debug.WriteLine($"Auto reconnecting to {portName}...");
                            tempPort.Close();
                            existing.ReconnectController();
                            continue;
                        }
                        else
                        {
                            Debug.WriteLine($"Skipping duplicate controller on {portName} (already running).");
                            tempPort.Close();
                            continue;
                        }
                    }

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

                    controller.Axes = new ObservableCollection<Axis>();
                    for (int i = 0; i < idResult.AxisCount; i++)
                    {
                        string axisLetter = (!string.IsNullOrEmpty(controller.Label) && controller.Label.Length > i) ? controller.Label[i].ToString() : "";
                        var axis = new Axis(controller, "Placeholder", axisLetter);
                        controller.Axes.Add(axis);
                    }

                    tempPort.Write("INFO=1");
                    tempPort.Write("POLI=25");
                    await Task.Delay(100);
                    var response = tempPort.ReadExisting();
                    response = string.Join("\n", response.Split('\n').TakeLast(120));
                    Debug.WriteLine("Response from controller on " + portName + ": " + response);
                    tempPort.Write("INFO=0");
                    await Task.Delay(100);
                    tempPort.DiscardInBuffer();

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

                    string toastXml = $@"
<toast>
    <visual>
        <binding template='ToastGeneric'>
            <text>New Controller Found</text>
            <text>Discovered {controller.FriendlyName} on port {controller.FriendlyPort}</text>
        </binding>
    </visual>
    <actions>
        <action activationType='foreground' arguments='action=ConnectController&port={controller.FriendlyPort}' content='Connect' />
    </actions>
</toast>";
                    App.GetService<IAppNotificationService>().Show(toastXml);
                }
                catch (FileNotFoundException ex)
                {
                    Debug.WriteLine($"Error processing port {portName}: {ex.Message}");
                    var existing = Controller.FoundControllers.FirstOrDefault(c => c.FriendlyPort == portName);
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

        var toRemove = Controller.FoundControllers.Where(ctrl => !ctrl.Running && !ports.Contains(ctrl.FriendlyPort)).ToList();
        foreach (var deadCtrl in toRemove)
        {
            Debug.WriteLine($"Removing controller {deadCtrl.Name} on {deadCtrl.FriendlyPort}, since port not present & not running.");
            Controller.FoundControllers.Remove(deadCtrl);
        }
    }

    private void StartDeviceWatcher()
    {
        Debug.WriteLine("Starting DeviceWatcher in App.xaml.cs...");
        string selector = "System.Devices.InterfaceClassGuid:=\"{A5DCBF10-6530-11D2-901F-00C04FB951ED}\"";
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
            MainWindow.DispatcherQueue.TryEnqueue(() => CheckForControllers());
        }
    }

    private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        Debug.WriteLine($"(App) Device removed: {args.Id}");
        MainWindow.DispatcherQueue.TryEnqueue(() => CheckForControllers());
    }

    private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        Debug.WriteLine($"(App) Device updated: {args.Id}");
        MainWindow.DispatcherQueue.TryEnqueue(() => CheckForControllers());
    }

    private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
    {
        Debug.WriteLine("(App) Enumeration completed. Checking for controllers...");
        MainWindow.DispatcherQueue.TryEnqueue(() => CheckForControllers());
    }

    private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
    {
        Debug.WriteLine("(App) DeviceWatcher stopped.");
    }
}