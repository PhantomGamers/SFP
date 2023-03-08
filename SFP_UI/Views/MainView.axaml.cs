using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;

using SFP_UI.Pages;

namespace SFP_UI.Views
{
    public partial class MainView : UserControl
    {
        private static bool s_isActive;
        private static MainView? s_instance;
        private Frame? _frameView;
        private NavigationView? _navView;

        public MainView()
        {
            s_instance = this;
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (e.Root is Window b)
            {
                b.Opened += OnParentWindowOpened;
                b.Activated += (s, e) => SetAppTitleColor(true);
                b.Deactivated += (s, e) => SetAppTitleColor(false);
            }

            _frameView = this.FindControl<Frame>("FrameView");
            _frameView.Navigated += OnFrameViewNavigated;

            _navView = this.FindControl<NavigationView>("NavView");
            _navView.MenuItems = GetNavigationViewItems();
            _navView.FooterMenuItems = GetFooterNavigationViewItems();
            _navView.ItemInvoked += OnNavigationViewItemInvoked;
            _navView.IsPaneOpen = false;

            _ = _frameView.Navigate(typeof(MainPage));
        }

        private static void SetAppTitleColor(bool? isActive = null)
        {
            s_isActive = isActive ?? s_isActive;

            if (s_instance?.FindControl<TextBlock>("AppTitle") is not TextBlock t)
            {
                return;
            }

            t.Foreground = s_isActive switch
            {
                false when s_instance.TryFindResource("TextFillColorDisabledBrush", out object? disabled) =>
                    (IBrush)disabled!,
                true when s_instance.TryFindResource("TextFillColorPrimaryBrush", out object? primary) =>
                    (IBrush)primary!,
                _ => t.Foreground
            };
        }

        private void OnFrameViewNavigated(object sender, NavigationEventArgs e)
        {
            foreach (NavigationViewItem nvi in _navView!.MenuItems)
            {
                if (nvi.Tag is not Type tag || tag != e.SourcePageType)
                {
                    continue;
                }

                _navView.SelectedItem = nvi;
                break;
            }
        }

        private List<NavigationViewItem> GetNavigationViewItems() => new()
        {
            new NavigationViewItem
            {
                Content = "Home",
                Tag = typeof(MainPage),
                IconSource = (IconSource)this.FindResource("HomeIcon"),
                Classes = { "SFPAppNav" }
            },
        };

        private List<NavigationViewItem> GetFooterNavigationViewItems() => new()
        {
            new NavigationViewItem
            {
                Content = "Settings",
                Tag = typeof(SettingsPage),
                IconSource = (IconSource)this.FindResource("SettingsIcon"),
                Classes = { "SFPAppNav" }
            }
        };

        private void OnNavigationViewItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
        {
            if (e.InvokedItemContainer is NavigationViewItem nvi && nvi.Tag is Type typ)
            {
                _ = _frameView!.Navigate(typ, null, e.RecommendedNavigationTransitionInfo);
            }
        }

        private void OnParentWindowOpened(object? sender, EventArgs e)
        {
            if (sender is not Window w)
            {
                return;
            }

            w.Opened -= OnParentWindowOpened;

            if (OperatingSystem.IsWindows())
            {
                return;
            }

            if (this.FindControl<Grid>("TitleBarHost") is Grid g)
            {
                g.IsVisible = false;
            }

            const string key = "NavigationViewContentMargin";
            if (!Resources.ContainsKey(key))
            {
                return;
            }

            var newThickness = new Thickness(0, 0, 0, 0);
            Resources[key] = newThickness;
        }
    }
}
