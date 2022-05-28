using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using FluentAvalonia.Core;
using FluentAvalonia.Core.ApplicationModel;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;

using SFP_UI.Pages;

namespace SFP_UI.Views
{
    public partial class MainView : UserControl
    {
        private Frame? _frameView;
        private NavigationView? _navView;

        public MainView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (e.Root is Window b)
            {
                b.Opened += OnParentWindowOpened;
            }
            _frameView = this.FindControl<Frame>("FrameView");
            _frameView.Navigated += OnFrameViewNavigated;

            _navView = this.FindControl<NavigationView>("NavView");
            _navView.MenuItems = GetNavigationViewItems();
            _navView.FooterMenuItems = GetFooterNavigationViewItems();
            _navView.ItemInvoked += OnNavigationViewItemInvoked;
            _navView.IsPaneOpen = false;

            _frameView.Navigate(typeof(MainPage));
        }

        private void OnFrameViewNavigated(object sender, NavigationEventArgs e)
        {
            foreach (NavigationViewItem nvi in _navView!.MenuItems)
            {
                if (nvi.Tag is Type tag && tag == e.SourcePageType)
                {
                    _navView.SelectedItem = nvi;
                    break;
                }
            }
        }

        private static List<NavigationViewItem> GetNavigationViewItems()
        {
            return new List<NavigationViewItem>
            {
                new NavigationViewItem
                {
                    Content = "Home",
                    Tag = typeof(MainPage),
                    Icon = new SymbolIcon { Symbol = Symbol.Home },
                    Classes =
                    {
                        "SFPAppNav"
                    }
                },
            };
        }

        private static List<NavigationViewItem> GetFooterNavigationViewItems()
        {
            return new List<NavigationViewItem>
            {
                new NavigationViewItem
                {
                    Content = "Settings",
                    Tag = typeof(SettingsPage),
                    Icon = new SymbolIcon { Symbol = Symbol.Settings },
                    Classes =
                    {
                        "SFPAppNav"
                    }
                }
            };
        }

        private void OnNavigationViewItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
        {
            if (e.InvokedItemContainer is NavigationViewItem nvi && nvi.Tag is Type typ)
            {
                _frameView!.Navigate(typ, null, e.RecommendedNavigationTransitionInfo);
            }
        }

        private void OnParentWindowOpened(object? sender, EventArgs e)
        {
            if (sender is Window w)
            {
                w.Opened -= OnParentWindowOpened;

                if (sender is CoreWindow cw)
                {
                    CoreApplicationViewTitleBar? titleBar = cw.TitleBar;
                    if (titleBar != null)
                    {
                        titleBar.ExtendViewIntoTitleBar = true;

                        titleBar.LayoutMetricsChanged += OnApplicationTitleBarLayoutMetricsChanged;

                        if (this.FindControl<Grid>("TitleBarHost") is Grid g)
                        {
                            cw.SetTitleBar(g);
                            g.Margin = new Thickness(0, 0, titleBar.SystemOverlayRightInset, 0);
                        }
                    }
                }
            }
        }

        private void OnApplicationTitleBarLayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            if (this.FindControl<Grid>("TitleBarHost") is Grid g)
            {
                g.Margin = new Thickness(0, 0, sender.SystemOverlayRightInset, 0);
            }
        }
    }
}