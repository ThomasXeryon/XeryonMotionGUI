using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using System.Linq;

namespace XeryonMotionGUI.ViewModels
{
    public partial class DemoBuilderViewModel : ObservableObject
    {
        [ObservableProperty]
        private string programName;
        // These attributes auto-generate public properties named AllSavedPrograms and SelectedProgram.
        [ObservableProperty]
        private ObservableCollection<ProgramInfo> allSavedPrograms = new ObservableCollection<ProgramInfo>();

        [ObservableProperty]
        private ProgramInfo selectedProgram;

        public DemoBuilderViewModel()
        {
            // Optionally, load programs on creation:
            // _ = LoadAllProgramsAsync();
        }


        // Command to add a new program.
        [RelayCommand]
        private async Task AddNewProgramAsync()
        {
            var newName = $"Program {AllSavedPrograms.Count + 1}";
            var newProg = new ProgramInfo(newName, new ObservableCollection<SavedBlockData>());
            AllSavedPrograms.Add(newProg);
            SelectedProgram = newProg;
            await SaveAllProgramsAsync();
        }

        // Command to rename the selected program.
        [RelayCommand]
        private async Task RenameProgramAsync()
        {
            if (SelectedProgram == null)
                return;

            var newName = await ShowRenameDialogAsync(SelectedProgram.ProgramName);
            if (!string.IsNullOrWhiteSpace(newName))
            {
                SelectedProgram.ProgramName = newName;
                await SaveAllProgramsAsync();
            }
        }

        // Command to delete the selected program.
        [RelayCommand]
        private async Task DeleteProgramAsync()
        {
            if (SelectedProgram == null)
                return;

            AllSavedPrograms.Remove(SelectedProgram);
            SelectedProgram = null;
            await SaveAllProgramsAsync();
        }

        // Helper to show a rename dialog.
        private async Task<string> ShowRenameDialogAsync(string currentName)
        {
            var dialog = new ContentDialog
            {
                Title = "Rename Program",
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = new TextBox
                {
                    Text = currentName,
                    PlaceholderText = "Enter a new name"
                }
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
                return (dialog.Content as TextBox)?.Text ?? string.Empty;
            return string.Empty;
        }

        // Load all programs from JSON.
        public async Task LoadAllProgramsAsync()
        {
            try
            {
                Debug.WriteLine("Starting LoadAllProgramsAsync...");
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFolder blocksFolder = await folder.GetFolderAsync("Blocks");
                StorageFile file = await blocksFolder.GetFileAsync("AllPrograms.json");
                string json = await FileIO.ReadTextAsync(file);
                Debug.WriteLine($"JSON content: {json}");

                var list = JsonSerializer.Deserialize<ObservableCollection<ProgramInfo>>(json);
                if (list != null)
                {
                    AllSavedPrograms = list;
                    if (AllSavedPrograms.Count > 0)
                        SelectedProgram = AllSavedPrograms[0];
                }
                else
                {
                    Debug.WriteLine("Deserialized list is null.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading programs: {ex.Message}");
            }
        }

        // Save all programs to JSON.
        public async Task SaveAllProgramsAsync()
        {
            try
            {
                Debug.WriteLine("Starting SaveAllProgramsAsync...");
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFolder blocksFolder;
                try
                {
                    blocksFolder = await folder.GetFolderAsync("Blocks");
                }
                catch (FileNotFoundException)
                {
                    blocksFolder = await folder.CreateFolderAsync("Blocks");
                }
                StorageFile file = await blocksFolder.CreateFileAsync("AllPrograms.json", CreationCollisionOption.ReplaceExisting);
                string json = JsonSerializer.Serialize(AllSavedPrograms, new JsonSerializerOptions { WriteIndented = true });
                Debug.WriteLine($"Serialized JSON: {json}");
                await FileIO.WriteTextAsync(file, json);
                Debug.WriteLine("Programs saved successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving programs: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }

    // ProgramInfo class defined in the ViewModels namespace.
    public partial class ProgramInfo : ObservableObject
    {
        [ObservableProperty]
        private string programName;

        public ObservableCollection<SavedBlockData> Blocks
        {
            get; set;
        }

        public ProgramInfo()
        {
            Blocks = new ObservableCollection<SavedBlockData>();
        }

        public ProgramInfo(string name, ObservableCollection<SavedBlockData> blocks)
        {
            programName = name;
            Blocks = blocks;
        }
    }

    // SavedBlockData class defined in the ViewModels namespace.
    public class SavedBlockData
    {
        public string BlockType
        {
            get; set;
        }
        public double X
        {
            get; set;
        }
        public double Y
        {
            get; set;
        }
        public string AxisSerial
        {
            get; set;
        }
        public string ControllerFriendlyName
        {
            get; set;
        }
        public int? NextBlockIndex
        {
            get; set;
        }
        public int? PreviousBlockIndex
        {
            get; set;
        }
        public int? WaitTime
        {
            get; set;
        }
        public bool? IsPositive
        {
            get; set;
        }
        public int? StepSize
        {
            get; set;
        }
        public string SelectedParameter
        {
            get; set;
        }
        public int? ParameterValue
        {
            get; set;
        }
        public int? RepeatCount
        {
            get; set;
        }
        public int? BlocksToRepeat
        {
            get; set;
        }
    }
}
