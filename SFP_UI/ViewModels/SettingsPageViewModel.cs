#region

using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SFP.Models;
using SFP.Models.Injection;
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
    [Reactive] public string SelectedTheme { get; set; } = null!;

    #endregion

    #region Steam
    [Reactive] public string SteamDirectory { get; set; } = null!;

    [Reactive] public string SteamLaunchArgs { get; set; } = null!;

    [Reactive] public bool InjectOnSteamStart { get; set; }

    [Reactive] public bool ForceSteamArgs { get; set; }

    [Reactive] public bool InjectCss { get; set; }

    [Reactive] public bool InjectJs { get; set; }

    [Reactive] public bool UseAppTheme { get; set; }

    [Reactive] public bool DumpPages { get; set; }
    #endregion

    public bool IsWindows { get; } = OperatingSystem.IsWindows();

    public SettingsPageViewModel()
    {
        InitProperties();
        #region App
        this.WhenAnyValue(x => x.CheckForUpdates)
            .Subscribe(value =>
            {
                Settings.Default.CheckForUpdates = value;
                Settings.Default.Save();
            });

        this.WhenAnyValue(x => x.ShowTrayIcon)
            .Subscribe(value =>
            {
                App.SetIconsState(value);
                Settings.Default.ShowTrayIcon = value;
                Settings.Default.Save();
            });

        this.WhenAnyValue(x => x.MinimizeToTray)
            .Subscribe(value =>
            {
                Settings.Default.MinimizeToTray = value;
                Settings.Default.Save();
            });

        this.WhenAnyValue(x => x.CloseToTray)
            .Subscribe(value =>
            {
                Settings.Default.CloseToTray = value;
                Settings.Default.Save();
            });

        this.WhenAnyValue(x => x.StartMinimized)
            .Subscribe(value =>
            {
                Settings.Default.StartMinimized = value;
                Settings.Default.Save();
            });

        this.WhenAnyValue(x => x.InjectOnAppStart)
            .Subscribe(value =>
            {
                Settings.Default.InjectOnAppStart = value;
                Settings.Default.Save();
            });

        this.WhenAnyValue(x => x.RunSteamOnStart)
            .Subscribe(value =>
            {
                Settings.Default.RunSteamOnStart = value;
                Settings.Default.Save();
            });

        this.WhenAnyValue(x => x.RunOnBoot)
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(value =>
            {
                Settings.Default.RunOnBoot = value;
                Settings.Default.Save();
                if (OperatingSystem.IsWindows())
                {
                    Utils.SetAppRunOnLaunch(value);
                }
            });

        this.WhenAnyValue(x => x.SelectedTheme)
            .Subscribe(value =>
            {
                Settings.Default.AppTheme = value.ToString();
                Settings.Default.Save();
                App.SetApplicationTheme(value);
            });
        #endregion

        #region Steam
        this.WhenAnyValue(x => x.SteamDirectory)
            .Subscribe(value =>
            {
                Settings.Default.SteamDirectory = value;
                Settings.Default.Save();
            });

        this.WhenAnyValue(x => x.SteamLaunchArgs)
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(value =>
            {
                Settings.Default.SteamLaunchArgs = value.Trim();
                Settings.Default.Save();
            });

        this.WhenAnyValue(x => x.InjectOnSteamStart)
            .Subscribe(value =>
            {
                Settings.Default.InjectOnSteamStart = value;
                Settings.Default.Save();
            });

        this.WhenAnyValue(x => x.ForceSteamArgs)
            .Subscribe(value =>
            {
                Settings.Default.ForceSteamArgs = value;
                Settings.Default.Save();
            });

        this.WhenAnyValue(x => x.InjectCss)
            .Subscribe(value =>
            {
                Settings.Default.InjectCSS = value;
                Settings.Default.Save();
            });

        this.WhenAnyValue(x => x.InjectJs)
            .Subscribe(value =>
            {
                if (value && !Settings.Default.InjectJSWarningAccepted)
                {
                    Settings.Default.InjectJS = false;
                    Settings.Default.Save();
                    ShowWarningDialog();
                }
                else
                {
                    Settings.Default.InjectJS = value;
                    Settings.Default.Save();
                }
            });

        this.WhenAnyValue(x => x.UseAppTheme)
            .Subscribe(value =>
            {
                Settings.Default.UseAppTheme = value;
                Settings.Default.Save();
                if (!value)
                {
                    Injector.UpdateColorScheme("light");
                    Injector.UpdateSystemAccentColors(false);
                }
                else
                {
                    Injector.UpdateColorScheme();
                    Injector.UpdateSystemAccentColors();
                }
            });

        this.WhenAnyValue(x => x.DumpPages)
            .Subscribe(value =>
            {
                Settings.Default.DumpPages = value;
                Settings.Default.Save();
            });
        #endregion

        BrowseSteam = ReactiveCommand.Create(BrowseSteamImpl);
        ResetSteam = ReactiveCommand.Create(() =>
        {
            Settings.Default.SteamDirectory = string.Empty;
            SteamDirectory = Steam.SteamDir ?? string.Empty;
            Settings.Default.Save();
        });
        ResetSettings = ReactiveCommand.Create(() =>
        {
            Settings.Default.Reset();
            InitProperties();
            Settings.Default.Save();
        });
    }

    private void InitProperties()
    {
        #region App
        CheckForUpdates = Settings.Default.CheckForUpdates;
        ShowTrayIcon = Settings.Default.ShowTrayIcon;
        MinimizeToTray = Settings.Default.MinimizeToTray;
        CloseToTray = Settings.Default.CloseToTray;
        StartMinimized = Settings.Default.StartMinimized;
        InjectOnAppStart = Settings.Default.InjectOnAppStart;
        RunSteamOnStart = Settings.Default.RunSteamOnStart;
        RunOnBoot = Settings.Default.RunOnBoot;
        SelectedTheme = AppThemes.Contains(Settings.Default.AppTheme) ? Settings.Default.AppTheme : "System Default";
        #endregion

        #region Steam
        SteamDirectory = Steam.SteamDir ?? string.Empty;
        SteamLaunchArgs = Settings.Default.SteamLaunchArgs;
        InjectOnSteamStart = Settings.Default.InjectOnSteamStart;
        ForceSteamArgs = Settings.Default.ForceSteamArgs;
        InjectCss = Settings.Default.InjectCSS;
        InjectJs = Settings.Default.InjectJS;
        UseAppTheme = Settings.Default.UseAppTheme;
        DumpPages = Settings.Default.DumpPages;
        #endregion

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
            PrimaryButtonCommand = ReactiveCommand.Create(() =>
            {
                Settings.Default.InjectJS = true;
                Settings.Default.InjectJSWarningAccepted = true;
                Settings.Default.Save();
                InjectJs = true;
            }),
            SecondaryButtonText = "No",
            SecondaryButtonCommand = ReactiveCommand.Create(() =>
            {
                InjectJs = false;
                Settings.Default.Save();
            })
        };
        await dialog.ShowAsync();
    }

    public ReactiveCommand<Unit, Unit> ResetSteam { get; }
    public ReactiveCommand<Unit, Unit> ResetSettings { get; }
    public ReactiveCommand<Unit, Unit> BrowseSteam { get; }

    private async void BrowseSteamImpl()
    {
        if (MainWindow.Instance == null)
        {
            return;
        }
        var result =
            await MainWindow.Instance.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
        if (result.Count > 0)
        {
            SteamDirectory = result[0].Path.LocalPath;
        }
    }
}
