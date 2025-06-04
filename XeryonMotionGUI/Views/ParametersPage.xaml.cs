using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;
using XeryonMotionGUI.ViewModels;
using XeryonMotionGUI.Classes;

namespace XeryonMotionGUI.Views
{
    public sealed partial class ParametersPage : Page
    {
        private readonly ParametersViewModel _vm;

        public ParametersPage()
        {
            InitializeComponent();
            NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;

            _vm = App.GetService<ParametersViewModel>();
            DataContext = _vm;
        }

        private async void OnFilePickerButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem menuItem)
                return;

            // grab the controller from the flyout item’s DataContext
            var ctrl = (Controller)menuItem.DataContext;
            _vm.SelectedController = ctrl;

            // now open the picker as before…
            var openPicker = new FileOpenPicker();
            InitializeWithWindow.Initialize(openPicker, WindowNative.GetWindowHandle(App.MainWindow));
            openPicker.FileTypeFilter.Add(".txt");
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                var content = await FileIO.ReadTextAsync(file);
                ShowTextEditPopup(content, edited =>
                {
                    FileIO.WriteTextAsync(file, edited).AsTask().Wait();
                    ctrl.UploadSettings(edited);
                });
            }
        }

        private void SendSettingsToDriver(string settings)
        {
            if (this.DataContext is ParametersViewModel viewModel)
            {
                var selectedController = viewModel.SelectedController;
                selectedController?.UploadSettings(settings);
            }
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
                IsReadOnly = true,
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



        private async void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem item) return;
            var ctrl = (Controller)item.DataContext;
            _vm.SelectedController = ctrl;

            await Task.Run(() => ctrl.SaveSettings());
        }

        private async void OnSaveAllButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem item) return;

            var ctrl = (Controller)item.DataContext;
            _vm.SelectedController = ctrl;

            var sb = new StringBuilder();
            foreach (var axis in ctrl.Axes)
            {
                sb.AppendLine($"# Axis: {axis.FriendlyName}");
                foreach (var p in axis.Parameters)
                {
                    // use the Command (e.g. "ENBL") instead of Name, no spaces
                    sb.AppendLine($"{p.Command}={p.Value}");
                }
                sb.AppendLine();
            }

            var savePicker = new FileSavePicker();
            InitializeWithWindow.Initialize(savePicker, WindowNative.GetWindowHandle(App.MainWindow));
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Text file", new[] { ".txt" });
            savePicker.SuggestedFileName = "ParametersExport";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await FileIO.WriteTextAsync(file, sb.ToString());
            }
        }

    }
}
