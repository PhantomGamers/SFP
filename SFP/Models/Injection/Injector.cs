#region

using Flurl;
using Flurl.Http;

#endregion

namespace SFP.Models.Injection;

public static class Injector
{
    public const string CefDebuggingUrl = "http://localhost:8080";

    public static async Task InjectAsync()
    {
        try
        {
            Browser browser = await Browser.GetBrowserAsync();
            /*if (browser == null)
            {
                return;
            }*/
            await Task.Run(browser.MonitorTargetsAsync);
        }
        catch (Exception e)
        {
            // ignored
        }

        /*
        Log.Logger.Info("Injecting!");
        IEnumerable<Tab> tabs = await GetTabsAsync();
        IEnumerable<Tab> enumerable = tabs.ToList();
        Log.Logger.Info($"Found {enumerable.Count()} tabs");
        foreach (Tab tab in enumerable)
        {
            if (tab.Url.Contains("store.steampowered.com") || tab.Url.Contains("steamcommunity.com"))
            {
                Log.Logger.Info("Found store tab!");
                await tab.InjectCssAsync("webkit.css", "Store");
            }
            else if (tab.Title == "Steam")
            {
                Log.Logger.Info("Found Steam tab!");
                await tab.InjectCssAsync("libraryroot.custom.css", "Steam");
            }
            else if (tab.Title.Contains("Friends List"))
            {
                Log.Logger.Info("Found Friends tab!");
                await tab.InjectCssAsync("friends.custom.css", "Friends");
            }
        }
        */
    }
}
