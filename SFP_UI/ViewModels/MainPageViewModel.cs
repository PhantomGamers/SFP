#region

using System.Reactive;
using NLog;
using ReactiveUI;
using Semver;
using SFP.Models;
using SFP.Models.Injection;

#endregion

namespace SFP_UI.ViewModels;

public class MainPageViewModel : ViewModelBase
{
    private bool _buttonsEnabled = true;
    private int _caretIndex;
    private bool _isInjected;
    private string _output = string.Empty;
    private string _startSteamText = Steam.IsSteamRunning ? "Restart Steam" : "Start Steam";
    private string _updateNotificationContent = string.Empty;
    private bool _updateNotificationIsOpen;

    public MainPageViewModel()
    {
        Instance = this;
        Injector.InjectionStateChanged += (_, _) => IsInjected = Injector.IsInjected;
        Steam.SteamStarted += (_, _) => StartSteamText = "Restart Steam";
        Steam.SteamStopped += (_, _) => StartSteamText = "Start Steam";
    }

    public static MainPageViewModel? Instance { get; private set; }

    public ReactiveCommand<string, Unit> UpdateNotificationViewCommand { get; } =
        ReactiveCommand.Create<string>(Utils.OpenUrl);

    public ReactiveCommand<Unit, Unit> InjectCommand { get; } =
        ReactiveCommand.Create(Steam.RunTryInject);

    public ReactiveCommand<Unit, Unit> StopInjectCommand { get; } = ReactiveCommand.Create(Injector.StopInjection);
    public ReactiveCommand<string, Unit> OpenFileCommand { get; } = ReactiveCommand.CreateFromTask<string>(OpenFile);
    public ReactiveCommand<string, Unit> OpenDirCommand { get; } = ReactiveCommand.Create<string>(OpenDir);

    public ReactiveCommand<Unit, Unit> StartSteamCommand { get; } =
        ReactiveCommand.Create(Steam.RunRestartSteam);

    public bool UpdateNotificationIsOpen
    {
        get => _updateNotificationIsOpen;
        set => this.RaiseAndSetIfChanged(ref _updateNotificationIsOpen, value);
    }

    public string UpdateNotificationContent
    {
        get => _updateNotificationContent;
        set => this.RaiseAndSetIfChanged(ref _updateNotificationContent, value);
    }

    public string Output
    {
        get => _output;
        private set => this.RaiseAndSetIfChanged(ref _output, value);
    }

    public int CaretIndex
    {
        get => _caretIndex;
        private set => this.RaiseAndSetIfChanged(ref _caretIndex, value);
    }

    public bool ButtonsEnabled
    {
        get => _buttonsEnabled;
        set => this.RaiseAndSetIfChanged(ref _buttonsEnabled, value);
    }

    public bool IsInjected
    {
        get => Injector.IsInjected;
        set => this.RaiseAndSetIfChanged(ref _isInjected, value);
    }

    public string StartSteamText
    {
        get => _startSteamText;
        set => this.RaiseAndSetIfChanged(ref _startSteamText, value);
    }

    public void PrintLine(LogLevel level, string message) => Print(level, $"{message}\n");

    private void Print(LogLevel level, string message)
    {
        Output = string.Concat(Output, $"[{DateTime.Now}][{level}] {message}");
        CaretIndex = Output.Length;
    }

    public void ShowUpdateNotification(SemVersion oldVersion, SemVersion newVersion)
    {
        Log.Logger.Info($"There is an update available! Your version: {oldVersion} Latest version: {newVersion}");
        UpdateNotificationContent =
            $"Your version: {oldVersion}{Environment.NewLine}Latest version: {newVersion}";
        UpdateNotificationIsOpen = true;
    }

    private static async Task OpenPath(string relativePath, bool isDirectory)
    {
        var path = Path.Join(Steam.SteamDir, @"steamui", relativePath);
        try
        {
            if (isDirectory)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            else
            {
                if (!File.Exists(path))
                {
                    await File.Create(path).DisposeAsync();
                }
            }

            Utils.OpenUrl(path);
        }
        catch (Exception e)
        {
            Log.Logger.Warn("Could not open " + path);
            Log.Logger.Debug(e);
        }
    }

    private static async Task OpenFile(string relativeFilePath)
    {
        await OpenPath(relativeFilePath, false);
    }

    private static void OpenDir(string relativeDirPath)
    {
        OpenPath(relativeDirPath, true).Wait();
    }
}
