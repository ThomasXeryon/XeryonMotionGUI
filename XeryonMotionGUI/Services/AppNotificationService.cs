using System.Collections.Specialized;
using System.Web;

using Microsoft.Windows.AppNotifications;
using XeryonMotionGUI.Classes;
using XeryonMotionGUI.Contracts.Services;
using XeryonMotionGUI.ViewModels;

namespace XeryonMotionGUI.Notifications;

public class AppNotificationService : IAppNotificationService
{
    private readonly INavigationService _navigationService;

    public AppNotificationService(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    ~AppNotificationService()
    {
        Unregister();
    }

    public void Initialize()
    {
        AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;

        AppNotificationManager.Default.Register();
    }

    public void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        // e.g. "action=ConnectController&port=COM3"
        var parsed = ParseArguments(args.Argument);
        string action = parsed["action"];

        if (action == "ConnectController")
        {
            string port = parsed["port"];
            // *** Put your "connect" logic here. ***
            // For instance:
            // 1) Find the matching controller from FoundControllers, if it exists
            // 2) If it's not running, call your 'ReconnectController()' or similar method

            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                var existing = Controller.FoundControllers
                                         .FirstOrDefault(c => c.FriendlyPort == port);

                if (existing != null && !existing.Running)
                {
                    // Reconnect or connect logic
                    existing.ReconnectController();
                    /*App.MainWindow.ShowMessageDialogAsync(
                        $"Attempting to connect {existing.Name} on {port}...",
                        "Connect Controller");*/
                }
                else
                {
                    /*App.MainWindow.ShowMessageDialogAsync(
                        $"No matching disconnected controller on {port} found or already running.",
                        "Connect Controller");*/
                }
            });
        }
        else if (action == "Settings")
        {
            // The existing example for going to settings page
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                _navigationService.NavigateTo(typeof(SettingsViewModel).FullName!);
            });
        }

/*        // Optional info or debug message
        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            App.MainWindow.ShowMessageDialogAsync(
                "Controller action invoked from toast!",
                "Notification Invoked");

            App.MainWindow.BringToFront();
        });*/
    }


    public bool Show(string payload)
    {
        var appNotification = new AppNotification(payload);

        AppNotificationManager.Default.Show(appNotification);

        return appNotification.Id != 0;
    }

    public NameValueCollection ParseArguments(string arguments)
    {
        return HttpUtility.ParseQueryString(arguments);
    }

    public void Unregister()
    {
        AppNotificationManager.Default.Unregister();
    }
}
