using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using SFP_UI.Pages;

namespace SFP_UI.ViewModels;

public class MainViewViewModel : ViewModelBase
{
    public NavigationFactory NavigationFactory { get; } = new();
}

public class NavigationFactory : INavigationPageFactory
{
    public NavigationFactory()
    {
        Instance = this;
    }

    private static NavigationFactory? Instance { get; set; }

    // Create a page based on a Type, but you can create it however you want
    public Control? GetPage(Type srcType)
    {
        // Return null here because we won't use this method at all
        return null;
    }

    // Create a page based on an object, such as a view model
    public Control GetPageFromObject(object target)
    {
        return target switch
        {
            MainPage => _pages[0],
            SettingsPage => _pages[1],
            SkinBrowserPage => _pages[2],
            _ => throw new Exception()
        };
    }

    // Do this to avoid needing Activator.CreateInstance to create from type info
    // and to avoid a ridiculous amount of 'ifs'
    private readonly Control[] _pages =
    [
        new MainPage(),
        new SettingsPage(),
        new SkinBrowserPage()
    ];

    public static Control[] GetPages()
    {
        return Instance!._pages;
    }
}
