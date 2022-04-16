using Avalonia.Controls;

using ReactiveUI;

using SFP_UI.Views;

namespace SFP_UI.ViewModels
{
    public class SettingsPageViewModel : ViewModelBase
    {
        public static SettingsPageViewModel? Instance { get; private set; }

        public SettingsPageViewModel()
        {
            Instance = this;
        }

        private bool shouldPatchOnStart = SFP.Properties.Settings.Default.ShouldPatchOnStart;

        public bool ShouldPatchOnStart
        {
            get => shouldPatchOnStart;
            private set
            {
                this.RaiseAndSetIfChanged(ref shouldPatchOnStart, value);
                SFP.Properties.Settings.Default.ShouldPatchOnStart = value;
            }
        }

        private bool shouldPatchFriends = SFP.Properties.Settings.Default.ShouldPatchFriends;

        public bool ShouldPatchFriends
        {
            get => shouldPatchFriends;
            private set
            {
                this.RaiseAndSetIfChanged(ref shouldPatchFriends, value);
                SFP.Properties.Settings.Default.ShouldPatchFriends = value;
            }
        }

        private bool shouldPatchLibrary = SFP.Properties.Settings.Default.ShouldPatchLibrary;

        public bool ShouldPatchLibrary
        {
            get => shouldPatchLibrary;
            private set
            {
                this.RaiseAndSetIfChanged(ref shouldPatchLibrary, value);
                SFP.Properties.Settings.Default.ShouldPatchLibrary = value;
            }
        }

        private bool shouldScanFriends = SFP.Properties.Settings.Default.ShouldScanFriends;

        public bool ShouldScanFriends
        {
            get => shouldScanFriends;
            private set
            {
                this.RaiseAndSetIfChanged(ref shouldScanFriends, value);
                SFP.Properties.Settings.Default.ShouldScanFriends = value;
            }
        }

        private bool shouldScanLibrary = SFP.Properties.Settings.Default.ShouldScanLibrary;

        public bool ShouldScanLibrary
        {
            get => shouldScanLibrary;
            private set
            {
                this.RaiseAndSetIfChanged(ref shouldScanLibrary, value);
                SFP.Properties.Settings.Default.ShouldScanLibrary = value;
            }
        }

        private bool scanOnly = SFP.Properties.Settings.Default.ScanOnly;

        public bool ScanOnly
        {
            get => scanOnly;
            private set
            {
                this.RaiseAndSetIfChanged(ref scanOnly, value);
                SFP.Properties.Settings.Default.ScanOnly = value;
            }
        }

        private bool restartSteamOnPatch = SFP.Properties.Settings.Default.RestartSteamOnPatch;

        public bool RestartSteamOnPatch
        {
            get => restartSteamOnPatch;
            private set
            {
                this.RaiseAndSetIfChanged(ref restartSteamOnPatch, value);
                SFP.Properties.Settings.Default.RestartSteamOnPatch = value;
            }
        }

        private bool shouldScanOnStart = SFP.Properties.Settings.Default.ShouldScanOnStart;

        public bool ShouldScanOnStart
        {
            get => shouldScanOnStart;
            private set
            {
                this.RaiseAndSetIfChanged(ref shouldScanOnStart, value);
                SFP.Properties.Settings.Default.ShouldScanOnStart = value;
            }
        }

        private string steamLaunchArgs = SFP.Properties.Settings.Default.SteamLaunchArgs;

        public string SteamLaunchArgs
        {
            get => steamLaunchArgs;
            private set
            {
                this.RaiseAndSetIfChanged(ref steamLaunchArgs, value);
                SFP.Properties.Settings.Default.SteamLaunchArgs = value;
            }
        }

        private string steamDirectory = SFP.Properties.Settings.Default.SteamDirectory;

        public string SteamDirectory
        {
            get => steamDirectory;
            private set
            {
                this.RaiseAndSetIfChanged(ref steamDirectory, value);
                SFP.Properties.Settings.Default.SteamDirectory = value;
            }
        }

        private string cacheDirectory = SFP.Properties.Settings.Default.CacheDirectory;

        public string CacheDirectory
        {
            get => cacheDirectory;
            private set
            {
                this.RaiseAndSetIfChanged(ref cacheDirectory, value);
                SFP.Properties.Settings.Default.CacheDirectory = value;
            }
        }

        private bool checkForUpdates = SFP.Properties.Settings.Default.CheckForUpdates;

        public bool CheckForUpdates
        {
            get => checkForUpdates;
            private set
            {
                this.RaiseAndSetIfChanged(ref checkForUpdates, value);
                SFP.Properties.Settings.Default.CheckForUpdates = value;
            }
        }

        private bool showTrayIcon = SFP.Properties.Settings.Default.ShowTrayIcon;

        public bool ShowTrayIcon
        {
            get => showTrayIcon;
            private set
            {
                this.RaiseAndSetIfChanged(ref showTrayIcon, value);
                SFP.Properties.Settings.Default.ShowTrayIcon = value;
            }
        }

        private bool minimizeToTray = SFP.Properties.Settings.Default.MinimizeToTray;

        public bool MinimizeToTray
        {
            get => minimizeToTray;
            private set
            {
                this.RaiseAndSetIfChanged(ref minimizeToTray, value);
                SFP.Properties.Settings.Default.MinimizeToTray = value;
            }
        }

        private bool startMinimized = SFP.Properties.Settings.Default.StartMinimized;

        public bool StartMinimized
        {
            get => startMinimized;
            private set
            {
                this.RaiseAndSetIfChanged(ref startMinimized, value);
                SFP.Properties.Settings.Default.StartMinimized = value;
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

        public static void OnSaveCommand()
        {
            SFP.Properties.Settings.Default.Save();
        }

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
            SteamDirectory = SFP.Properties.Settings.Default.SteamDirectory;
            SteamLaunchArgs = SFP.Properties.Settings.Default.SteamLaunchArgs;
            CacheDirectory = SFP.Properties.Settings.Default.CacheDirectory;
        }

        public async void OnBrowseSteamCommand()
        {
            if (MainWindow.Instance != null)
            {
                var dialog = new OpenFolderDialog();
                var result = await dialog.ShowAsync(MainWindow.Instance);
                SteamDirectory = result ?? SteamDirectory;
            }
        }

        public async void OnBrowseCacheCommand()
        {
            if (MainWindow.Instance != null)
            {
                var dialog = new OpenFolderDialog();
                var result = await dialog.ShowAsync(MainWindow.Instance);
                CacheDirectory = result ?? CacheDirectory;
            }
        }
    }
}
