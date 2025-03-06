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
            // Subscribe to collection changes to auto-save when programs list changes
            AllSavedPrograms.CollectionChanged += (s, e) => _ = SaveAllProgramsAsync();
            Debug.WriteLine("DemoBuilderViewModel constructor completed.");
        }

        // Called whenever you add or move blocks around.
        public void SaveCurrentProgramState(DemoBuilderPage page)
        {
            if (SelectedProgram == null)
            {
                Debug.WriteLine("No program selected to save state.");
                return;
            }

            // 1) Clear out old block data
            SelectedProgram.Blocks.Clear();

            // 2) Gather the DraggableElements on the workspace and convert them
            var children = page.GetWorkspaceBlocks();
            Debug.WriteLine($"Saving state for '{SelectedProgram.ProgramName}'. Found {children.Count} blocks in canvas.");

            for (int i = 0; i < children.Count; i++)
            {
                var draggable = children[i];
                var savedBlockData = page.ConvertBlockBaseToSavedBlockData(draggable.Block);

                // Position & indexes
                savedBlockData.X = Canvas.GetLeft(draggable);
                savedBlockData.Y = Canvas.GetTop(draggable);
                savedBlockData.PreviousBlockIndex = children.IndexOf(draggable.PreviousBlock);
                savedBlockData.NextBlockIndex = children.IndexOf(draggable.NextBlock);

                // 3) Add to the Program’s Blocks
                SelectedProgram.Blocks.Add(savedBlockData);

                Debug.WriteLine($"Saved block '{draggable.Text}' to '{SelectedProgram.ProgramName}' at ({savedBlockData.X}, {savedBlockData.Y}), PrevIdx={savedBlockData.PreviousBlockIndex}, NextIdx={savedBlockData.NextBlockIndex}");
            }

            // 4) Persist to local settings
            _ = SaveAllProgramsAsync();
            DebugAllProgramsState();
        }

        // Creates a brand new blank program
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
                // Make sure we don't duplicate an existing program name
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
                Debug.WriteLine("Cannot delete the last program.");
                return;
            }

            AllSavedPrograms.Remove(SelectedProgram);
            SelectedProgram = AllSavedPrograms.Count > 0 ? AllSavedPrograms[0] : null;

            await SaveAllProgramsAsync();
            Debug.WriteLine($"Deleted program. Remaining: {AllSavedPrograms.Count}");
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

        // ========= 1) LOAD PROGRAMS FROM APP'S LOCAL SETTINGS  =========
        public async Task LoadAllProgramsAsync()
        {
            Debug.WriteLine("LoadAllProgramsAsync started.");
            try
            {
                // Instead of reading a file from disk, we read from local settings:
                var localSettings = ApplicationData.Current.LocalSettings;

                // See if we have a JSON string stored
                if (localSettings.Values.TryGetValue("AllProgramsJson", out object jsonObj))
                {
                    string json = jsonObj as string;
                    Debug.WriteLine($"Loaded JSON content from local settings: {json}");

                    if (!string.IsNullOrEmpty(json))
                    {
                        var list = JsonSerializer.Deserialize<ObservableCollection<ProgramInfo>>(json);
                        if (list != null && list.Count > 0)
                        {
                            // Make sure the loaded programs have unique names
                            var uniquePrograms = new ObservableCollection<ProgramInfo>();
                            foreach (var program in list)
                            {
                                var uniqueName = GetUniqueProgramName(program.ProgramName);
                                if (uniqueName != program.ProgramName)
                                {
                                    Debug.WriteLine($"Renamed loaded program '{program.ProgramName}' to '{uniqueName}'.");
                                    program.ProgramName = uniqueName;
                                }
                                uniquePrograms.Add(program);
                            }
                            AllSavedPrograms = uniquePrograms;
                            SelectedProgram = AllSavedPrograms[0];
                            Debug.WriteLine($"Loaded {AllSavedPrograms.Count} programs from local settings.");
                            DebugAllProgramsState();
                        }
                        else
                        {
                            Debug.WriteLine("Deserialized an empty or null list from JSON.");
                            if (AllSavedPrograms.Count == 0)
                            {
                                await AddNewProgramAsync();
                            }
                        }
                    }
                    else
                    {
                        // The JSON was empty
                        Debug.WriteLine("No JSON data found in local settings, or empty string.");
                        if (AllSavedPrograms.Count == 0)
                        {
                            await AddNewProgramAsync();
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("No 'AllProgramsJson' key found in local settings.");
                    // If no existing data, create a new default program
                    if (AllSavedPrograms.Count == 0)
                    {
                        await AddNewProgramAsync();
                    }
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

        // ========= 2) SAVE PROGRAMS TO APP'S LOCAL SETTINGS =========
        public async Task SaveAllProgramsAsync()
        {
            for (int i = 0; i < 3; i++) // up to 3 retries if something fails
            {
                try
                {
                    Debug.WriteLine("SaveAllProgramsAsync started.");

                    // Serialize all programs
                    string json = JsonSerializer.Serialize(AllSavedPrograms, new JsonSerializerOptions { WriteIndented = true });
                    Debug.WriteLine($"Serialized JSON to save: {json}");

                    // Store in local settings instead of a file
                    var localSettings = ApplicationData.Current.LocalSettings;
                    localSettings.Values["AllProgramsJson"] = json;

                    Debug.WriteLine($"Programs saved successfully to local settings. Total programs: {AllSavedPrograms.Count}");
                    DebugAllProgramsState();
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving programs (attempt {i + 1}/3): {ex.Message}");
                    if (i < 2) await Task.Delay(100); // small delay before retry
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
                    Debug.WriteLine($"  Block Type={block.BlockType}, X={block.X}, Y={block.Y}, " +
                                    $"PrevIdx={block.PreviousBlockIndex}, NextIdx={block.NextBlockIndex}");
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
