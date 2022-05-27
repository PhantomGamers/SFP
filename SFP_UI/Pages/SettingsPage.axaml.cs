using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using SFP_UI.ViewModels;

namespace SFP_UI.Pages
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
            DataContext = new SettingsPageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
