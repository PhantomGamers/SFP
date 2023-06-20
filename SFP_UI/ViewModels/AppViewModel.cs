using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SFP.Models;
using SFP.Models.Injection;
using SFP_UI.Views;

namespace SFP_UI.ViewModels;

public class AppViewModel : ViewModelBase
{
    [Reactive] public string InjectHeader { get; set; } = "Start Injection";
    [Reactive]
    public ReactiveCommand<Unit, Unit> RunInject { get; set; } =
        ReactiveCommand.Create(Steam.RunTryInject);

    [Reactive] public string SteamHeader { get; set; } = Steam.IsSteamRunning ? "Restart Steam" : "Start Steam";
    public ReactiveCommand<Unit, Unit> RunSteam { get; set; } =
        ReactiveCommand.Create(Steam.RunRestartSteam);

    public ReactiveCommand<Unit, Unit> ShowSettings { get; } =
        ReactiveCommand.Create(MainWindow.ShowSettings);

    public ReactiveCommand<Unit, Unit> ShowWindow { get; } =
        ReactiveCommand.Create(MainWindow.ShowWindow);
    public ReactiveCommand<Unit, Unit> Quit { get; } = ReactiveCommand.Create(App.QuitApplication);

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
        if (Injector.IsInjected)
        {
            InjectHeader = "Stop Injection";
            RunInject = ReactiveCommand.Create(Injector.StopInjection);
        }
        else
        {
            InjectHeader = "Start Injection";
            RunInject = ReactiveCommand.Create(Steam.RunTryInject);
        }
    }
}
