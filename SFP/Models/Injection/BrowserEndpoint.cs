#region

using System.Text.Json.Serialization;
using Flurl;
using Flurl.Http;

#endregion

namespace SFP.Models.Injection;

public struct BrowserEndpoint
{
    [JsonPropertyName("webSocketDebuggerUrl")]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string? WebSocketDebuggerUrl { get; set; }

    private const string CefDebuggingUrl = "http://localhost:8080";

    internal static async Task<BrowserEndpoint> GetBrowserEndpointAsync()
    {
        try
        {
            return await CefDebuggingUrl.AppendPathSegments("json", "version").GetJsonAsync<BrowserEndpoint>();
        }
        catch (FlurlHttpException e)
        {
            Log.Logger.Error("Could not fetch browser, is Steam running with -cef-enable-debugging ?");
            Log.Logger.Debug(e);
            throw;
        }
    }
}
