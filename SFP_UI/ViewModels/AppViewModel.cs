using System.Reactive;
using ReactiveUI;
using SFP_UI.Views;

namespace SFP_UI.ViewModels;

public class AppViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> ShowWindow { get; } =
        ReactiveCommand.Create(MainWindow.ShowWindow);

    public ReactiveCommand<Unit, Unit> Quit { get; } = ReactiveCommand.Create(App.QuitApplication);

}
