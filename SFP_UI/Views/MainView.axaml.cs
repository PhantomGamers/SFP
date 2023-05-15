#region

using System.Collections;
using Avalonia;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using SFP_UI.Pages;

#endregion

namespace SFP_UI.Views;

public partial class MainView : UserControl
{
    private Frame? _frameView;
    private NavigationView? _navView;

    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

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

    private void SetNviIcon(NavigationViewItem item, bool selected, bool recurse = false)
    {
        // Technically, yes you could set up binding and converters and whatnot to let the icon change
        // between filled and unfilled based on selection, but this is so much simpler

        if (item.Tag is not Type type)
        {
            return;
        }

        if (type == typeof(MainPage))
        {
            item.IconSource = this.TryFindResource(selected ? "HomeIconFilled" : "HomeIcon", out var value)
                ? (IconSource)value!
                : null;
        }
        else if (type == typeof(SettingsPage))
        {
            item.IconSource = this.TryFindResource(selected ? "SettingsIconFilled" : "SettingsIcon", out var value)
                ? (IconSource)value!
                : null;
        }

        if (recurse)
        {
            return;
        }

        if (_navView is null)
        {
            return;
        }

        var allItems = _navView.MenuItemsSource.Cast<object>()
            .Concat(_navView.FooterMenuItemsSource.Cast<object>())
            .OfType<NavigationViewItem>();

        foreach (var nvi in allItems.Where(nvi => !nvi.Equals(item)))
        {
            SetNviIcon(nvi, false, true);
        }
    }

    private void OnFrameViewNavigated(object sender, NavigationEventArgs e)
    {
        if (_navView != null && !TryNavigateItem(e, _navView.MenuItemsSource))
        {
            _ = TryNavigateItem(e, _navView.FooterMenuItemsSource);
        }
    }

    private bool TryNavigateItem(NavigationEventArgs e, IEnumerable itemsSource)
    {
        foreach (NavigationViewItem nvi in itemsSource)
        {
            if (nvi.Tag is not Type tag || tag != e.SourcePageType)
            {
                continue;
            }

            if (_navView != null)
            {
                _navView.SelectedItem = nvi;
            }

            SetNviIcon(nvi, true);
            return true;
        }

        return false;
    }

    private IEnumerable<NavigationViewItem> GetNavigationViewItems()
    {
        return new List<NavigationViewItem>
        {
            new()
            {
                Content = "Home",
                Tag = typeof(MainPage),
                IconSource = (IconSource)this.FindResource("HomeIcon")!,
                Classes = { "SFPAppNav" }
            }
        };
    }

    private IEnumerable<NavigationViewItem> GetFooterNavigationViewItems()
    {
        return new List<NavigationViewItem>
        {
            new()
            {
                Content = "Settings",
                Tag = typeof(SettingsPage),
                IconSource = (IconSource)this.FindResource("SettingsIcon")!,
                Classes = { "SFPAppNav" }
            }
        };
    }

    private void OnNavigationViewItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        // Change the current selected item back to normal
        SetNviIcon((_navView!.SelectedItem as NavigationViewItem)!, false);
        if (e.InvokedItemContainer is NavigationViewItem { Tag: Type typ })
        {
            _ = _frameView!.Navigate(typ, null, e.RecommendedNavigationTransitionInfo);
        }
    }
}
