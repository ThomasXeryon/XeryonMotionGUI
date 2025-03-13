using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using XeryonMotionGUI.Views;

namespace XeryonMotionGUI.ViewModels
{
    public partial class DemoBuilderViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ProgramInfo> allSavedPrograms = new();

        [ObservableProperty]
        private ProgramInfo selectedProgram;

        private const string ProgramFileName = "AllPrograms.json";

        public DemoBuilderViewModel()
        {
            Debug.WriteLine("DemoBuilderViewModel constructor started.");
            // Attempt to load from file
            _ = InitializeViewModelAsync();
            // Subscribe to changes to auto-save
            AllSavedPrograms.CollectionChanged += (s, e) => _ = SaveAllProgramsAsync();
            Debug.WriteLine("DemoBuilderViewModel constructor completed.");
        }

        private async Task InitializeViewModelAsync()
        {
            await LoadAllProgramsAsync();

            // If still no programs in memory, create one default
            if (AllSavedPrograms.Count == 0)
            {
                Debug.WriteLine("No programs found in memory after loading. Creating a default program...");
                await AddNewProgramAsync();
                // This ensures at least one program is present
            }
        }

        // Called whenever user changes the blocks in the workspace
        public void SaveCurrentProgramState(DemoBuilderPage page)
        {
            if (SelectedProgram == null)
            {
                Debug.WriteLine("No program selected to save state.");
                return;
            }

            // Clear old block data
            SelectedProgram.Blocks.Clear();

            // Gather DraggableElements and convert them
            var children = page.GetWorkspaceBlocks();
            Debug.WriteLine($"Saving state for '{SelectedProgram.ProgramName}'. Found {children.Count} blocks in canvas.");

            for (int i = 0; i < children.Count; i++)
            {
                var draggable = children[i];
                var savedBlockData = page.ConvertBlockBaseToSavedBlockData(draggable.Block);

                // Store position & connections
                savedBlockData.X = Canvas.GetLeft(draggable);
                savedBlockData.Y = Canvas.GetTop(draggable);
                savedBlockData.PreviousBlockIndex = children.IndexOf(draggable.PreviousBlock);
                savedBlockData.NextBlockIndex = children.IndexOf(draggable.NextBlock);

                SelectedProgram.Blocks.Add(savedBlockData);

                Debug.WriteLine($"Saved block '{draggable.Text}' to '{SelectedProgram.ProgramName}' "
                              + $"at ({savedBlockData.X}, {savedBlockData.Y}), "
                              + $"PrevIdx={savedBlockData.PreviousBlockIndex}, NextIdx={savedBlockData.NextBlockIndex}");
            }

            // Persist updated program list
            _ = SaveAllProgramsAsync();
            DebugAllProgramsState();
        }

        // Creates a brand-new blank program
        [RelayCommand]
        public async Task AddNewProgramAsync()
        {
            var baseName = $"Program {AllSavedPrograms.Count + 1}";
            var newName = GetUniqueProgramName(baseName);

            var newProg = new ProgramInfo(newName, new ObservableCollection<SavedBlockData>());
            AllSavedPrograms.Add(newProg);
            SelectedProgram = newProg;

            await SaveAllProgramsAsync();
            Debug.WriteLine($"Added new program '{newName}' with 0 blocks.");
        }

