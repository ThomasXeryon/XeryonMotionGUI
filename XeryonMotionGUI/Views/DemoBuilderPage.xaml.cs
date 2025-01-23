using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.Storage.Pickers;
using System.Windows;
using WinRT.Interop;
using Windows.Storage;
using XeryonMotionGUI.Classes;

namespace XeryonMotionGUI.Views
{
    public sealed partial class DemoBuilderPage : Page
    {
        private List<Block> _blocks = new List<Block>();

        public DemoBuilderPage()
        {
            this.InitializeComponent();
        }

        // DragStarting: Called when dragging starts
        private void Block_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var button = sender as Button;
            if (button != null)
            {
                Debug.WriteLine($"Dragging block: {button.Tag}");
                args.Data.Properties.Add("BlockType", button.Tag.ToString()); // Add custom data
                args.Data.SetText(button.Content.ToString()); // Set block content as text
            }
        }

        // Drop: Called when dropping the block on the canvas
        private async void WorkspaceCanvas_Drop(object sender, DragEventArgs args)
        {
            Debug.WriteLine("Drop event fired.");

            if (args.DataView.Properties.TryGetValue("BlockType", out var blockType))
            {
                // Retrieve the text of the block being dropped
                string blockContent = await args.DataView.GetTextAsync();

                // Create a new Button for the dropped block
                var block = new Button
                {
                    Content = blockContent,
                    Tag = blockType,
                    Margin = new Thickness(10),
                    Padding = new Thickness(10),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };

                // Position the block in the workspace
                var position = args.GetPosition(WorkspaceCanvas);
                Canvas.SetLeft(block, position.X);
                Canvas.SetTop(block, position.Y);

                WorkspaceCanvas.Children.Add(block); // Add the block to the canvas
                _blocks.Add(new Block(blockType.ToString())); // Add the block to your internal list
            }
        }

        private void WorkspaceCanvas_DragOver(object sender, DragEventArgs args)
        {
            Debug.WriteLine("DragOver event fired.");
            args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy; // Allow copy
        }

        // Execute the Demo
        private async void RunDemo_Click(object sender, RoutedEventArgs e)
        {
            var selectedController = GetSelectedController(); // Implement this method to get the selected controller
            if (selectedController == null)
            {
                ShowError("No controller selected.");
                return;
            }

            foreach (var block in _blocks)
            {
                await block.ExecuteAsync(selectedController);
            }
        }

        // Save the Demo
        private async void SaveDemo_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Demo File", new List<string> { ".demo" });
            savePicker.SuggestedFileName = "Demo";

            var hWnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(savePicker, hWnd);

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                var json = JsonConvert.SerializeObject(_blocks);
                await FileIO.WriteTextAsync(file, json);
            }
        }

        // Load the Demo
        private async void LoadDemo_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker();
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".demo");

            var hWnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(openPicker, hWnd);

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                var json = await FileIO.ReadTextAsync(file);
                _blocks = JsonConvert.DeserializeObject<List<Block>>(json);

                // Clear the workspace and add the loaded blocks
                WorkspaceCanvas.Children.Clear();
                foreach (var block in _blocks)
                {
                    var blockButton = new Button
                    {
                        Content = block.Type,
                        Tag = block.Type,
                        Margin = new Thickness(10),
                        Padding = new Thickness(10),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    WorkspaceCanvas.Children.Add(blockButton);
                }
            }
        }

        // Helper Methods
        private Controller GetSelectedController()
        {
            // Implement logic to get the selected controller
            Debug.WriteLine("GetSelectedController called.");
            return null;
        }

        private void ShowError(string message)
        {
            Debug.WriteLine($"Error: {message}");
            // Implement logic to show an error message (e.g., using a ContentDialog)
        }
    }
}