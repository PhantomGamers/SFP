#region

using System.Text.Json.Serialization;

using Flurl;
using Flurl.Http;

using SFP.Properties;

#endregion

namespace SFP.Models.Injection;

public struct BrowserEndpoint
{
    [JsonPropertyName("webSocketDebuggerUrl")]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string? WebSocketDebuggerUrl { get; set; }

    private const string CefDebuggingUrl = "http://localhost";

    internal static async Task<BrowserEndpoint> GetBrowserEndpointAsync()
    {
        if (!Steam.IsSteamRunning)
        {
            Log.Logger.Error("Could not fetch browser, Steam is not running");
            throw new NullReferenceException();
        }
        try
        {
            return await $"{CefDebuggingUrl}:{Settings.Default.SteamCefPort}".AppendPathSegments("json", "version").GetJsonAsync<BrowserEndpoint>();
        }
        catch (FlurlHttpException e)
        {
            var cmdLine = Steam.GetCommandLine();
            if (cmdLine.Count == 0)
            {
                Log.Logger.Error("Could not fetch browser, is Steam running with -cef-enable-debugging ?");
            }
            else if (!cmdLine.Contains("-cef-enable-debugging"))
            {
                Log.Logger.Error("Could not fetch browser, Steam is not running with -cef-enable-debugging");
            }
            else
            {
                Log.Logger.Error("Could not fetch browser, SFP either tried to inject too early or another service is running on port 8080");
            }
            Log.Logger.Debug(e);
            throw;
        }
    }
}