#region

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;

using FluentAvalonia.Styling;

using JetBrains.Annotations;

using SFP.Models;
using SFP.Models.Injection;
using SFP.Properties;

using SFP_UI.Models;
using SFP_UI.ViewModels;
using SFP_UI.Views;

#endregion

namespace SFP_UI;

public class App : Application
{
    public App()
    {
        DataContext = new AppViewModel();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

#pragma warning disable EPC27
    public override async void OnFrameworkInitializationCompleted()
#pragma warning restore EPC27
    {
        try
        {
            if (!Settings.Default.StartMinimized || !Settings.Default.MinimizeToTray)
            {
                StartMainWindow();
            }

            base.OnFrameworkInitializationCompleted();

            if (Design.IsDesignMode)
            {
                return;
            }

            if (OperatingSystem.IsMacOS())
            {
                SetIconsState(true);
            }
            else
            {
                SetIconsState(Settings.Default.ShowTrayIcon);
            }

            Injector.SetColorScheme(ActualThemeVariant.ToString());
            Injector.SetAccentColors(GetColorValues());
            ActualThemeVariantChanged += (_, _) => _ = OnActualThemeVariantChangedAsync();
            if (Current?.PlatformSettings != null)
            {
                Current.PlatformSettings.ColorValuesChanged += (_, _) => _ = OnColorValuesChangedAsync();
            }
            else
            {
                Log.Logger.Warn("PlatformSettings is null, can't update system accent colors");
            }

            await HandleStartupTasks();

            if (Settings.Default.CheckForUpdates)
            {
                Dispatcher.UIThread.Post(() => _ = UpdateChecker.CheckForUpdates());
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error("Error in OnFrameworkInitializationCompleted event handler");
            Log.Logger.Debug(ex);
        }
    }

    private static string[] GetColorValues()
    {
        if (Current!.Styles[0] is not FluentAvaloniaTheme faTheme)
        {
            Log.Logger.Warn("Could not get color values, FluentAvaloniaTheme is null");
            return [];
        }
        var colorValues = new string[7];
        for (var i = 0; i < 7; i++)
        {
            if (!faTheme.Resources.TryGetResource(Injector.ColorNames[i], null, out var c))
            {
                Log.Logger.Warn("Could not get color value for {ColorName}", Injector.ColorNames[i]);
                continue;
            }

            var rgbaStr = Utils.ConvertARGBtoRGBA(c!.ToString()!);
            colorValues[i] = rgbaStr;
        }

        return colorValues;
    }

    private static async Task HandleStartupTasks()
    {
        await Task.Run(Steam.StartMonitorSteam);

        if (Settings.Default.InjectOnAppStart)
        {
            await Task.Run(Steam.TryInject);
        }

        if (Settings.Default.RunSteamOnStart)
        {
            await Task.Run(() => Steam.StartSteam(Settings.Default.SteamLaunchArgs));
        }
    }

    public static void SetIconsState(bool state)
    {
        var icons = TrayIcon.GetIcons(Current!);
        if (icons == null)
        {
            return;
        }
        foreach (var icon in icons)
        {
            icon.IsVisible = state;
        }
    }

    public static void SetApplicationTheme(string themeVariantString)
    {
        var faTheme = Current?.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();
        faTheme?.PreferSystemTheme = themeVariantString == "System Default";

        Current!.RequestedThemeVariant = themeVariantString switch
        {
            FluentAvaloniaTheme.DarkModeString => ThemeVariant.Dark,
            FluentAvaloniaTheme.LightModeString => ThemeVariant.Light,
            FluentAvaloniaTheme.HighContrastModeString => FluentAvaloniaTheme.HighContrastTheme,
            _ => ThemeVariant.Default
        };
    }

    public static void StartMainWindow()
    {
        if (MainWindow.Instance is not null ||
            Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        desktop.MainWindow = new MainWindow();
    }

    public static void QuitApplication()
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Log.Logger.Info("Quitting");
                lifetime.Shutdown();
            });
        }
    }

    [UsedImplicitly]
    private void TrayIcon_OnClicked(object? sender, EventArgs e)
    {
        MainWindow.ShowWindow();
    }

    private static async Task OnActualThemeVariantChangedAsync()
    {
        try
        {
            Injector.SetColorScheme(Current!.ActualThemeVariant.ToString());
            await Injector.UpdateColorScheme();
        }
        catch (Exception ex)
        {
            Log.Logger.Error("Error updating color scheme on theme variant changed");
            Log.Logger.Debug(ex);
        }
    }

    private static async Task OnColorValuesChangedAsync()
    {
        try
        {
            if (Current == null)
            {
                return;
            }
            Injector.SetAccentColors(GetColorValues());
            await Injector.UpdateSystemAccentColors();
        }
        catch (Exception ex)
        {
            Log.Logger.Error("Error updating system accent colors");
            Log.Logger.Debug(ex);
        }
    }
}