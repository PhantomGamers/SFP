#region

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Websocket.Client;

#endregion

namespace SFP.Models.Injection;

public struct Tab
{
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("title")] public string Title { get; set; }

    [JsonPropertyName("url")] public string Url { get; set; }

    [JsonPropertyName("webSocketDebuggerUrl")]
    public string WebSocketDebuggerUrl { get; set; }

    [SuppressMessage("CodeSmell", "EPC13:Suspiciously unobserved result.")]
    public async Task EvaluateJavaScript(string javaScript)
    {
        Log.Logger.Info(Environment.NewLine + javaScript);
        using WebsocketClient client = new(new Uri(WebSocketDebuggerUrl));
        await client.Start();
        string evalString = $$"""
        {
            "id": 1,
            "method": "Runtime.evaluate",
            "params": {
              "expression": {{javaScript}},
              "userGesture": True,
              "awaitPromise": False
            }
        }
        """;

        string navString = $$"""
        {
           "id": 1,
           "method": "Page.navigate",
           "params": {
             "url": "https://www.google.com"
           }
        }
        """;

        Log.Logger.Info("evalString" + Environment.NewLine + evalString);
        await client.SendInstant(evalString);
        //Log.Logger.Info("navString" + Environment.NewLine + navString);
        //await client.SendInstant(navString);
    }
}
