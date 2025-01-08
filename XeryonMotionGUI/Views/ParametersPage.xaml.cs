using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using Windows.Storage;
using XeryonMotionGUI.Classes;
using Microsoft.UI.Xaml.Media;
using XeryonMotionGUI.ViewModels;
using Windows.UI;
using Microsoft.UI;
using System.Threading.Tasks;

namespace XeryonMotionGUI.Views
{
    public sealed partial class ParametersPage : Page
    {
        public ObservableCollection<Controller> RunningControllers => Controller.RunningControllers;

        public ParametersPage()
        {
            InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;

            // Setting the DataContext if it's not already set
            if (this.DataContext == null)
            {
                this.DataContext = new ParametersViewModel();
            }
        }

        private async void OnFilePickerButtonClick(object sender, RoutedEventArgs e)
        {
            var senderButton = sender as Button;
            if (senderButton == null)
                return;
            senderButton.IsEnabled = false; 
            var window = App.MainWindow;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var picker = new FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.ViewMode = PickerViewMode.List;
            picker.FileTypeFilter.Add(".txt");
            var file = await picker.PickSingleFileAsync();
            var icon = senderButton.Content as SymbolIcon;
            if (icon == null)
            {
                senderButton.IsEnabled = true; 
                return;
            }
            var originalIcon = icon.Symbol;
            if (file != null && file.FileType == ".txt")
            {
                icon.Symbol = Symbol.Accept;  

            }
            else
            {
                icon.Symbol = Symbol.Cancel; 
            }
            await Task.Delay(1000);
            icon.Symbol = originalIcon;  
            senderButton.IsEnabled = true;  
        }

        private async void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null || !button.IsEnabled)
                return;

            var icon = button.Content as SymbolIcon;
            if (icon == null)
                return;

            var originalIcon = icon.Symbol;
            button.IsEnabled = false;  // Disable the button while saving

            await Task.Delay(500);
            icon.Symbol = Symbol.Accept;  // Change icon to accept while saving

            await Task.Delay(1000);
            icon.Symbol = originalIcon;  // Reset the icon after save
            button.IsEnabled = true;  // Re-enable the button after saving
        }
    }
}
