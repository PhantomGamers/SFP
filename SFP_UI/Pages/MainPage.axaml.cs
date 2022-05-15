using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SFP_UI.Pages
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
            DataContext = new ViewModels.MainPageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
