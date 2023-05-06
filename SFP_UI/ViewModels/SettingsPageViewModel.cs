#region

using System.Reactive;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Platform.Storage;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Controls;
using ReactiveUI;
using SFP.Models;
using SFP_UI.Views;
using Settings = SFP.Properties.Settings;
using Utils = SFP.Models.Windows.Utils;

// ReSharper disable MemberCanBePrivate.Global

#endregion

namespace SFP_UI.ViewModels;

public class SettingsPageViewModel : ViewModelBase
{
    private string _appTheme = Settings.Default.AppTheme;
    private bool _checkForUpdates = Settings.Default.CheckForUpdates;
    private bool _closeToTray = Settings.Default.CloseToTray;
    private bool _forceSteamArgs = Settings.Default.ForceSteamArgs;
    private bool _injectOnAppStart = Settings.Default.InjectOnAppStart;
    private bool _injectOnSteamStart = Settings.Default.InjectOnSteamStart;
    private bool _minimizeToTray = Settings.Default.MinimizeToTray;
    private bool _runOnBoot = Settings.Default.RunOnBoot;
    private bool _runSteamOnStart = Settings.Default.RunSteamOnStart;
    private bool _showTrayIcon = Settings.Default.ShowTrayIcon;
    private bool _startMinimized = Settings.Default.StartMinimized;
    private string _steamDirectory = Steam.SteamDir ?? string.Empty;
    private string _steamLaunchArgs = Settings.Default.SteamLaunchArgs;
    private bool _injectCss = Settings.Default.InjectCSS;
    private bool _injectJs = Settings.Default.InjectJS;

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
        InjectWarningAcceptCommand = ReactiveCommand.CreateFromTask(OnInjectWarningAcceptCommand);
    }

    public bool IsWindows { get; } = OperatingSystem.IsWindows();

    public static SettingsPageViewModel? Instance { get; private set; }

    public ReactiveCommand<Unit, Unit> SaveCommand { get; } = ReactiveCommand.Create(OnSaveCommand);
    public ReactiveCommand<Unit, Unit> ReloadCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseSteamCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetSteamCommand { get; }
    public ReactiveCommand<Unit, Unit> InjectWarningAcceptCommand { get; }

    public string SteamDirectory
    {
        get => _steamDirectory;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref _steamDirectory, value);
            Settings.Default.SteamDirectory = value;
        }
    }

    public string SteamLaunchArgs
    {
        get => _steamLaunchArgs;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref _steamLaunchArgs, value);
            Settings.Default.SteamLaunchArgs = value;
        }
    }

    public bool RunSteamOnStart
    {
        get => _runSteamOnStart;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref _runSteamOnStart, value);
            Settings.Default.RunSteamOnStart = value;
        }
    }

    public bool CheckForUpdates
    {
        get => _checkForUpdates;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref _checkForUpdates, value);
            Settings.Default.CheckForUpdates = value;
        }
    }

    public bool ShowTrayIcon
    {
        get => _showTrayIcon;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref _showTrayIcon, value);
            Settings.Default.ShowTrayIcon = value;
        }
    }

    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref _minimizeToTray, value);
            Settings.Default.MinimizeToTray = value;
        }
    }

    public bool CloseToTray
    {
        get => _closeToTray;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref _closeToTray, value);
            Settings.Default.CloseToTray = value;
        }
    }

    public bool StartMinimized
    {
        get => _startMinimized;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref _startMinimized, value);
            Settings.Default.StartMinimized = value;
        }
    }

    public string AppTheme
    {
        get => _appTheme;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref _appTheme, value);
            Settings.Default.AppTheme = value;
            App.SetApplicationTheme(value);
        }
    }

    public bool InjectOnSteamStart
    {
        get => _injectOnSteamStart;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref _injectOnSteamStart, value);
            Settings.Default.InjectOnSteamStart = value;
        }
    }

    public bool RunOnBoot
    {
        get => _runOnBoot;
        set
        {
            if (_runOnBoot != value && OperatingSystem.IsWindows() && !Utils.SetAppRunOnLaunch(value))
            {
                return;
            }

            _ = this.RaiseAndSetIfChanged(ref _runOnBoot, value);
            Settings.Default.RunOnBoot = value;
        }
    }

    public bool InjectOnAppStart
    {
        get => _injectOnAppStart;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref _injectOnAppStart, value);
            Settings.Default.InjectOnAppStart = value;
        }
    }

    public bool ForceSteamArgs
    {
        get => _forceSteamArgs;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref _forceSteamArgs, value);
            Settings.Default.ForceSteamArgs = value;
        }
    }

    public bool InjectCss
    {
        get => _injectCss;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref _injectCss, value);
            Settings.Default.InjectCSS = value;
        }
    }

    public bool InjectJs
    {
        get => _injectJs;
        set
        {
            if (value && !Settings.Default.InjectJSWarningAccepted)
            {
                ShowWarningDialog();
                return;
            }
            _ = this.RaiseAndSetIfChanged(ref _injectJs, value);
            Settings.Default.InjectJS = value;
        }
    }

    private async void ShowWarningDialog()
    {
        var dialog = new ContentDialog
        {
            Title = "Warning",
            Content =
                "You are enabling JavaScript injection.\n" +
                "JavaScript can potentially contain malicious code and you should only use scripts from people you trust.\n" +
                "Continue?",
            PrimaryButtonText = "Yes",
            PrimaryButtonCommand = InjectWarningAcceptCommand,
            SecondaryButtonText = "No"
        };
        await dialog.ShowAsync();
    }

    public static void OnSaveCommand() => Settings.Default.Save();

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
        InjectOnSteamStart = Settings.Default.InjectOnSteamStart;
        InjectOnAppStart = Settings.Default.InjectOnAppStart;
        RunOnBoot = Settings.Default.RunOnBoot;
        RunSteamOnStart = Settings.Default.RunSteamOnStart;
        SteamLaunchArgs = Settings.Default.SteamLaunchArgs;
        ForceSteamArgs = Settings.Default.ForceSteamArgs;
        InjectCss = Settings.Default.InjectCSS;
        InjectJs = Settings.Default.InjectJS;
    }

    private async Task OnBrowseSteamCommand()
    {
        if (MainWindow.Instance != null)
        {
            var result =
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

    private Task OnInjectWarningAcceptCommand()
    {
        Settings.Default.InjectJSWarningAccepted = true;
        InjectJs = true;
        Settings.Default.Save();
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
