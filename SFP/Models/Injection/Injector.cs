#region

using Flurl;
using Flurl.Http;

#endregion

namespace SFP.Models.Injection;

public static class Injector
{
    private const string CefDebuggingUrl = "http://localhost:8080";

    private static async Task<IEnumerable<Tab>> GetTabsAsync()
    {
        try
        {
            return await CefDebuggingUrl.AppendPathSegment("json").GetJsonAsync<IEnumerable<Tab>>();
        }
        catch (FlurlHttpException e)
        {
            Log.Logger.Error("Could not fetch tabs, is Steam running with CEF debugging enabled?");
            Log.Logger.Error(e);
            return Enumerable.Empty<Tab>();
        }
    }

    public static async Task InjectAsync()
    {
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
    }
}
