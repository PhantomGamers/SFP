#region

using PuppeteerSharp;

#endregion

namespace SFP.Models.Injection;

public static class Injector
{
    private static Browser? s_browser;
    private static bool s_isInjected;
    private static readonly SemaphoreSlim s_semaphore = new(1, 1);
    public static bool IsInjected => s_isInjected && s_browser != null;

    public static event EventHandler? InjectionStateChanged;

    public static async Task StartInjectionAsync(bool noError = false)
    {
        if (s_browser is { IsConnected: true })
        {
            return;
        }

        if (!await s_semaphore.WaitAsync(TimeSpan.Zero))
        {
            return;
        }

        try
        {
            if (File.Exists(Steam.MillenniumPath))
            {
                Log.Logger.Warn("Millennium is already injected, skipping injection");
                return;
            }
            var browserEndpoint = (await BrowserEndpoint.GetBrowserEndpointAsync()).WebSocketDebuggerUrl!;
            ConnectOptions options = new()
            {
                BrowserWSEndpoint = browserEndpoint,
                DefaultViewport = null,
                EnqueueAsyncMessages = false,
                EnqueueTransportMessages = false
            };

            Log.Logger.Info("Connecting to " + browserEndpoint);
            s_browser = await Puppeteer.ConnectAsync(options);
            s_browser.Disconnected += OnDisconnected;
            Log.Logger.Info("Connected");
            s_browser.TargetCreated += Browser_TargetUpdate;
            s_browser.TargetChanged += Browser_TargetUpdate;
            await InjectAsync();
            s_isInjected = true;
            InjectionStateChanged?.Invoke(null, EventArgs.Empty);
            Log.Logger.Info("Injection finished");
        }
        catch (Exception e)
        {
            StopInjection();
            if (noError)
            {
                return;
            }

            Log.Logger.Error(e);
        }
        finally
        {
            s_semaphore.Release();
        }
    }

    private static async Task InjectAsync()
    {
        if (s_browser == null)
        {
            Log.Logger.Warn("Inject was called but CEF instance is not connected");
            return;
        }

        var targets = s_browser.Targets();
        Log.Logger.Info("Found " + targets.Length + " targets");
        foreach (var page in targets.Select(async t => await t.PageAsync()))
        {
            await ProcessPage(await page);
        }
    }

    public static void StopInjection()
    {
        if (s_browser?.IsConnected ?? false)
        {
            Log.Logger.Info("Disconnecting from Steam instance");
        }
        s_isInjected = false;
        s_browser?.Disconnect();
        s_browser = null;
        InjectionStateChanged?.Invoke(null, EventArgs.Empty);
    }

    private static void OnDisconnected(object? sender, EventArgs e)
    {
        Log.Logger.Info("Disconnected from Steam instance");
        StopInjection();
    }

    private static async void Browser_TargetUpdate(object? sender, TargetChangedArgs e)
    {
        try
        {
            var page = await e.Target.PageAsync();
            await ProcessPage(page);
        }
        catch (EvaluationFailedException err)
        {
            Log.Logger.Warn("Evaluation failed exception when trying to get page");
            Log.Logger.Debug(err);
        }
        catch (PuppeteerException err)
        {
            Log.Logger.Warn("Puppeteer exception when trying to get page");
            Log.Logger.Debug(err);
        }
    }

    private static async Task ProcessPage(Page? page)
    {
        if (page == null)
        {
            return;
        }

        page.FrameNavigated -= Frame_Navigate;
        page.FrameNavigated += Frame_Navigate;

        string? title;
        try
        {
            title = await page.GetTitleAsync();
        }
        catch (Exception e)
        {
            Log.Logger.Error("Unexpected error when trying to get page title");
            Log.Logger.Debug("url: " + page.Url);
            Log.Logger.Debug(e);
            return;
        }

        if (title == "Steam Big Picture Mode" || title.StartsWith("QuickAccess_") || title.StartsWith("MainMenu_") ||
            title.StartsWith(@"notificationtoasts_"))
        {
            await InjectCssAsync(page, @"bigpicture.custom.css", "Steam Big Picture Mode");
            return;
        }


        if (title == "Steam" || title == "Steam Settings" || title == "GameOverview" || title.EndsWith("Menu") ||
            title.EndsWith(@"Supernav") || title.StartsWith("SP Overlay:"))
        {
            await InjectCssAsync(page, @"libraryroot.custom.css", "Steam client");
            return;
        }

        if (await page.QuerySelectorAsync(@".friendsui-container") != null)
        {
            await InjectCssAsync(page, "friends.custom.css", "Friends and Chat");
        }
    }

    private static async void Frame_Navigate(object? sender, FrameEventArgs e)
    {
        if (!e.Frame.Url.StartsWith("https://store.steampowered.com") &&
            !e.Frame.Url.StartsWith("https://steamcommunity.com"))
        {
            return;
        }

        await InjectCssAsync(e.Frame, "webkit.css", "Steam web", client: false);
    }

    private static async Task InjectCssAsync(Page page, string cssFileRelativePath, string tabFriendlyName,
        bool retry = true, bool silent = false, bool client = true)
    {
        var frame = page.MainFrame;
        await InjectCssAsync(frame, cssFileRelativePath, tabFriendlyName, retry, silent, client);
    }

    private static async Task InjectCssAsync(Frame frame, string cssFileRelativePath, string tabFriendlyName,
        bool retry = true, bool silent = false, bool client = true)
    {
        var cssInjectString =
            $$"""
                function injectCss() {
                    if (document.getElementById('{{frame.Id}}') !== null) return;
                    const link = document.createElement('link');
                    link.id = '{{frame.Id}}';
                    link.rel = 'stylesheet';
                    link.type = 'text/css';
                    link.href = 'https://steamloopback.host/{{cssFileRelativePath}}';
                    document.head.append(link);
                }
                if ((document.readyState === 'loading') && '{{!client}}' === 'True') {
                    addEventListener('DOMContentLoaded', injectCss);
                } else {
                    injectCss();
                }
                """;
        try
        {
            await frame.EvaluateExpressionAsync(cssInjectString);
            if (!silent)
            {
                Log.Logger.Info("Injected into " + tabFriendlyName);
            }
        }
        catch (EvaluationFailedException e)
        {
            if (!silent && tabFriendlyName != "Steam web")
            {
                Log.Logger.Error(tabFriendlyName + " failed to inject: " + e);
                Log.Logger.Info("Retrying...");
            }

            if (retry)
            {
                await InjectCssAsync(frame, cssFileRelativePath, tabFriendlyName, false, silent, client);
            }
        }
    }
}
