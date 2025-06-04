using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using XeryonMotionGUI.Contracts.Services;
using XeryonMotionGUI.Helpers;

namespace XeryonMotionGUI.ViewModels
{
    public partial class SettingsViewModel : ObservableRecipient
    {
        // --- file-based storage ---
        private class UserSettings
        {
            public string UserLevel { get; set; } = "Normal";
        }

        private readonly string _settingsFilePath;
        private UserSettings _settings;

        // --- existing deps ---
        private readonly ShellViewModel _shellViewModel;
        private readonly IThemeSelectorService _themeSelectorService;

        public SettingsViewModel(
            IThemeSelectorService themeSelectorService,
            ShellViewModel shellViewModel)
        {
            _shellViewModel = shellViewModel;
            _themeSelectorService = themeSelectorService;

            // prepare storage path under %LocalAppData%\XeryonMotionGUI\
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "XeryonMotionGUI");
            Directory.CreateDirectory(folder);
            _settingsFilePath = Path.Combine(folder, "userSettings.json");

            // theme stuff
            _elementTheme = _themeSelectorService.Theme;
            _versionDescription = GetVersionDescription();
            SwitchThemeCommand = new RelayCommand<ElementTheme>(async p =>
            {
                if (ElementTheme != p)
                {
                    ElementTheme = p;
                    await _themeSelectorService.SetThemeAsync(p);
                }
            });

            // load last‐saved mode (or default)
            LoadUserLevel();
        }

        // --- existing properties ---

        public SolidColorBrush ShellPageBackground
        {
            get => _shellViewModel.ShellPageBackground;
            set => _shellViewModel.ShellPageBackground = value;
        }

        [ObservableProperty]
        private ElementTheme _elementTheme;

        [ObservableProperty]
        private string _versionDescription;

        public ICommand SwitchThemeCommand
        {
            get;
        }

        // --- new mode flags ---

        [ObservableProperty]
        private bool _isNormalMode;

        [ObservableProperty]
        private bool _isExpertMode;

        partial void OnIsNormalModeChanged(bool isNow)
        {
            if (isNow)
            {
                // enforce mutual exclusion
                _isExpertMode = false;
                SaveUserLevel("Normal");
            }
        }

        partial void OnIsExpertModeChanged(bool isNow)
        {
            if (isNow)
            {
                _isNormalMode = false;
                SaveUserLevel("Expert");
            }
        }

        // --- load & save JSON file ---

        private void LoadUserLevel()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                    _settings = JsonSerializer.Deserialize<UserSettings>(
                        File.ReadAllText(_settingsFilePath))!;
                else
                    _settings = new UserSettings();
            }
            catch
            {
                _settings = new UserSettings();
            }

            // apply into our flags
            if (_settings.UserLevel == "Expert")
            {
                _isExpertMode = true;
                _isNormalMode = false;
            }
            else
            {
                _isNormalMode = true;
                _isExpertMode = false;
            }
        }

        private void SaveUserLevel(string level)
        {
            _settings.UserLevel = level;
            File.WriteAllText(
                _settingsFilePath,
                JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true }));
        }

        // --- version helper (unchanged) ---

        private static string GetVersionDescription()
        {
            Version version;
            if (RuntimeHelper.IsMSIX)
            {
                var v = Windows.ApplicationModel.Package.Current.Id.Version;
                version = new(v.Major, v.Minor, v.Build, v.Revision);
            }
            else
            {
                version = Assembly.GetExecutingAssembly().GetName().Version!;
            }
            return $"{"AppDisplayName".GetLocalized()} - "
                   + $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }
}
