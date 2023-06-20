#region

using System.Reactive;
using System.Text;
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

    private static readonly StringBuilder s_outputBuilder = new();

    [Reactive] public bool UpdateNotificationIsOpen { get; set; }

    [Reactive] public string UpdateNotificationContent { get; set; } = string.Empty;

    [Reactive] public bool ButtonsEnabled { get; set; } = true;

    [Reactive] public bool IsInjected { get; set; } = Injector.IsInjected;

    [Reactive] public string StartSteamText { get; set; } = Steam.IsSteamRunning ? "Restart Steam" : "Start Steam";

    private string _output = s_outputBuilder.ToString();
    public string Output
    {
        get => _output;
        private set => this.RaiseAndSetIfChanged(ref _output, value);
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
        Injector.InjectionStateChanged -= OnInjectionStateChanged;
        Injector.InjectionStateChanged += OnInjectionStateChanged;
        Steam.SteamStarted -= OnSteamStarted;
        Steam.SteamStarted += OnSteamStarted;
        Steam.SteamStopped -= OnSteamStopped;
        Steam.SteamStopped += OnSteamStopped;
    }

    private void OnSteamStopped(object? o, EventArgs eventArgs)
    {
        StartSteamText = "Start Steam";
    }

    private void OnSteamStarted(object? o, EventArgs eventArgs)
    {
        StartSteamText = "Restart Steam";
    }

    private void OnInjectionStateChanged(object? o, EventArgs eventArgs)
    {
        IsInjected = Injector.IsInjected;
    }

    public static void PrintLine(LogLevel level, string message)
    {
        Print(level, $"{message}\n");
    }

    private static void Print(LogLevel level, string message)
    {
        TrimOutput();
        s_outputBuilder.Append($"[{DateTime.Now}][{level}] {message}");
        if (Instance == null)
        {
            return;
        }
        Instance.Output = s_outputBuilder.ToString();
    }

    private static void TrimOutput()
    {
        const short MaxLength = short.MaxValue;
        if (s_outputBuilder.Length <= MaxLength)
        {
            return;
        }

        var output = s_outputBuilder.ToString();
        var firstNewlineIndex = output.IndexOf('\n');
        if (firstNewlineIndex == -1)
        {
            return;
        }
        var firstLine = output[..firstNewlineIndex];
        var lastNewlineIndex = output.IndexOf('\n', MaxLength / 2);
        if (lastNewlineIndex == -1)
        {
            return;
        }
        s_outputBuilder.Clear();
        s_outputBuilder.Append(firstLine);
        s_outputBuilder.Append(output.AsSpan(lastNewlineIndex));
    }

    public void ShowUpdateNotification(SemVersion oldVersion, SemVersion newVersion)
    {
        Log.Logger.Info($"There is an update available! Your version: {oldVersion} Latest version: {newVersion}");
        UpdateNotificationContent =
            $"Your version: {oldVersion}{Environment.NewLine}Latest version: {newVersion}";
        UpdateNotificationIsOpen = true;
    }
}
