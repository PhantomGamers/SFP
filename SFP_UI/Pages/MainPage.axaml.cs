#region

using Avalonia.Controls;
using SFP_UI.ViewModels;

#endregion

namespace SFP_UI.Pages;

public partial class MainPage : UserControl
{
    public MainPage()
    {
        InitializeComponent();
        DataContext = new MainPageViewModel();
    }
}
