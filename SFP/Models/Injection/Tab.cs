#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl;
using Flurl.Http;

// ReSharper disable MemberCanBePrivate.Global

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
    public async Task<string> EvaluateJavaScriptAsync(string javaScript)
    {
        using ClientWebSocket ws = new();
        CancellationTokenSource cts = new();
        Stopwatch stopwatch = Stopwatch.StartNew();
        await ws.ConnectAsync(new Uri(WebSocketDebuggerUrl), cts.Token);
        stopwatch.Stop();
        Log.Logger.Info("Connected to " + WebSocketDebuggerUrl + " in " + stopwatch.ElapsedMilliseconds + "ms");
        string request = JsonSerializer.Serialize(new
        {
            id = 1,
            method = "Runtime.evaluate",
            @params = new { expression = javaScript, userGesture = true }
        });
        await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(request)), WebSocketMessageType.Text, true,
            cts.Token);
        byte[] buffer = new byte[1024];
        WebSocketReceiveResult response = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
        return Encoding.UTF8.GetString(buffer, 0, response.Count);
    }

    public async Task InjectCssAsync(string cssFileRelativePath, string tabFriendlyName)
    {
        string cssInjectString =
            $$"""
                (function() {
                    if (document.getElementById('{{Id}}') !== null) return;
                    const style = document.createElement('style');
                    style.id = '{{Id}}';
                    document.head.append(style);
                    style.textContent = `@import url('https://steamloopback.host/{{cssFileRelativePath}}');`;
                })()
                """;
        await EvaluateJavaScriptAsync(cssInjectString);
        Log.Logger.Info("Applied custom style to " + tabFriendlyName);
    }
}
