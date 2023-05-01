#region

using System.Reactive;
using Avalonia;
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
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Log.Logger.Info($"Initializing SFP version {UpdateChecker.Version}");

                    if (Settings.Default.InjectOnSteamStart)
                    {
                        _ = Task.Run(() =>
                        {
                            Steam.StartMonitorSteam();
                            Steam.StartSteam();
                        });
                    }
                    else if (Settings.Default.RunSteamOnStart)
                    {
                        _ = Task.Run(() => Steam.StartSteam(Settings.Default.SteamLaunchArgs));
                    }
                });
            }
            catch (Exception e)
            {
                Log.Logger.Error(e);
            }
        }

        if (Settings.Default.CheckForUpdates)
        {
            await Dispatcher.UIThread.InvokeAsync(UpdateChecker.CheckForUpdates);
        }

        if (SettingsPageViewModel.Instance != null)
        {
            await Dispatcher.UIThread.InvokeAsync(SettingsPageViewModel.Instance.OnReloadCommand);
            await Dispatcher.UIThread.InvokeAsync(SettingsPageViewModel.OnSaveCommand);
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static void SetApplicationTheme(string themeVariantString)
    {
        FluentAvaloniaTheme? faTheme = AvaloniaLocator.Current.GetService<FluentAvaloniaTheme>();
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
}