        [RelayCommand]
        private async Task RenameProgramAsync()
        {
            if (SelectedProgram == null)
            {
                Debug.WriteLine("No program selected to rename.");
                return;
            }

            var originalName = SelectedProgram.ProgramName;
            var newName = await ShowRenameDialogAsync(originalName);

            if (!string.IsNullOrWhiteSpace(newName) && newName != originalName)
            {
                // ensure no duplicates
                if (AllSavedPrograms.Any(p => p != SelectedProgram &&
                                              p.ProgramName.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                {
                    var uniqueName = GetUniqueProgramName(newName);
                    Debug.WriteLine($"Duplicate name '{newName}' detected. Using '{uniqueName}' instead.");
                    newName = uniqueName;
                }

                SelectedProgram.ProgramName = newName;
                await SaveAllProgramsAsync();
                Debug.WriteLine($"Renamed program from '{originalName}' to '{newName}'.");
            }
            else
            {
                Debug.WriteLine("Rename canceled or invalid name provided.");
            }
        }

        [RelayCommand]
        private async Task DeleteProgramAsync()
        {
            if (SelectedProgram == null)
            {
                Debug.WriteLine("No program selected to delete.");
                return;
            }
            if (AllSavedPrograms.Count <= 1)
            {
                Debug.WriteLine("Cannot delete the last program. Must keep at least one.");
                return;
            }

            AllSavedPrograms.Remove(SelectedProgram);
            SelectedProgram = AllSavedPrograms.Count > 0 ? AllSavedPrograms[0] : null;

            await SaveAllProgramsAsync();
            Debug.WriteLine($"Deleted program. Remaining count: {AllSavedPrograms.Count}");
        }

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
                },
                XamlRoot = App.MainWindow.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                return (dialog.Content as TextBox)?.Text ?? string.Empty;
            }
            return string.Empty;
        }

        private string GetUniqueProgramName(string baseName)
        {
            string newName = baseName;
            int suffix = 1;

            while (AllSavedPrograms.Any(p => p.ProgramName.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                newName = $"{baseName} ({suffix++})";
            }
            return newName;
        }

        // ========================================
        // ============ LOADING / SAVING ==========
        // ========================================

        public async Task LoadAllProgramsAsync()
        {
            Debug.WriteLine("LoadAllProgramsAsync (file-based) started.");
            try
            {
                // 1) Attempt to open "AllPrograms.json" from LocalFolder
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFile file = await folder.GetFileAsync(ProgramFileName);

                // 2) Read text & deserialize
                string json = await FileIO.ReadTextAsync(file);
                Debug.WriteLine($"Loaded JSON from {file.Path}:\n{json}");

                var list = JsonSerializer.Deserialize<ObservableCollection<ProgramInfo>>(json);
                if (list != null && list.Count > 0)
                {
                    // Optionally ensure program names are unique
                    var uniquePrograms = new ObservableCollection<ProgramInfo>();
                    foreach (var program in list)
                    {
                        var uniqueName = GetUniqueProgramName(program.ProgramName);
                        if (!uniqueName.Equals(program.ProgramName, StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine($"Renaming '{program.ProgramName}' to '{uniqueName}' to avoid duplicates.");
                            program.ProgramName = uniqueName;
                        }
                        uniquePrograms.Add(program);
                    }

                    AllSavedPrograms = uniquePrograms;
                    SelectedProgram = AllSavedPrograms[0];
                    Debug.WriteLine($"Loaded {AllSavedPrograms.Count} programs from {file.Path}.");
                    DebugAllProgramsState();
                }
                else
                {
                    Debug.WriteLine("Deserialized an empty or null list from JSON. Will create a default if we have no programs in memory.");
                }
            }
            catch (FileNotFoundException)
            {
                Debug.WriteLine($"{ProgramFileName} not found. We'll create one if we have zero programs in memory.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading from file: {ex.Message}");
                // If parse or other error, we just keep whatever is in AllSavedPrograms memory.
            }

            Debug.WriteLine("LoadAllProgramsAsync (file-based) completed.");
        }

        public async Task SaveAllProgramsAsync()
        {
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    Debug.WriteLine("SaveAllProgramsAsync (file-based) started.");

                    // 1) Serialize
                    string json = JsonSerializer.Serialize(AllSavedPrograms,
                        new JsonSerializerOptions { WriteIndented = true });
                    Debug.WriteLine($"Serialized JSON:\n{json}");

                    // 2) Create or replace file
                    StorageFolder folder = ApplicationData.Current.LocalFolder;
                    StorageFile file = await folder.CreateFileAsync(
                        ProgramFileName,
                        CreationCollisionOption.ReplaceExisting);

                    // 3) Write text
                    await FileIO.WriteTextAsync(file, json);

                    Debug.WriteLine($"Programs saved successfully to {file.Path}. "
                                  + $"Total programs: {AllSavedPrograms.Count}");
                    DebugAllProgramsState();
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving programs (attempt {attempt}/3): {ex.Message}");
                    if (attempt < 3) await Task.Delay(100);
                }
            }
            Debug.WriteLine("Failed to save programs after 3 attempts.");
        }

        private void DebugAllProgramsState()
        {
            Debug.WriteLine("Current state of AllSavedPrograms:");
            foreach (var program in AllSavedPrograms)
            {
                Debug.WriteLine($"Program '{program.ProgramName}': {program.Blocks.Count} blocks");
                foreach (var block in program.Blocks)
                {
                    Debug.WriteLine($"  Block Type={block.BlockType}, "
                                  + $"X={block.X}, Y={block.Y}, "
                                  + $"PrevIdx={block.PreviousBlockIndex}, NextIdx={block.NextBlockIndex}");
                }
            }
        }
    }

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
            ProgramName = name;
            Blocks = blocks;
        }
    }

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
        public bool? IsStart
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
