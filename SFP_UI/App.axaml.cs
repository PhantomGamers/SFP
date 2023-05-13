#region

using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using FluentAvalonia.Styling;
using ReactiveUI;
using SFP.Models;
using SFP_UI.Models;
using SFP_UI.ViewModels;
using SFP_UI.Views;
using Settings = SFP.Properties.Settings;

#endregion

namespace SFP_UI;

public class App : Application
{
    public static ReactiveCommand<Unit, Unit> ShowWindowCommand { get; } =
        ReactiveCommand.Create(MainWindow.ShowWindow);

    public static ReactiveCommand<Unit, Unit> QuitCommand { get; } = ReactiveCommand.Create(QuitApplication);

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel() };

            try
            {
                desktop.MainWindow.Title += $" v{UpdateChecker.Version}";
                await Dispatcher.UIThread.InvokeAsync(() => Log.Logger.Info(
                    $"Initializing SFP version {UpdateChecker.Version} on platform {RuntimeInformation.RuntimeIdentifier}"));
            }
            catch (Exception e)
            {
                Log.Logger.Error(e);
            }
        }

        base.OnFrameworkInitializationCompleted();

        SetIconsState(Settings.Default.ShowTrayIcon);

        if (Settings.Default is { StartMinimized: true, MinimizeToTray: true })
        {
            MainWindow.Instance?.Hide();
        }

        await HandleStartupTasks();

        if (Settings.Default.CheckForUpdates)
        {
            await Dispatcher.UIThread.InvokeAsync(UpdateChecker.CheckForUpdates);
        }

        if (SettingsPageViewModel.Instance != null)
        {
            await Dispatcher.UIThread.InvokeAsync(SettingsPageViewModel.Instance.OnReloadCommand);
            await Dispatcher.UIThread.InvokeAsync(SettingsPageViewModel.OnSaveCommand);
        }
    }

    private static async Task HandleStartupTasks()
    {
        await Task.Run(Steam.StartMonitorSteam);

        if (Settings.Default.InjectOnAppStart && Steam.IsSteamWebHelperRunning)
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
        var icons = TrayIcon.GetIcons(Application.Current!);
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
        var faTheme = AvaloniaLocator.Current.GetService<FluentAvaloniaTheme>();
        if (faTheme != null)
        {
            faTheme.PreferSystemTheme = themeVariantString == "System Default";
        }

        Current!.RequestedThemeVariant = themeVariantString switch
        {
            FluentAvaloniaTheme.DarkModeString => ThemeVariant.Dark,
            FluentAvaloniaTheme.LightModeString => ThemeVariant.Light,
            FluentAvaloniaTheme.HighContrastModeString => FluentAvaloniaTheme.HighContrastTheme,
            _ => ThemeVariant.Default
        };
    }

    private static void QuitApplication()
    {
        Log.Logger.Info("Quitting");
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void TrayIcon_OnClicked(object? sender, EventArgs e) => MainWindow.ShowWindow();
}
