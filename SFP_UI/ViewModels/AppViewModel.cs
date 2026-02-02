using ReactiveUI.SourceGenerators;

using SFP.Models;
using SFP.Models.Injection;

using SFP_UI.Views;

namespace SFP_UI.ViewModels;

public partial class AppViewModel : ViewModelBase
{
    [Reactive] public partial string InjectHeader { get; set; } = "Start Injection";

    [ReactiveCommand]
    private static void RunInject()
    {
        if (Injector.IsInjected)
        {
            Injector.StopInjection();
        }
        else
        {
            _ = Steam.RunTryInject();
        }
    }

    [Reactive] public partial string SteamHeader { get; set; } = Steam.IsSteamRunning ? "Restart Steam" : "Start Steam";

    [ReactiveCommand]
    private static void RunSteam() => _ = Steam.RunRestartSteam();

    [ReactiveCommand]
    private static void ShowSettings() => MainWindow.ShowSettings();

    [ReactiveCommand]
    private static void ShowWindow() => MainWindow.ShowWindow();

    [ReactiveCommand]
    private static void Quit() => App.QuitApplication();

    public AppViewModel()
    {
        OnInjectionStateChanged(null, EventArgs.Empty);
        Injector.InjectionStateChanged -= OnInjectionStateChanged;
        Injector.InjectionStateChanged += OnInjectionStateChanged;
        Steam.SteamStarted -= OnSteamStarted;
        Steam.SteamStarted += OnSteamStarted;
        Steam.SteamStopped -= OnSteamStopped;
        Steam.SteamStopped += OnSteamStopped;
    }

    private void OnSteamStopped(object? o, EventArgs eventArgs)
    {
        SteamHeader = "Start Steam";
    }

    private void OnSteamStarted(object? o, EventArgs eventArgs)
    {
        SteamHeader = "Restart Steam";
    }

    private void OnInjectionStateChanged(object? o, EventArgs eventArgs)
    {
        InjectHeader = Injector.IsInjected ? "Stop Injection" : "Start Injection";
    }
}