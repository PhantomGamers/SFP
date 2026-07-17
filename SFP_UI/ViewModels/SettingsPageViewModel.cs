#region

using System.Reactive.Linq;

using Avalonia.Platform.Storage;

using FluentAvalonia.UI.Controls;

using ReactiveUI.SourceGenerators;

using SFP.Models;
using SFP.Models.Injection;
using SFP.Properties;

using SFP_UI.Views;

using Utils = SFP.Models.Windows.Utils;

#endregion

namespace SFP_UI.ViewModels;

public partial class SettingsPageViewModel : ViewModelBase
{
    #region App
    [Reactive] public partial bool CheckForUpdates { get; set; }

    [Reactive] public partial bool ShowTrayIcon { get; set; }

    [Reactive] public partial bool MinimizeToTray { get; set; }

    [Reactive] public partial bool CloseToTray { get; set; }

    [Reactive] public partial bool StartMinimized { get; set; }

    [Reactive] public partial bool InjectOnAppStart { get; set; }

    [Reactive] public partial bool RunSteamOnStart { get; set; }

    [Reactive] public partial bool RunOnBoot { get; set; }

    public IEnumerable<string> AppThemes { get; } = ["Dark", "Light", "System Default"];
    [Reactive] public partial string SelectedTheme { get; set; } = null!;

    [Reactive] public partial int InitialInjectionDelay { get; set; }

    #endregion

    #region Steam
    [Reactive] public partial string SteamDirectory { get; set; } = null!;

    [Reactive] public partial string SteamLaunchArgs { get; set; } = null!;

    [Reactive] public partial short SteamCefPort { get; set; }

    [Reactive] public partial bool InjectOnSteamStart { get; set; }

    [Reactive] public partial bool ForceSteamArgs { get; set; }

    [Reactive] public partial bool InjectCss { get; set; }

    [Reactive] public partial bool InjectJs { get; set; }

    [Reactive] public partial bool UseAppTheme { get; set; }

    [Reactive] public partial bool DumpPages { get; set; }
    #endregion

    public bool IsWindows { get; } = OperatingSystem.IsWindows();
    public bool IsMacOs { get; } = OperatingSystem.IsMacOS();

