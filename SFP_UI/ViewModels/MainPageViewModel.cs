#region

using System.Reactive;
using NLog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Semver;
using SFP.Models;
using SFP.Models.Injection;

#endregion

namespace SFP_UI.ViewModels;

public class MainPageViewModel : ViewModelBase
{
    public static MainPageViewModel? Instance { get; private set; }

    [Reactive] public bool UpdateNotificationIsOpen { get; set; }

    [Reactive] public string UpdateNotificationContent { get; set; } = string.Empty;

    [Reactive] public bool ButtonsEnabled { get; set; } = true;

    [Reactive] public bool IsInjected { get; set; }

    [Reactive] public string StartSteamText { get; set; } = Steam.IsSteamRunning ? "Restart Steam" : "Start Steam";

    private static string s_output = string.Empty;
    public string Output
    {
        get => s_output;
        private set => this.RaiseAndSetIfChanged(ref s_output, value);
    }

    private static int s_caretIndex;
    public int CaretIndex
    {
        get => s_caretIndex;
        private set => this.RaiseAndSetIfChanged(ref s_caretIndex, value);
    }

    public ReactiveCommand<string, Unit> UpdateNotificationViewCommand { get; } =
        ReactiveCommand.Create<string>(Utils.OpenUrl);

    public ReactiveCommand<Unit, Unit> InjectCommand { get; } =
        ReactiveCommand.Create(Steam.RunTryInject);

    public ReactiveCommand<Unit, Unit> StopInjectCommand { get; } = ReactiveCommand.Create(Injector.StopInjection);

    public ReactiveCommand<Unit, Unit> StartSteamCommand { get; } =
        ReactiveCommand.Create(Steam.RunRestartSteam);

    public MainPageViewModel()
    {
        Instance = this;
        Injector.InjectionStateChanged += (_, _) => IsInjected = Injector.IsInjected;
        Steam.SteamStarted += (_, _) => StartSteamText = "Restart Steam";
        Steam.SteamStopped += (_, _) => StartSteamText = "Start Steam";
    }

    public static void PrintLine(LogLevel level, string message)
    {
        Print(level, $"{message}\n");
    }

    private static void Print(LogLevel level, string message)
    {
        if (Instance != null)
        {
            Instance.Output = string.Concat(Instance.Output, $"[{DateTime.Now}][{level}] {message}");
            Instance.CaretIndex = Instance.Output.Length;
        }
        else
        {
            s_output = string.Concat(s_output, $"[{DateTime.Now}][{level}] {message}");
            s_caretIndex = s_output.Length;
        }
    }

    public void ShowUpdateNotification(SemVersion oldVersion, SemVersion newVersion)
    {
        Log.Logger.Info($"There is an update available! Your version: {oldVersion} Latest version: {newVersion}");
        UpdateNotificationContent =
            $"Your version: {oldVersion}{Environment.NewLine}Latest version: {newVersion}";
        UpdateNotificationIsOpen = true;
    }
}
