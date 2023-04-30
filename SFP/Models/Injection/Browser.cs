using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl;
using Flurl.Http;
using Websocket.Client;

namespace SFP.Models.Injection;

public struct Browser
{
    [JsonPropertyName("webSocketDebuggerUrl")]
    public string? WebSocketDebuggerUrl { get; set; }

    private readonly ManualResetEvent _exitEvent = new(false);

    public Browser()
    {
        WebSocketDebuggerUrl = null;
    }

    internal async Task MonitorTargetsAsync()
    {
        string request = JsonSerializer.Serialize(new
        {
            id = 1,
            method = "Target.setDiscoverTargets",
            @params = new { discover = true }
        });
        _exitEvent.Reset();
        var url = new Uri(WebSocketDebuggerUrl!);

        var factory = new Func<ClientWebSocket>(() => new ClientWebSocket
        {
            Options =
            {
                KeepAliveInterval = TimeSpan.Zero
            }
        });

        using var client = new WebsocketClient(url, factory);
        client.IsReconnectionEnabled = false;
        Browser instance = this;
        client.DisconnectionHappened.Subscribe(info =>
        {
            Log.Logger.Info($"Disconnect happened, type: {info.Type}");
            instance.StopMonitoringTargets();
        });

        client.MessageReceived.Subscribe(OnClientMessageReceived);
        await client.Start();

        await Task.Run(() => client.Send($"{request}"));

        Log.Logger.Info("Monitoring targets");

        _exitEvent.WaitOne();
    }

    internal void StopMonitoringTargets()
    {
        _exitEvent.Set();
    }

    internal static void ProcessTargetInfo(TargetInfo targetInfo)
    {
        Log.Logger.Info(targetInfo);
    }

    [SuppressMessage("CodeSmell", "ERP022:Unobserved exception in a generic exception handler")]
    internal void OnClientMessageReceived(ResponseMessage msg)
    {
        try
        {
            TargetEvent? response = JsonSerializer.Deserialize<TargetEvent>(msg.Text);
            if (response == null)
            {
                return;
            }

            if (response.Params.TargetInfo.Type != "page" ||
                !string.IsNullOrWhiteSpace(response.Params.TargetInfo.Title) &&
                string.IsNullOrWhiteSpace(response.Params.TargetInfo.Url))
            {
                return;
            }

            ProcessTargetInfo(response.Params.TargetInfo);
        }
        catch (Exception)
        {
            // ignore
        }
    }
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