    public SettingsPageViewModel()
    {
        InitProperties();
        #region App
        this.Changed
            .Where(e => e.PropertyName == nameof(CheckForUpdates))
            .Select(_ => CheckForUpdates)
            .DistinctUntilChanged()
            .Subscribe(value =>
            {
                Settings.Default.CheckForUpdates = value;
                Settings.Default.Save();
            });

        this.Changed
            .Where(e => e.PropertyName == nameof(ShowTrayIcon))
            .Select(_ => ShowTrayIcon)
            .DistinctUntilChanged()
            .Subscribe(value =>
            {
                App.SetIconsState(value);
                Settings.Default.ShowTrayIcon = value;
                Settings.Default.Save();
            });

        this.Changed
            .Where(e => e.PropertyName == nameof(MinimizeToTray))
            .Select(_ => MinimizeToTray)
            .DistinctUntilChanged()
            .Subscribe(value =>
            {
                Settings.Default.MinimizeToTray = value;
                Settings.Default.Save();
            });

        this.Changed
            .Where(e => e.PropertyName == nameof(CloseToTray))
            .Select(_ => CloseToTray)
            .DistinctUntilChanged()
            .Subscribe(value =>
            {
                Settings.Default.CloseToTray = value;
                Settings.Default.Save();
            });

        this.Changed
            .Where(e => e.PropertyName == nameof(StartMinimized))
            .Select(_ => StartMinimized)
            .DistinctUntilChanged()
            .Subscribe(value =>
            {
                Settings.Default.StartMinimized = value;
                Settings.Default.Save();
            });

        this.Changed
            .Where(e => e.PropertyName == nameof(InjectOnAppStart))
            .Select(_ => InjectOnAppStart)
            .DistinctUntilChanged()
            .Subscribe(value =>
            {
                Settings.Default.InjectOnAppStart = value;
                Settings.Default.Save();
            });

        this.Changed
            .Where(e => e.PropertyName == nameof(RunSteamOnStart))
            .Select(_ => RunSteamOnStart)
            .DistinctUntilChanged()
            .Subscribe(value =>
            {
                Settings.Default.RunSteamOnStart = value;
                Settings.Default.Save();
            });

        this.Changed
            .Where(e => e.PropertyName == nameof(RunOnBoot))
            .Select(_ => RunOnBoot)
            .DistinctUntilChanged()
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

        this.Changed
            .Where(e => e.PropertyName == nameof(SelectedTheme))
            .Select(_ => SelectedTheme)
            .DistinctUntilChanged()
            .Subscribe(value =>
            {
                Settings.Default.AppTheme = value.ToString();
                Settings.Default.Save();
                App.SetApplicationTheme(value);
            });

        this.Changed
            .Where(e => e.PropertyName == nameof(InitialInjectionDelay))
            .Select(_ => InitialInjectionDelay)
            .DistinctUntilChanged()
            .Subscribe(value =>
            {
                Settings.Default.InitialInjectionDelay = value;
                Settings.Default.Save();
            });
        #endregion

        #region Steam
        this.Changed
            .Where(e => e.PropertyName == nameof(SteamDirectory))
            .Select(_ => SteamDirectory)
            .DistinctUntilChanged()
            .Subscribe(value =>
            {
                Settings.Default.SteamDirectory = value;
                Settings.Default.Save();
            });

        this.Changed
            .Where(e => e.PropertyName == nameof(SteamLaunchArgs))
            .Select(_ => SteamLaunchArgs)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(value =>
            {
                Settings.Default.SteamLaunchArgs = value.Trim();
                Settings.Default.Save();
            });

        this.Changed
            .Where(e => e.PropertyName == nameof(SteamCefPort))
            .Select(_ => SteamCefPort)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(value =>
            {
                Settings.Default.SteamCefPort = value;
                Settings.Default.Save();
            });

        this.Changed
            .Where(e => e.PropertyName == nameof(InjectOnSteamStart))
            .Select(_ => InjectOnSteamStart)
            .DistinctUntilChanged()
            .Subscribe(value =>
            {
                Settings.Default.InjectOnSteamStart = value;
                Settings.Default.Save();
            });

        this.Changed
            .Where(e => e.PropertyName == nameof(ForceSteamArgs))
            .Select(_ => ForceSteamArgs)
            .DistinctUntilChanged()
            .Subscribe(value =>
            {
                Settings.Default.ForceSteamArgs = value;
                Settings.Default.Save();
            });

        this.Changed
            .Where(e => e.PropertyName == nameof(InjectCss))
            .Select(_ => InjectCss)
            .DistinctUntilChanged()
            .Subscribe(value =>
            {
                Settings.Default.InjectCSS = value;
                Settings.Default.Save();
            });

        this.Changed
            .Where(e => e.PropertyName == nameof(InjectJs))
            .Select(_ => InjectJs)
            .DistinctUntilChanged()
            .Subscribe(value =>
            {
                if (value && !Settings.Default.InjectJSWarningAccepted)
                {
                    Settings.Default.InjectJS = false;
                    Settings.Default.Save();
                    _ = ShowWarningDialog();
                }
                else
                {
                    Settings.Default.InjectJS = value;
                    Settings.Default.Save();
                }
            });

        this.Changed
            .Where(e => e.PropertyName == nameof(UseAppTheme))
            .Select(_ => UseAppTheme)
            .DistinctUntilChanged()
            .Subscribe(value =>
            {
                Settings.Default.UseAppTheme = value;
                Settings.Default.Save();
                _ = OnUseAppThemeChangedAsync(value);
            });

        this.Changed
            .Where(e => e.PropertyName == nameof(DumpPages))
            .Select(_ => DumpPages)
            .DistinctUntilChanged()
            .Subscribe(value =>
            {
                Settings.Default.DumpPages = value;
                Settings.Default.Save();
            });
        #endregion
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
        InitialInjectionDelay = Settings.Default.InitialInjectionDelay;
        #endregion

        #region Steam
        SteamDirectory = Steam.SteamDir ?? string.Empty;
        SteamLaunchArgs = Settings.Default.SteamLaunchArgs;
        SteamCefPort = Settings.Default.SteamCefPort;
        InjectOnSteamStart = Settings.Default.InjectOnSteamStart;
        ForceSteamArgs = Settings.Default.ForceSteamArgs;
        InjectCss = Settings.Default.InjectCSS;
        InjectJs = Settings.Default.InjectJS;
        UseAppTheme = Settings.Default.UseAppTheme;
        DumpPages = Settings.Default.DumpPages;
        #endregion

    }
    private async Task ShowWarningDialog()
    {
        var dialog = new ContentDialog
        {
            Title = "Warning",
            Content =
                "You are enabling JavaScript injection.\n" +
                "JavaScript can potentially contain malicious code and you should only use scripts from people you trust.\n" +
                "Continue?",
            PrimaryButtonText = "Yes",
            PrimaryButtonCommand = ConfirmInjectJsCommand,
            SecondaryButtonText = "No",
            SecondaryButtonCommand = CancelInjectJsCommand
        };
        await dialog.ShowAsync();
    }

    [ReactiveCommand]
    private async Task BrowseSteam() => await BrowseSteamImpl().ConfigureAwait(false);

    [ReactiveCommand]
    private void ResetSteam()
    {
        Settings.Default.SteamDirectory = string.Empty;
        SteamDirectory = Steam.SteamDir ?? string.Empty;
        Settings.Default.Save();
    }

    [ReactiveCommand]
    private void ResetSettings()
    {
        Settings.Default.Reset();
        InitProperties();
        Settings.Default.Save();
    }

    [ReactiveCommand]
    private void ConfirmInjectJs()
    {
        Settings.Default.InjectJS = true;
        Settings.Default.InjectJSWarningAccepted = true;
        Settings.Default.Save();
        InjectJs = true;
    }

    [ReactiveCommand]
    private void CancelInjectJs()
    {
        InjectJs = false;
        Settings.Default.Save();
    }

    private async Task BrowseSteamImpl()
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

    private static async Task OnUseAppThemeChangedAsync(bool useAppTheme)
    {
        try
        {
            if (!useAppTheme)
            {
                await Injector.UpdateColorScheme("light");
                await Injector.UpdateSystemAccentColors(false);
            }
            else
            {
                await Injector.UpdateColorScheme();
                await Injector.UpdateSystemAccentColors();
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error("Error updating colors on UseAppTheme changed");
            Log.Logger.Debug(ex);
        }
    }
}