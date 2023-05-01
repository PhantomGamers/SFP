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
    private string _appTheme = Settings.Default.AppTheme;
    private bool _checkForUpdates = Settings.Default.CheckForUpdates;
    private bool _closeToTray = Settings.Default.CloseToTray;
    private bool _minimizeToTray = Settings.Default.MinimizeToTray;
    private bool _showTrayIcon = Settings.Default.ShowTrayIcon;
    private bool _startMinimized = Settings.Default.StartMinimized;
    private string _steamDirectory = Steam.SteamDir ?? string.Empty;

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
                FluentAvaloniaTheme.HighContrastModeString => 2,
                _ => 3
            };
        }

        ReloadCommand = ReactiveCommand.Create(OnReloadCommand);
        BrowseSteamCommand = ReactiveCommand.CreateFromTask(OnBrowseSteamCommand);
        ResetSteamCommand = ReactiveCommand.CreateFromTask(OnResetSteamCommand);
    }

    public static SettingsPageViewModel? Instance { get; private set; }

    public ReactiveCommand<Unit, Unit> SaveCommand { get; } = ReactiveCommand.Create(OnSaveCommand);
    public ReactiveCommand<Unit, Unit> ReloadCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseSteamCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetSteamCommand { get; }

    public string SteamDirectory
    {
        get => _steamDirectory;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref _steamDirectory, value);
            Settings.Default.SteamDirectory = value;
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

    private string AppTheme
    {
        get => _appTheme;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref _appTheme, value);
            Settings.Default.AppTheme = value;
            App.SetApplicationTheme(value);
        }
    }

    public static void OnSaveCommand()
    {
        Log.Logger.Info("Settings saved.");
        Settings.Default.Save();
    }

    public void OnReloadCommand()
    {
        Settings.Default.Reload();
        SteamDirectory = Steam.SteamDir ?? string.Empty;
        AppTheme = Settings.Default.AppTheme;
        StartMinimized = Settings.Default.StartMinimized;
        MinimizeToTray = Settings.Default.MinimizeToTray;
        CloseToTray = Settings.Default.CloseToTray;
        CheckForUpdates = Settings.Default.CheckForUpdates;
        ShowTrayIcon = Settings.Default.ShowTrayIcon;
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

    private Task OnResetSteamCommand()
    {
        Settings.Default.SteamDirectory = string.Empty;
        SteamDirectory = Steam.SteamDir ?? string.Empty;
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
