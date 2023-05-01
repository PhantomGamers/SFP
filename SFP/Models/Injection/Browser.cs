#region

using System.Text.Json.Serialization;
using Flurl;
using Flurl.Http;

#endregion

namespace SFP.Models.Injection;

public struct Browser
{
    [JsonPropertyName("webSocketDebuggerUrl")]
    public string? WebSocketDebuggerUrl { get; set; }

    internal static async Task<Browser> GetBrowserAsync()
    {
        try
        {
            return await Injector.CefDebuggingUrl.AppendPathSegments("json", "version").GetJsonAsync<Browser>();
        }
        catch (FlurlHttpException e)
        {
            Log.Logger.Error("Could not fetch browser, is Steam running with CEF debugging enabled?");
            Log.Logger.Debug(e);
            throw;
        }
    }
}
