#region

using System.Reactive;
using NLog;
using ReactiveUI;
using Semver;
using SFP.Models;

#endregion

namespace SFP_UI.ViewModels;

public class MainPageViewModel : ViewModelBase
{
    private bool _buttonsEnabled = true;

    private int _caretIndex;

    private string _output = string.Empty;

    private string _updateNotificationContent = string.Empty;
    private bool _updateNotificationIsOpen;
    public MainPageViewModel() => Instance = this;

    public static MainPageViewModel? Instance { get; private set; }

    public ReactiveCommand<string, Unit> UpdateNotificationViewCommand { get; } =
        ReactiveCommand.Create<string>(Utils.OpenUrl);

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
}
