using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using XeryonMotionGUI.Classes;
using XeryonMotionGUI.ViewModels;
using WinRT.Interop;

namespace XeryonMotionGUI.Views
{
    public sealed partial class ParametersPage : Page
    {
        public ObservableCollection<Controller> RunningControllers => Controller.RunningControllers;

        public ParametersPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
            if (this.DataContext == null)
            {
                this.DataContext = new ParametersViewModel();
            }
        }

        private async void OnFilePickerButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button senderButton)
                return;
            senderButton.IsEnabled = false;
            try
            {
                var openPicker = new FileOpenPicker();
                var window = App.MainWindow;
                var hWnd = WindowNative.GetWindowHandle(window);
                InitializeWithWindow.Initialize(openPicker, hWnd);
                openPicker.ViewMode = PickerViewMode.Thumbnail;
                openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                openPicker.FileTypeFilter.Add(".txt");
                var file = await openPicker.PickSingleFileAsync();
                if (file != null && file.FileType == ".txt")
                {
                    string fileContent = await FileIO.ReadTextAsync(file);
                    ShowTextEditPopup(fileContent, async (editedContent) =>
                    {
                        await FileIO.WriteTextAsync(file, editedContent);
                        SendSettingsToDriver(editedContent);
                    });
                    await UpdateButtonWithIconAsync(senderButton, Symbol.Accept, 1000);
                }
                else
                {
                    await UpdateButtonWithIconAsync(senderButton, Symbol.Cancel, 1000);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                await UpdateButtonWithIconAsync(senderButton, Symbol.Cancel, 1000);
            }
        }

        private async Task UpdateButtonWithIconAsync(Button button, Symbol newSymbol, int delayMs)
        {
            if (button.Content is not SymbolIcon icon)
                return;
            var originalIcon = icon.Symbol;
            button.IsEnabled = false;
            icon.Symbol = newSymbol;
            await Task.Delay(delayMs);
            icon.Symbol = originalIcon;
            button.IsEnabled = true;
        }

        private void ShowTextEditPopup(string content, Action<string> sendSettingsToDriver)
        {
            var popup = new ContentDialog
            {
                Title = "Edit Settings File?",
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };
            var textBox = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Text = content,
                Margin = new Thickness(0),
                Height = 500,
                Width = 400
            };
            popup.Content = new ScrollViewer
            {
                Content = textBox,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            popup.XamlRoot = App.MainWindow.Content.XamlRoot;
            popup.PrimaryButtonClick += (s, e) =>
            {
                string editedContent = textBox.Text;
                sendSettingsToDriver(editedContent);
            };
            popup.ShowAsync();
        }

        private void SendSettingsToDriver(string settings)
        {
            if (this.DataContext is ParametersViewModel viewModel)
            {
                var selectedController = viewModel.SelectedController;
                selectedController?.UploadSettings(settings);
            }
        }

        private async void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is not ParametersViewModel viewModel)
                return;
            var selectedController = viewModel.SelectedController;
            if (sender is not Button button || !button.IsEnabled)
                return;
            if (button.Content is not SymbolIcon icon)
                return;
            await UpdateButtonWithIconAsync(button, Symbol.Accept, 500);
            selectedController?.SaveSettings();
        }
    }
}
