#region

using System.Reactive;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Platform.Storage;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Controls;
using ReactiveUI;
using SFP.Models;
using SFP.Properties;
using SFP_UI.Views;
using Utils = SFP.Models.Windows.Utils;

// ReSharper disable MemberCanBePrivate.Global

#endregion

namespace SFP_UI.ViewModels;

public class SettingsPageViewModel : ViewModelBase
{
    #region App
    private bool _checkForUpdates;
    public bool CheckForUpdates
    {
        get => _checkForUpdates;
        set => this.RaiseAndSetIfChanged(ref _checkForUpdates, value);
    }

    private bool _showTrayIcon;
    public bool ShowTrayIcon
    {
        get => _showTrayIcon;
        set => this.RaiseAndSetIfChanged(ref _showTrayIcon, value);
    }

    private bool _minimizeToTray;
    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set => this.RaiseAndSetIfChanged(ref _minimizeToTray, value);
    }

    private bool _closeToTray;
    public bool CloseToTray
    {
        get => _closeToTray;
        set => this.RaiseAndSetIfChanged(ref _closeToTray, value);
    }

    private bool _startMinimized;
    public bool StartMinimized
    {
        get => _startMinimized;
        set => this.RaiseAndSetIfChanged(ref _startMinimized, value);
    }

    private bool _injectOnAppStart;
    public bool InjectOnAppStart
    {
        get => _injectOnAppStart;
        set => this.RaiseAndSetIfChanged(ref _injectOnAppStart, value);
    }

    private bool _runSteamOnStart;
    public bool RunSteamOnStart
    {
        get => _runSteamOnStart;
        set => this.RaiseAndSetIfChanged(ref _runSteamOnStart, value);
    }

    private bool _runOnBoot = Settings.Default.RunOnBoot;
    public bool RunOnBoot
    {
        get => _runOnBoot;
        set => this.RaiseAndSetIfChanged(ref _runOnBoot, value);
    }

    public IEnumerable<string> AppThemes { get; } = new[] { "Dark", "Light", "System Default" };
    private string _selectedTheme = null!;
    public string SelectedTheme
    {
        get => _selectedTheme;
        set => this.RaiseAndSetIfChanged(ref _selectedTheme, value);
    }
    #endregion

    #region Steam
    private string _steamDirectory = null!;
    public string SteamDirectory
    {
        get => _steamDirectory;
        set => this.RaiseAndSetIfChanged(ref _steamDirectory, value);
    }

    private string _steamLaunchArgs = null!;
    public string SteamLaunchArgs
    {
        get => _steamLaunchArgs;
        set => this.RaiseAndSetIfChanged(ref _steamLaunchArgs, value);
    }

    private bool _injectOnSteamStart;
    public bool InjectOnSteamStart
    {
        get => _injectOnSteamStart;
        set => this.RaiseAndSetIfChanged(ref _injectOnSteamStart, value);
    }

    private bool _forceSteamArgs;
    public bool ForceSteamArgs
    {
        get => _forceSteamArgs;
        set => this.RaiseAndSetIfChanged(ref _forceSteamArgs, value);
    }

    private bool _injectCss;
    public bool InjectCss
    {
        get => _injectCss;
        set => this.RaiseAndSetIfChanged(ref _injectCss, value);
    }

    private bool _injectJs;
    public bool InjectJs
    {
        get => _injectJs;
        set => this.RaiseAndSetIfChanged(ref _injectJs, value);
    }
    #endregion

    public bool IsWindows { get; } = OperatingSystem.IsWindows();
    public ReactiveCommand<Unit, Unit> BrowseSteamCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetSteamCommand { get; }
    public ReactiveCommand<Unit, Unit> InjectWarningAcceptCommand { get; }

    public SettingsPageViewModel()
    {
        #region App
        CheckForUpdates = Settings.Default.CheckForUpdates;
        this.WhenAnyValue(x => x.CheckForUpdates)
            .Subscribe(value =>
            {
                Settings.Default.CheckForUpdates = value;
                Settings.Default.Save();
            });

        ShowTrayIcon = Settings.Default.ShowTrayIcon;
        this.WhenAnyValue(x => x.ShowTrayIcon)
            .Subscribe(value =>
            {
                Settings.Default.ShowTrayIcon = value;
                Settings.Default.Save();
            });

        MinimizeToTray = Settings.Default.MinimizeToTray;
        this.WhenAnyValue(x => x.MinimizeToTray)
            .Subscribe(value =>
            {
                Settings.Default.MinimizeToTray = value;
                Settings.Default.Save();
            });

        CloseToTray = Settings.Default.CloseToTray;
        this.WhenAnyValue(x => x.CloseToTray)
            .Subscribe(value =>
            {
                Settings.Default.CloseToTray = value;
                Settings.Default.Save();
            });

        StartMinimized = Settings.Default.StartMinimized;
        this.WhenAnyValue(x => x.StartMinimized)
            .Subscribe(value =>
            {
                Settings.Default.StartMinimized = value;
                Settings.Default.Save();
            });

        InjectOnAppStart = Settings.Default.InjectOnAppStart;
        this.WhenAnyValue(x => x.InjectOnAppStart)
            .Subscribe(value =>
            {
                Settings.Default.InjectOnAppStart = value;
                Settings.Default.Save();
            });

        RunSteamOnStart = Settings.Default.RunSteamOnStart;
        this.WhenAnyValue(x => x.RunSteamOnStart)
            .Subscribe(value =>
            {
                Settings.Default.RunSteamOnStart = value;
                Settings.Default.Save();
            });

        RunOnBoot = Settings.Default.RunOnBoot;
        this.WhenAnyValue(x => x.RunOnBoot)
            .Subscribe(value =>
            {
                Settings.Default.RunOnBoot = value;
                Settings.Default.Save();
            });

        SelectedTheme = AppThemes.Contains(Settings.Default.AppTheme) ? Settings.Default.AppTheme : "System Default";
        this.WhenAnyValue(x => x.SelectedTheme)
            .Subscribe(value =>
            {
                Settings.Default.AppTheme = value.ToString();
                Settings.Default.Save();
                App.SetApplicationTheme(value);
            });
        #endregion

        #region Steam
        SteamDirectory = Settings.Default.SteamDirectory;
        this.WhenAnyValue(x => x.SteamDirectory)
            .Subscribe(value =>
            {
                Settings.Default.SteamDirectory = SteamDirectory;
                Settings.Default.Save();
            });

        SteamLaunchArgs = Settings.Default.SteamLaunchArgs;
        this.WhenAnyValue(x => x.SteamLaunchArgs)
            .Subscribe(value =>
            {
                Settings.Default.SteamLaunchArgs = SteamLaunchArgs;
                Settings.Default.Save();
            });

        InjectOnSteamStart = Settings.Default.InjectOnSteamStart;
        this.WhenAnyValue(x => x.InjectOnSteamStart)
            .Subscribe(value =>
            {
                Settings.Default.InjectOnSteamStart = value;
                Settings.Default.Save();
            });

        ForceSteamArgs = Settings.Default.ForceSteamArgs;
        this.WhenAnyValue(x => x.ForceSteamArgs)
            .Subscribe(value =>
            {
                Settings.Default.ForceSteamArgs = value;
                Settings.Default.Save();
            });

        InjectCss = Settings.Default.InjectCSS;
        this.WhenAnyValue(x => x.InjectCss)
            .Subscribe(value =>
            {
                Settings.Default.InjectCSS = value;
                Settings.Default.Save();
            });

        InjectJs = Settings.Default.InjectJS;
        this.WhenAnyValue(x => x.InjectJs)
            .Subscribe(value =>
            {
                Settings.Default.InjectJS = value;
                Settings.Default.Save();
            });
        #endregion

        BrowseSteamCommand = ReactiveCommand.CreateFromTask(OnBrowseSteamCommand);
        ResetSteamCommand = ReactiveCommand.CreateFromTask(OnResetSteamCommand);
        InjectWarningAcceptCommand = ReactiveCommand.CreateFromTask(OnInjectWarningAcceptCommand);
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
}
