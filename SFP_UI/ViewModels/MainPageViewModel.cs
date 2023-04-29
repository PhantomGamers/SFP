#region

using System.Reactive;
using System.Runtime.InteropServices;
using NLog;
using ReactiveUI;
using SFP.Models;
using Settings = SFP.Properties.Settings;

#endregion

namespace SFP_UI.ViewModels;

public class MainPageViewModel : ViewModelBase
{
    private string _output = string.Empty;

    private bool _buttonsEnabled = true;

    private int _caretIndex;

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
}
