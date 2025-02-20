using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using XeryonMotionGUI.Contracts.Services;
using XeryonMotionGUI.ViewModels;
using System.Web;  // so you have NameValueCollection, HttpUtility, etc.

namespace XeryonMotionGUI.Activation
{
    public class AppNotificationActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
    {
        private readonly INavigationService _navigationService;
        private readonly IAppNotificationService _notificationService;

        public AppNotificationActivationHandler(INavigationService navigationService, IAppNotificationService notificationService)
        {
            _navigationService = navigationService;
            _notificationService = notificationService;
        }

        protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
        {
            return AppInstance.GetCurrent().GetActivatedEventArgs()?.Kind == ExtendedActivationKind.AppNotification;
        }

        protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
        {
            // Access the AppNotificationActivatedEventArgs
            var activatedEventArgs = (AppNotificationActivatedEventArgs)
                AppInstance.GetCurrent().GetActivatedEventArgs().Data;

            // e.g. activatedEventArgs.Argument might be "action=HardwarePage&port=COM3"
            var parsed = _notificationService.ParseArguments(activatedEventArgs.Argument);
            // 'parsed' is a NameValueCollection

            // Instead of parsed.TryGetValue("action", out var action), do:
            string action = parsed["action"];
            if (string.IsNullOrEmpty(action))
            {
                // No recognized 'action'
                await Task.CompletedTask;
                return;
            }

            if (action == "Settings")
            {
                // Navigate to the Settings page
                App.MainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    _navigationService.NavigateTo(typeof(SettingsViewModel).FullName!);
                });
            }
            else if (action == "HardwarePage")
            {
                // For the 'port' argument, do the same:
                string portValue = parsed["port"];
                if (string.IsNullOrEmpty(portValue))
                {
                    portValue = "";
                }

                App.MainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    // Navigate to your hardware page or viewmodel
                    _navigationService.NavigateTo(typeof(HardwareViewModel).FullName!);
                    // e.g. _navigationService.NavigateTo(typeof(HardwareViewModel).FullName!, portValue);
                });
            }
            else
            {
                // Unrecognized action => handle accordingly
                App.MainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    App.MainWindow.ShowMessageDialogAsync(
                        "Unknown notification action: " + action,
                        "Notification Activation");
                });
            }

            // Optionally remove or replace this example message:
            App.MainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                App.MainWindow.ShowMessageDialogAsync("Handled App Notification Activation.", "Notification Activation");
            });

            await Task.CompletedTask;
        }
    }
}
