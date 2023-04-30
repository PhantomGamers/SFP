using System.Reactive;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using NLog.Fluent;
using ReactiveUI;
using SFP_UI.Views;
using Log = SFP.Models.Log;

namespace SFP_UI.ViewModels;

public class AppViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> ShowWindowCommand { get; } =
        ReactiveCommand.Create(MainWindow.Instance!.ShowWindow);
    public ReactiveCommand<Unit, Unit> QuitCommand { get; } = ReactiveCommand.Create(QuitApplication);

    private static void QuitApplication()
    {
        Log.Logger.Info("Quitting");
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }
}
