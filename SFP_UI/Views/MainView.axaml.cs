#region

using Avalonia;
using Avalonia.Controls;

using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;

using SFP_UI.Pages;
using SFP_UI.ViewModels;

#endregion

namespace SFP_UI.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        Instance = this;
        InitializeComponent();
    }

    public static MainView? Instance { get; private set; }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        MainViewViewModel vm = new();
        DataContext = vm;
        FrameView.NavigationPageFactory = vm.NavigationFactory;

        FrameView.Navigated += OnFrameViewNavigated;
        NavView.ItemInvoked += OnNavigationViewItemInvoked;

        NavView.MenuItemsSource = GetNavigationViewItems();
        NavView.FooterMenuItemsSource = GetFooterNavigationViewItems();

        NavView.IsPaneOpen = false;

        IEnumerable<FANavigationViewItem> navViewItems = NavView.MenuItemsSource.Cast<FANavigationViewItem>();
        FrameView.NavigateFromObject(navViewItems.ElementAt(0).Tag);
    }

    private void SetNviIcon(FANavigationViewItem? item, bool selected)
    {
        // Technically, yes you could set up binding and converters and whatnot to let the icon change
        // between filled and unfilled based on selection, but this is so much simpler

        if (item == null)
        {
            return;
        }

        object? t = item.Tag;

        item.IconSource = t switch
        {
            MainPage => this.TryFindResource(selected ? "HomeIconFilled" : "HomeIcon", out object? value)
                ? (FAIconSource)value!
                : null,
            SettingsPage => this.TryFindResource(selected ? "SettingsIconFilled" : "SettingsIcon", out object? value)
                ? (FAIconSource)value!
                : null,
            _ => item.IconSource
        };
    }

    private void OnFrameViewNavigated(object sender, FANavigationEventArgs e)
    {
        Control? page = e.Content as Control;

        foreach (FANavigationViewItem nvi in NavView.MenuItemsSource)
        {
            if (nvi.Tag != null && nvi.Tag.Equals(page))
            {
                NavView.SelectedItem = nvi;
                SetNviIcon(nvi, true);
            }
            else
            {
                SetNviIcon(nvi, false);
            }
        }

        foreach (FANavigationViewItem nvi in NavView.FooterMenuItemsSource)
        {
            if (nvi.Tag != null && nvi.Tag.Equals(page))
            {
                NavView.SelectedItem = nvi;
                SetNviIcon(nvi, true);
            }
            else
            {
                SetNviIcon(nvi, false);
            }
        }
    }

    private IEnumerable<FANavigationViewItem> GetNavigationViewItems()
    {
        return
        [
            new FANavigationViewItem
            {
                Content = "Home",
                Tag = NavigationFactory.GetPages()[0],
                IconSource = (FAIconSource)this.FindResource("HomeIcon")!,
                Classes = { "SFPAppNav" }
            }
        ];
    }

    private IEnumerable<FANavigationViewItem> GetFooterNavigationViewItems()
    {
        return
        [
            new FANavigationViewItem
            {
                Content = "Settings",
                Tag = NavigationFactory.GetPages()[1],
                IconSource = (FAIconSource)this.FindResource("SettingsIcon")!,
                Classes = { "SFPAppNav" }
            }
        ];
    }

    private void OnNavigationViewItemInvoked(object? sender, FANavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer is FANavigationViewItem { Tag: Control c })
        {
            _ = FrameView.NavigateFromObject(c);
        }
    }
}