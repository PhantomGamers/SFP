#region

using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using SFP.Models;
using SFP.Models.Injection.Config;
using SFP.Properties;
using SFP_UI.Models;
using SFP_UI.ViewModels;

#endregion

namespace SFP_UI.Pages;

public partial class MainPage : UserControl
{
    public MainPage()
    {
        InitializeComponent();
        DataContext = new MainPageViewModel();
        var flyout = (OpenFileDropDownButton.Flyout as MenuFlyout)!;
        flyout.Opened += (_, _) => OpenFileModel.PopulateOpenFileDropDownButton(flyout.Items);
    }
}
