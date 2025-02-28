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
        private ObservableCollection<XeryonMotionGUI.ViewModels.ProgramInfo> allSavedPrograms = new ObservableCollection<XeryonMotionGUI.ViewModels.ProgramInfo>();

        [ObservableProperty]
        private XeryonMotionGUI.ViewModels.ProgramInfo selectedProgram;

        public DemoBuilderViewModel()
        {
            _ = LoadAllProgramsAsync();
        }

        public void SaveCurrentProgramState(DemoBuilderPage page)
        {
            if (SelectedProgram != null)
            {
                SelectedProgram.Blocks.Clear();
                var children = page.WorkspaceCanvas.Children
                    .OfType<DraggableElement>()
                    .Where(de => de != page.SnapShadow && !(de.Block is Blocks.StartBlock))
                    .ToList();
                foreach (var draggable in children)
                {
                    var savedBlockData = page.ConvertBlockBaseToSavedBlockData(draggable.Block);
                    savedBlockData.X = Canvas.GetLeft(draggable);
                    savedBlockData.Y = Canvas.GetTop(draggable);
                    savedBlockData.PreviousBlockIndex = children.IndexOf(draggable.PreviousBlock);
                    savedBlockData.NextBlockIndex = children.IndexOf(draggable.NextBlock);
                    SelectedProgram.Blocks.Add(savedBlockData);
                }
                _ = SaveAllProgramsAsync();
            }
        }

        [RelayCommand]
        public async Task AddNewProgramAsync()
        {
            var newName = $"Program {AllSavedPrograms.Count + 1}";
            var newProg = new XeryonMotionGUI.ViewModels.ProgramInfo(newName, new ObservableCollection<SavedBlockData>());
            AllSavedPrograms.Add(newProg);
            SelectedProgram = newProg;
            await SaveAllProgramsAsync();
        }

        [RelayCommand]
        private async Task RenameProgramAsync()
        {
            if (SelectedProgram == null) return;
            var newName = await ShowRenameDialogAsync(SelectedProgram.programName); // Changed to programName
            if (!string.IsNullOrWhiteSpace(newName))
            {
                SelectedProgram.programName = newName; // Changed to programName
                await SaveAllProgramsAsync();
            }
        }

        [RelayCommand]
        private async Task DeleteProgramAsync()
        {
            if (SelectedProgram == null) return;
            AllSavedPrograms.Remove(SelectedProgram);
            SelectedProgram = null;
            await SaveAllProgramsAsync();
        }

        private async Task<string> ShowRenameDialogAsync(string currentName)
        {
            var dialog = new ContentDialog
            {
                Title = "Rename Program",
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = new TextBox { Text = currentName, PlaceholderText = "Enter a new name" }
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? (dialog.Content as TextBox)?.Text ?? string.Empty : string.Empty;
        }

        public async Task LoadAllProgramsAsync()
        {
            try
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFolder blocksFolder = await folder.CreateFolderAsync("Blocks", CreationCollisionOption.OpenIfExists);
                StorageFile file = await blocksFolder.CreateFileAsync("AllPrograms.json", CreationCollisionOption.OpenIfExists);
                string json = await FileIO.ReadTextAsync(file);
                if (!string.IsNullOrEmpty(json))
                {
                    var list = JsonSerializer.Deserialize<ObservableCollection<XeryonMotionGUI.ViewModels.ProgramInfo>>(json);
                    if (list != null)
                    {
                        AllSavedPrograms = list;
                        if (AllSavedPrograms.Count > 0 && SelectedProgram == null)
                            SelectedProgram = AllSavedPrograms[0];
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading programs: {ex.Message}");
            }
        }

        public async Task SaveAllProgramsAsync()
        {
            try
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFolder blocksFolder = await folder.CreateFolderAsync("Blocks", CreationCollisionOption.OpenIfExists);
                StorageFile file = await blocksFolder.CreateFileAsync("AllPrograms.json", CreationCollisionOption.ReplaceExisting);
                string json = JsonSerializer.Serialize(AllSavedPrograms, new JsonSerializerOptions { WriteIndented = true });
                await FileIO.WriteTextAsync(file, json);
                Debug.WriteLine("Programs saved successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving programs: {ex.Message}");
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
            programName = name; // Changed to programName
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