using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        private ObservableCollection<ProgramInfo> allSavedPrograms = new ObservableCollection<ProgramInfo>();

        [ObservableProperty]
        private ProgramInfo selectedProgram;

        public DemoBuilderViewModel()
        {
            Debug.WriteLine("DemoBuilderViewModel constructor started.");
            _ = LoadAllProgramsAsync();
            Debug.WriteLine("DemoBuilderViewModel constructor completed.");
        }

        public void SaveCurrentProgramState(DemoBuilderPage page)
        {
            if (SelectedProgram == null)
            {
                Debug.WriteLine("No program selected to save state.");
                return;
            }

            SelectedProgram.Blocks.Clear();
            var children = page.GetWorkspaceBlocks();
            Debug.WriteLine($"Saving state for '{SelectedProgram.ProgramName}'. Found {children.Count} blocks in canvas.");
            for (int i = 0; i < children.Count; i++)
            {
                var draggable = children[i];
                var savedBlockData = page.ConvertBlockBaseToSavedBlockData(draggable.Block);
                savedBlockData.X = Canvas.GetLeft(draggable);
                savedBlockData.Y = Canvas.GetTop(draggable);
                savedBlockData.PreviousBlockIndex = children.IndexOf(draggable.PreviousBlock);
                savedBlockData.NextBlockIndex = children.IndexOf(draggable.NextBlock);
                SelectedProgram.Blocks.Add(savedBlockData);
                Debug.WriteLine($"Saved block '{draggable.Text}' to '{SelectedProgram.ProgramName}' at ({savedBlockData.X}, {savedBlockData.Y}), PrevIdx={savedBlockData.PreviousBlockIndex}, NextIdx={savedBlockData.NextBlockIndex}");
            }
            _ = SaveAllProgramsAsync();
            DebugAllProgramsState();
        }

        private void DebugAllProgramsState()
        {
            Debug.WriteLine("Current state of AllSavedPrograms:");
            foreach (var program in AllSavedPrograms)
            {
                Debug.WriteLine($"Program '{program.ProgramName}': {program.Blocks.Count} blocks");
                foreach (var block in program.Blocks)
                {
                    Debug.WriteLine($"  Block Type={block.BlockType}, X={block.X}, Y={block.Y}, PrevIdx={block.PreviousBlockIndex}, NextIdx={block.NextBlockIndex}");
                }
            }
        }

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
                // Check for duplicate names
                if (AllSavedPrograms.Any(p => p != SelectedProgram && p.ProgramName.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                {
                    var uniqueName = GetUniqueProgramName(newName);
                    Debug.WriteLine($"Duplicate name '{newName}' detected. Using unique name: '{uniqueName}'.");
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
                Debug.WriteLine("Cannot delete the last program.");
                return; // Prevent deletion if only one program remains
            }

            AllSavedPrograms.Remove(SelectedProgram);
            SelectedProgram = AllSavedPrograms.Count > 0 ? AllSavedPrograms[0] : null;
            await SaveAllProgramsAsync();
            Debug.WriteLine($"Deleted program. Remaining programs: {AllSavedPrograms.Count}");
        }

        private async Task<string> ShowRenameDialogAsync(string currentName)
        {
            var dialog = new ContentDialog
            {
                Title = "Rename Program",
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = new TextBox { Text = currentName, PlaceholderText = "Enter a new name" },
                XamlRoot = App.MainWindow.Content.XamlRoot // Ensure XamlRoot is set correctly
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? (dialog.Content as TextBox)?.Text ?? string.Empty : string.Empty;
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

        public async Task LoadAllProgramsAsync()
        {
            Debug.WriteLine("LoadAllProgramsAsync started.");
            try
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFolder blocksFolder = await folder.CreateFolderAsync("Blocks", CreationCollisionOption.OpenIfExists);
                StorageFile file = await blocksFolder.CreateFileAsync("AllPrograms.json", CreationCollisionOption.OpenIfExists);
                string json = await FileIO.ReadTextAsync(file);
                if (!string.IsNullOrEmpty(json))
                {
                    var list = JsonSerializer.Deserialize<ObservableCollection<ProgramInfo>>(json);
                    if (list != null && list.Count > 0)
                    {
                        // Ensure unique names for loaded programs
                        var uniquePrograms = new ObservableCollection<ProgramInfo>();
                        foreach (var program in list)
                        {
                            var uniqueName = GetUniqueProgramName(program.ProgramName);
                            if (uniqueName != program.ProgramName)
                            {
                                Debug.WriteLine($"Renamed loaded program '{program.ProgramName}' to '{uniqueName}' to avoid duplicate.");
                                program.ProgramName = uniqueName;
                            }
                            uniquePrograms.Add(program);
                        }
                        AllSavedPrograms = uniquePrograms;
                        SelectedProgram = AllSavedPrograms[0];
                        Debug.WriteLine($"Loaded {AllSavedPrograms.Count} programs from file.");
                        DebugAllProgramsState();
                    }
                }
                else if (AllSavedPrograms.Count == 0)
                {
                    await AddNewProgramAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading programs: {ex.Message}");
                if (AllSavedPrograms.Count == 0)
                {
                    await AddNewProgramAsync();
                }
            }
            Debug.WriteLine("LoadAllProgramsAsync completed.");
        }

        public async Task SaveAllProgramsAsync()
        {
            int retries = 3;
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    StorageFolder folder = ApplicationData.Current.LocalFolder;
                    StorageFolder blocksFolder = await folder.CreateFolderAsync("Blocks", CreationCollisionOption.OpenIfExists);
                    StorageFile file = await blocksFolder.CreateFileAsync("AllPrograms.json", CreationCollisionOption.ReplaceExisting);
                    string json = JsonSerializer.Serialize(AllSavedPrograms, new JsonSerializerOptions { WriteIndented = true });
                    await FileIO.WriteTextAsync(file, json);
                    Debug.WriteLine($"Programs saved successfully. Total programs: {AllSavedPrograms.Count}");
                    DebugAllProgramsState();
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving programs (attempt {i + 1}/{retries}): {ex.Message}");
                    if (i < retries - 1) await Task.Delay(100); // Wait before retrying
                }
            }
            Debug.WriteLine("Failed to save programs after all retries.");
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
        } // For LoggingBlock
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