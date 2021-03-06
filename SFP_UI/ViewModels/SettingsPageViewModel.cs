using Avalonia.Controls;

using ReactiveUI;

using SFP.Models;

using SFP_UI.Views;

namespace SFP_UI.ViewModels
{
    public class SettingsPageViewModel : ViewModelBase
    {
        public static SettingsPageViewModel? Instance { get; private set; }

        public SettingsPageViewModel(ComboBox appThemeComboBox)
        {
            Instance = this;
            appThemeComboBox.SelectionChanged += OnAppThemeSelectedChanged;
            appThemeComboBox.SelectedIndex = SFP.Properties.Settings.Default.AppTheme == FluentAvalonia.Styling.FluentAvaloniaTheme.DarkModeString
                ? 0
                : SFP.Properties.Settings.Default.AppTheme == FluentAvalonia.Styling.FluentAvaloniaTheme.LightModeString ? 1 : 2;
        }

        private bool _shouldPatchOnStart = SFP.Properties.Settings.Default.ShouldPatchOnStart;

        public bool ShouldPatchOnStart
        {
            get => _shouldPatchOnStart;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _shouldPatchOnStart, value);
                SFP.Properties.Settings.Default.ShouldPatchOnStart = value;
            }
        }

        private bool _shouldPatchFriends = SFP.Properties.Settings.Default.ShouldPatchFriends;

        public bool ShouldPatchFriends
        {
            get => _shouldPatchFriends;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _shouldPatchFriends, value);
                SFP.Properties.Settings.Default.ShouldPatchFriends = value;
            }
        }

        private bool _shouldPatchLibrary = SFP.Properties.Settings.Default.ShouldPatchLibrary;

        public bool ShouldPatchLibrary
        {
            get => _shouldPatchLibrary;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _shouldPatchLibrary, value);
                SFP.Properties.Settings.Default.ShouldPatchLibrary = value;
            }
        }

        private bool _shouldPatchResources = SFP.Properties.Settings.Default.ShouldPatchResources;

        public bool ShouldPatchResources
        {
            get => _shouldPatchResources;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _shouldPatchResources, value);
                SFP.Properties.Settings.Default.ShouldPatchResources = value;
            }
        }

        private bool _shouldScanFriends = SFP.Properties.Settings.Default.ShouldScanFriends;

        public bool ShouldScanFriends
        {
            get => _shouldScanFriends;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _shouldScanFriends, value);
                SFP.Properties.Settings.Default.ShouldScanFriends = value;
            }
        }

        private bool _shouldScanLibrary = SFP.Properties.Settings.Default.ShouldScanLibrary;

        public bool ShouldScanLibrary
        {
            get => _shouldScanLibrary;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _shouldScanLibrary, value);
                SFP.Properties.Settings.Default.ShouldScanLibrary = value;
            }
        }

        private bool _shouldScanResources = SFP.Properties.Settings.Default.ShouldScanResources;

        public bool ShouldScanResources
        {
            get => _shouldScanResources;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _shouldScanResources, value);
                SFP.Properties.Settings.Default.ShouldScanResources = value;
            }
        }

        private bool _scanOnly = SFP.Properties.Settings.Default.ScanOnly;

        public bool ScanOnly
        {
            get => _scanOnly;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _scanOnly, value);
                SFP.Properties.Settings.Default.ScanOnly = value;
            }
        }

        private bool _restartSteamOnPatch = SFP.Properties.Settings.Default.RestartSteamOnPatch;

        public bool RestartSteamOnPatch
        {
            get => _restartSteamOnPatch;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _restartSteamOnPatch, value);
                SFP.Properties.Settings.Default.RestartSteamOnPatch = value;
            }
        }

        private bool _shouldScanOnStart = SFP.Properties.Settings.Default.ShouldScanOnStart;

        public bool ShouldScanOnStart
        {
            get => _shouldScanOnStart;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _shouldScanOnStart, value);
                SFP.Properties.Settings.Default.ShouldScanOnStart = value;
            }
        }

        private string _steamLaunchArgs = SFP.Properties.Settings.Default.SteamLaunchArgs;

        public string SteamLaunchArgs
        {
            get => _steamLaunchArgs;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _steamLaunchArgs, value);
                SFP.Properties.Settings.Default.SteamLaunchArgs = value;
            }
        }

        private string _steamDirectory = Steam.SteamDir ?? string.Empty;

        public string SteamDirectory
        {
            get => _steamDirectory;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _steamDirectory, value);
                SFP.Properties.Settings.Default.SteamDirectory = value;
            }
        }

        private string _cacheDirectory = Steam.CacheDir;

        public string CacheDirectory
        {
            get => _cacheDirectory;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _cacheDirectory, value);
                SFP.Properties.Settings.Default.CacheDirectory = value;
            }
        }

        private bool _checkForUpdates = SFP.Properties.Settings.Default.CheckForUpdates;

        public bool CheckForUpdates
        {
            get => _checkForUpdates;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _checkForUpdates, value);
                SFP.Properties.Settings.Default.CheckForUpdates = value;
            }
        }

        private bool _showTrayIcon = SFP.Properties.Settings.Default.ShowTrayIcon;

        public bool ShowTrayIcon
        {
            get => _showTrayIcon;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _showTrayIcon, value);
                SFP.Properties.Settings.Default.ShowTrayIcon = value;
            }
        }

        private bool _minimizeToTray = SFP.Properties.Settings.Default.MinimizeToTray;

        public bool MinimizeToTray
        {
            get => _minimizeToTray;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _minimizeToTray, value);
                SFP.Properties.Settings.Default.MinimizeToTray = value;
            }
        }

        private bool _closeToTray = SFP.Properties.Settings.Default.CloseToTray;

        public bool CloseToTray
        {
            get => _closeToTray;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _closeToTray, value);
                SFP.Properties.Settings.Default.CloseToTray = value;
            }
        }

        private bool _startMinimized = SFP.Properties.Settings.Default.StartMinimized;

        public bool StartMinimized
        {
            get => _startMinimized;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _startMinimized, value);
                SFP.Properties.Settings.Default.StartMinimized = value;
            }
        }

        private int _scannerDelay = SFP.Properties.Settings.Default.ScannerDelay;

        public int ScannerDelay
        {
            get => _scannerDelay;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _scannerDelay, value);
                SFP.Properties.Settings.Default.ScannerDelay = value;
            }
        }

        /*
        private bool startWithOS = SFP.Properties.Settings.Default.StartWithOS;

        public bool StartWithOS
        {
            get => startMinimized;
            private set
            {
                this.RaiseAndSetIfChanged(ref startWithOS, value);
                SFP.Properties.Settings.Default.StartWithOS = value;
            }
        }
        */

        private string _appTheme = SFP.Properties.Settings.Default.AppTheme;

        public string AppTheme
        {
            get => _appTheme;
            private set
            {
                _ = this.RaiseAndSetIfChanged(ref _appTheme, value);
                SFP.Properties.Settings.Default.AppTheme = value;
                if (value == "System Default")
                {
                    MainWindow.Instance?.Theme?.InvalidateThemingFromSystemThemeChanged();
                }
                else
                {
                    MainWindow.Instance!.Theme!.RequestedTheme = value;
                }
            }
        }

        public static void OnSaveCommand() => SFP.Properties.Settings.Default.Save();

        public void OnReloadCommand()
        {
            SFP.Properties.Settings.Default.Reload();
            ShouldPatchOnStart = SFP.Properties.Settings.Default.ShouldPatchOnStart;
            ShouldPatchFriends = SFP.Properties.Settings.Default.ShouldPatchFriends;
            ShouldPatchLibrary = SFP.Properties.Settings.Default.ShouldPatchLibrary;
            ShouldScanFriends = SFP.Properties.Settings.Default.ShouldScanFriends;
            ShouldScanLibrary = SFP.Properties.Settings.Default.ShouldScanLibrary;
            ScanOnly = SFP.Properties.Settings.Default.ScanOnly;
            RestartSteamOnPatch = SFP.Properties.Settings.Default.RestartSteamOnPatch;
            ShouldScanOnStart = SFP.Properties.Settings.Default.ShouldScanOnStart;
            SteamDirectory = Steam.SteamDir ?? string.Empty;
            SteamLaunchArgs = SFP.Properties.Settings.Default.SteamLaunchArgs;
            CacheDirectory = Steam.CacheDir;
            ScannerDelay = SFP.Properties.Settings.Default.ScannerDelay;
            AppTheme = SFP.Properties.Settings.Default.AppTheme;
            StartMinimized = SFP.Properties.Settings.Default.StartMinimized;
            MinimizeToTray = SFP.Properties.Settings.Default.MinimizeToTray;
            CloseToTray = SFP.Properties.Settings.Default.CloseToTray;
            CheckForUpdates = SFP.Properties.Settings.Default.CheckForUpdates;
            ShowTrayIcon = SFP.Properties.Settings.Default.ShowTrayIcon;
        }

        public async void OnBrowseSteamCommand()
        {
            if (MainWindow.Instance != null)
            {
                var dialog = new OpenFolderDialog();
                string? result = await dialog.ShowAsync(MainWindow.Instance);
                SteamDirectory = result ?? SteamDirectory;
            }
        }

        public async void OnBrowseCacheCommand()
        {
            if (MainWindow.Instance != null)
            {
                var dialog = new OpenFolderDialog();
                string? result = await dialog.ShowAsync(MainWindow.Instance);
                CacheDirectory = result ?? CacheDirectory;
            }
        }

        public void OnResetSteamCommand()
        {
            SFP.Properties.Settings.Default.SteamDirectory = string.Empty;
            SteamDirectory = Steam.SteamDir ?? string.Empty;
        }

        public void OnResetCacheCommand()
        {
            SFP.Properties.Settings.Default.CacheDirectory = string.Empty;
            CacheDirectory = Steam.CacheDir;
        }

        public void OnAppThemeSelectedChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem cbi)
            {
                AppTheme = (string)cbi.Content;
            }
        }
    }
}
