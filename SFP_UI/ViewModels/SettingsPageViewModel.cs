#region

using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Platform.Storage;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
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
    [Reactive] public bool CheckForUpdates { get; set; }

    [Reactive] public bool ShowTrayIcon { get; set; }

    [Reactive] public bool MinimizeToTray { get; set; }

    [Reactive] public bool CloseToTray { get; set; }

    [Reactive] public bool StartMinimized { get; set; }

    [Reactive] public bool InjectOnAppStart { get; set; }

    [Reactive] public bool RunSteamOnStart { get; set; }

    [Reactive] public bool RunOnBoot { get; set; }

    public IEnumerable<string> AppThemes { get; } = new[] { "Dark", "Light", "System Default" };
    [Reactive] public string SelectedTheme { get; set; }
    #endregion

    #region Steam
    [Reactive] public string SteamDirectory { get; set; }

    [Reactive] public string SteamLaunchArgs { get; set; }

    [Reactive] public bool InjectOnSteamStart { get; set; }

    [Reactive] public bool ForceSteamArgs { get; set; }

    [Reactive] public bool InjectCss { get; set; }

    [Reactive] public bool InjectJs { get; set; }
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
            .Throttle(TimeSpan.FromSeconds(1))
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
