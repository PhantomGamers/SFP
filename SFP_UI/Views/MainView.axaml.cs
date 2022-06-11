using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using FluentAvalonia.Core.ApplicationModel;
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

        public static void SetAppTitleColor(bool? isActive = null)
        {
            s_isActive = isActive ?? s_isActive;

            if (s_instance?.FindControl<TextBlock>("AppTitle") is TextBlock t)
            {
                if (!s_isActive && s_instance.TryFindResource("TextFillColorDisabledBrush", out object? disabled))
                {
                    t.Foreground = (IBrush)disabled!;
                }
                else if (s_isActive && s_instance.TryFindResource("TextFillColorPrimaryBrush", out object? primary))
                {
                    t.Foreground = (IBrush)primary!;
                }
            }
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

        private static List<NavigationViewItem> GetNavigationViewItems() => new()
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

        private static List<NavigationViewItem> GetFooterNavigationViewItems() => new()
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

        private void OnNavigationViewItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
        {
            if (e.InvokedItemContainer is NavigationViewItem nvi && nvi.Tag is Type typ)
            {
                _ = _frameView!.Navigate(typ, null, e.RecommendedNavigationTransitionInfo);
            }
        }

        private void OnParentWindowOpened(object? sender, EventArgs e)
        {
            if (sender is Window w)
            {
                w.Opened -= OnParentWindowOpened;

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (this.FindControl<Grid>("TitleBarHost") is Grid g)
                    {
                        g.IsVisible = false;
                    }

                    string key = "NavigationViewContentMargin";
                    if (Resources.ContainsKey(key))
                    {
                        var newThickness = new Thickness(0, 0, 0, 0);
                        Resources[key] = newThickness;
                    }

                    return;
                }

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
