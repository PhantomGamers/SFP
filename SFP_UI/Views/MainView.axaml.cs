#region

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using SFP_UI.Pages;

#endregion

namespace SFP_UI.Views;

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
            b.Activated += (_, _) => SetAppTitleColor(true);
            b.Deactivated += (_, _) => SetAppTitleColor(false);
        }

        _frameView = this.FindControl<Frame>("FrameView");
        if (_frameView == null)
        {
            return;
        }

        _frameView.Navigated += OnFrameViewNavigated;

        _navView = this.FindControl<NavigationView>("NavView");
        if (_navView != null)
        {
            _navView.MenuItemsSource = GetNavigationViewItems();
            _navView.FooterMenuItemsSource = GetFooterNavigationViewItems();
            _navView.ItemInvoked += OnNavigationViewItemInvoked;
            _navView.IsPaneOpen = false;
        }

        _ = _frameView.Navigate(typeof(MainPage));
    }

    private static void SetAppTitleColor(bool? isActive = null)
    {
        s_isActive = isActive ?? s_isActive;

        if (s_instance?.FindControl<TextBlock>("AppTitle") is not { } t)
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
        foreach (NavigationViewItem nvi in _navView!.MenuItemsSource)
        {
            if (nvi.Tag is not Type tag || tag != e.SourcePageType)
            {
                continue;
            }

            _navView.SelectedItem = nvi;
            break;
        }
    }

    private IEnumerable<NavigationViewItem> GetNavigationViewItems() => new List<NavigationViewItem>
    {
        new()
        {
            Content = "Home",
            Tag = typeof(MainPage),
            IconSource = (IconSource)this.FindResource("HomeIcon")!,
            Classes = { "SFPAppNav" }
        }
    };

    private IEnumerable<NavigationViewItem> GetFooterNavigationViewItems() => new List<NavigationViewItem>
    {
        new()
        {
            Content = "Settings",
            Tag = typeof(SettingsPage),
            IconSource = (IconSource)this.FindResource("SettingsIcon")!,
            Classes = { "SFPAppNav" }
        }
    };

    private void OnNavigationViewItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer is NavigationViewItem { Tag: Type typ })
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

        if (this.FindControl<Grid>("TitleBarHost") is { } g)
        {
            g.IsVisible = false;
        }

        const string key = "NavigationViewContentMargin";
        if (!Resources.ContainsKey(key))
        {
            return;
        }

        Thickness newThickness = new(0, 0, 0, 0);
        Resources[key] = newThickness;
    }
}
