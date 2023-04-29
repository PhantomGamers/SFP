using System.Text.Json.Serialization;
using Flurl;
using Flurl.Http;

namespace SFP.Models.Injection;

public static class Injector
{
    private const string CefDebuggingUrl = "http://localhost:8080";

    private static async Task<IEnumerable<Tab>> GetTabs()
    {
        try
        {
            return await CefDebuggingUrl.AppendPathSegment("json").GetJsonAsync<IEnumerable<Tab>>();
        }
        catch (FlurlHttpException e)
        {
            Log.Logger.Error(e);
            return Enumerable.Empty<Tab>();
        }
    }

    public static async Task Inject()
    {
        Log.Logger.Info("Injecting!");
        IEnumerable<Tab> tabs = await GetTabs();
        IEnumerable<Tab> enumerable = tabs.ToList();
        Log.Logger.Info($"Found {enumerable.Count()} tabs");
        foreach (Tab tab in enumerable)
        {
            if (tab.Url.Contains("store.steampowered.com"))
            {
                Log.Logger.Info("Found store tab!");
                string cssInjectString =
                $$"""
                (function() {
                    const style = document.createElement('style');
                    style.id = '{{Guid.NewGuid()}}';
                    document.head.append(style);
                    style.textContent = `@import url('https://steamloopback.host/webkit.css');`;
                })()
                """.Trim().Replace('\n', ' ');
                await tab.EvaluateJavaScript(cssInjectString);
                Log.Logger.Info("Applied custom style");
            }
        }
    }
}

public struct Browser
{
    [JsonPropertyName("webSocketDebuggerUrl")]
    public string WebSocketDebuggerUrl { get; set; }
}
