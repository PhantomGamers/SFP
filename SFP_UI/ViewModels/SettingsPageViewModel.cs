#region

using System.Reactive;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Platform.Storage;
using FluentAvalonia.Styling;
using ReactiveUI;
using SFP.Models;
using SFP_UI.Views;
using Settings = SFP.Properties.Settings;

#endregion

namespace SFP_UI.ViewModels;

public class SettingsPageViewModel : ViewModelBase
{
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

    private string _appTheme = Settings.Default.AppTheme;

    private string _cacheDirectory = Steam.CacheDir;

    private bool _checkForUpdates = Settings.Default.CheckForUpdates;

    private bool _closeToTray = Settings.Default.CloseToTray;

    private bool _minimizeToTray = Settings.Default.MinimizeToTray;

    private bool _restartSteamOnPatch = Settings.Default.RestartSteamOnPatch;

    private int _scannerDelay = Settings.Default.ScannerDelay;

    private bool _scanOnly = Settings.Default.ScanOnly;

    private bool _shouldPatchFriends = Settings.Default.ShouldPatchFriends;

    private bool _shouldPatchLibrary = Settings.Default.ShouldPatchLibrary;

    private bool _shouldPatchOnStart = Settings.Default.ShouldPatchOnStart;

    private bool _shouldPatchResources = Settings.Default.ShouldPatchResources;

    private bool _shouldScanFriends = Settings.Default.ShouldScanFriends;

    private bool _shouldScanLibrary = Settings.Default.ShouldScanLibrary;

    private bool _shouldScanOnStart = Settings.Default.ShouldScanOnStart;

    private bool _shouldScanResources = Settings.Default.ShouldScanResources;

    private bool _showTrayIcon = Settings.Default.ShowTrayIcon;

    private bool _startMinimized = Settings.Default.StartMinimized;

    private string _steamDirectory = Steam.SteamDir ?? string.Empty;

    private string _steamLaunchArgs = Settings.Default.SteamLaunchArgs;

    public SettingsPageViewModel(SelectingItemsControl? appThemeComboBox)
    {
        Instance = this;
        if (appThemeComboBox != null)
        {
            appThemeComboBox.SelectionChanged += OnAppThemeSelectedChanged;
            appThemeComboBox.SelectedIndex = Settings.Default.AppTheme switch
            {
                FluentAvaloniaTheme.DarkModeString => 0,
                FluentAvaloniaTheme.LightModeString => 1,
                _ => 2
            };
        }

        BrowseCacheCommand = ReactiveCommand.CreateFromTask(OnBrowseCacheCommand);
        ReloadCommand = ReactiveCommand.CreateFromTask(OnReloadCommand);
        BrowseSteamCommand = ReactiveCommand.CreateFromTask(OnBrowseSteamCommand);
        ResetSteamCommand = ReactiveCommand.CreateFromTask(OnResetSteamCommand);
        ResetCacheCommand = ReactiveCommand.CreateFromTask(OnResetCacheCommand);
    }

    public static SettingsPageViewModel? Instance { get; private set; }

    public ReactiveCommand<Unit, Unit> SaveCommand { get; } = ReactiveCommand.CreateFromTask(OnSaveCommand);
    public ReactiveCommand<Unit, Unit> ReloadCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseSteamCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseCacheCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetSteamCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetCacheCommand { get; }

