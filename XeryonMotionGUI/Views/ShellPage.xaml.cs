﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Windows.System;

using XeryonMotionGUI.Contracts.Services;
using XeryonMotionGUI.Helpers;
using XeryonMotionGUI.ViewModels;

namespace XeryonMotionGUI.Views;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel
    {
        get;
    }

    public ShellPage(ShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ViewModel.NavigationService.Frame = NavigationFrame;
        ViewModel.NavigationViewService.Initialize(NavigationViewControl);

        // https://docs.microsoft.com/windows/apps/develop/title-bar?tabs=winui3#full-customization
        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
        App.MainWindow.Activated += MainWindow_Activated;
        AppTitleBarText.Text = "AppDisplayName".GetLocalized();

        this.InitializeComponent();
        ViewModel = App.GetService<ShellViewModel>(); // Ensure the ViewModel is resolved
        DataContext = ViewModel;
    }

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);

        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu));
        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoBack));
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        App.AppTitlebar = AppTitleBarText as UIElement;
    }

    private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        AppTitleBar.Margin = new Thickness()
        {
            Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
            Top = AppTitleBar.Margin.Top,
            Right = AppTitleBar.Margin.Right,
            Bottom = AppTitleBar.Margin.Bottom
        };
    }

    private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
    {
        var keyboardAccelerator = new KeyboardAccelerator() { Key = key };

        if (modifiers.HasValue)
        {
            keyboardAccelerator.Modifiers = modifiers.Value;
        }

        keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;

        return keyboardAccelerator;
    }

    private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var navigationService = App.GetService<INavigationService>();

        var result = navigationService.GoBack();

        args.Handled = result;
    }

    private void PopOutButton_Click(object sender, RoutedEventArgs e)
    {
        if (NavigationFrame.Content is Page currentPage)
        {
            // Get the type of the current page.
            var pageType = currentPage.GetType();

            // Create a new window.
            var newWindow = new Window();

            // Create an instance of NavigationRootPage (the navigation container).
            var navRootPage = new NavigationRootPage();

            // Copy the RequestedTheme from the current page to ensure consistency.
            if (currentPage is FrameworkElement fe)
            {
                navRootPage.RequestedTheme = fe.RequestedTheme;
            }
            else
            {
                navRootPage.RequestedTheme = ElementTheme.Default;
            }

            // Set the NavigationRootPage as the content for the new window.
            newWindow.Content = navRootPage;

            // Activate the new window.
            newWindow.Activate();

            // Navigate to the target page using the container's frame.
            navRootPage.Navigate(pageType, null);
        }
        else
        {
            ShowErrorDialog("The current content is not a valid page.");
        }
    }


    // Show an error dialog
    private async void ShowErrorDialog(string message)
    {
        ContentDialog errorDialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };

        await errorDialog.ShowAsync();
    }
}