    public bool ShouldPatchOnStart
    {
        get => _shouldPatchOnStart;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _shouldPatchOnStart, value);
            Settings.Default.ShouldPatchOnStart = value;
        }
    }

    public bool ShouldPatchFriends
    {
        get => _shouldPatchFriends;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _shouldPatchFriends, value);
            Settings.Default.ShouldPatchFriends = value;
        }
    }

    public bool ShouldPatchLibrary
    {
        get => _shouldPatchLibrary;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _shouldPatchLibrary, value);
            Settings.Default.ShouldPatchLibrary = value;
        }
    }

    public bool ShouldPatchResources
    {
        get => _shouldPatchResources;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _shouldPatchResources, value);
            Settings.Default.ShouldPatchResources = value;
        }
    }

    public bool ShouldScanFriends
    {
        get => _shouldScanFriends;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _shouldScanFriends, value);
            Settings.Default.ShouldScanFriends = value;
        }
    }

    public bool ShouldScanLibrary
    {
        get => _shouldScanLibrary;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _shouldScanLibrary, value);
            Settings.Default.ShouldScanLibrary = value;
        }
    }

    public bool ShouldScanResources
    {
        get => _shouldScanResources;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _shouldScanResources, value);
            Settings.Default.ShouldScanResources = value;
        }
    }

    public bool ScanOnly
    {
        get => _scanOnly;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _scanOnly, value);
            Settings.Default.ScanOnly = value;
        }
    }

    public bool RestartSteamOnPatch
    {
        get => _restartSteamOnPatch;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _restartSteamOnPatch, value);
            Settings.Default.RestartSteamOnPatch = value;
        }
    }

    public bool ShouldScanOnStart
    {
        get => _shouldScanOnStart;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _shouldScanOnStart, value);
            Settings.Default.ShouldScanOnStart = value;
        }
    }

    public string SteamLaunchArgs
    {
        get => _steamLaunchArgs;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _steamLaunchArgs, value);
            Settings.Default.SteamLaunchArgs = value;
        }
    }

    public string SteamDirectory
    {
        get => _steamDirectory;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _steamDirectory, value);
            Settings.Default.SteamDirectory = value;
        }
    }

    public string CacheDirectory
    {
        get => _cacheDirectory;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _cacheDirectory, value);
            Settings.Default.CacheDirectory = value;
        }
    }

    public bool CheckForUpdates
    {
        get => _checkForUpdates;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _checkForUpdates, value);
            Settings.Default.CheckForUpdates = value;
        }
    }

    public bool ShowTrayIcon
    {
        get => _showTrayIcon;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _showTrayIcon, value);
            Settings.Default.ShowTrayIcon = value;
        }
    }

    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _minimizeToTray, value);
            Settings.Default.MinimizeToTray = value;
        }
    }

    public bool CloseToTray
    {
        get => _closeToTray;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _closeToTray, value);
            Settings.Default.CloseToTray = value;
        }
    }

    public bool StartMinimized
    {
        get => _startMinimized;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _startMinimized, value);
            Settings.Default.StartMinimized = value;
        }
    }

    public int ScannerDelay
    {
        get => _scannerDelay;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _scannerDelay, value);
            Settings.Default.ScannerDelay = value;
        }
    }

    public string AppTheme
    {
        get => _appTheme;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _appTheme, value);
            Settings.Default.AppTheme = value;
        }
    }

    public static Task OnSaveCommand()
    {
        Settings.Default.Save();
        return Task.CompletedTask;
    }

    public Task OnReloadCommand()
    {
        Settings.Default.Reload();
        ShouldPatchOnStart = Settings.Default.ShouldPatchOnStart;
        ShouldPatchFriends = Settings.Default.ShouldPatchFriends;
        ShouldPatchLibrary = Settings.Default.ShouldPatchLibrary;
        ShouldScanFriends = Settings.Default.ShouldScanFriends;
        ShouldScanLibrary = Settings.Default.ShouldScanLibrary;
        ScanOnly = Settings.Default.ScanOnly;
        RestartSteamOnPatch = Settings.Default.RestartSteamOnPatch;
        ShouldScanOnStart = Settings.Default.ShouldScanOnStart;
        SteamDirectory = Steam.SteamDir ?? string.Empty;
        SteamLaunchArgs = Settings.Default.SteamLaunchArgs;
        CacheDirectory = Steam.CacheDir;
        ScannerDelay = Settings.Default.ScannerDelay;
        AppTheme = Settings.Default.AppTheme;
        StartMinimized = Settings.Default.StartMinimized;
        MinimizeToTray = Settings.Default.MinimizeToTray;
        CloseToTray = Settings.Default.CloseToTray;
        CheckForUpdates = Settings.Default.CheckForUpdates;
        ShowTrayIcon = Settings.Default.ShowTrayIcon;
        ShouldPatchResources = Settings.Default.ShouldPatchResources;
        ShouldScanResources = Settings.Default.ShouldScanResources;
        return Task.CompletedTask;
    }

    private async Task OnBrowseSteamCommand()
    {
        if (MainWindow.Instance != null)
        {
            IReadOnlyList<IStorageFolder> result =
                await MainWindow.Instance.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
            if (result.Count > 0)
            {
                SteamDirectory = result[0].Path.LocalPath;
            }
        }
    }

    private async Task OnBrowseCacheCommand()
    {
        if (MainWindow.Instance != null)
        {
            IReadOnlyList<IStorageFolder> result =
                await MainWindow.Instance.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
            if (result.Count > 0)
            {
                CacheDirectory = result[0].Path.LocalPath;
            }
        }
    }

    private Task OnResetSteamCommand()
    {
        Settings.Default.SteamDirectory = string.Empty;
        SteamDirectory = Steam.SteamDir ?? string.Empty;
        return Task.CompletedTask;
    }

    private Task OnResetCacheCommand()
    {
        Settings.Default.CacheDirectory = string.Empty;
        CacheDirectory = Steam.CacheDir;
        return Task.CompletedTask;
    }

    private void OnAppThemeSelectedChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedItem: ComboBoxItem cbi })
        {
            AppTheme = (string?)cbi.Content ?? AppTheme;
        }
    }
}
