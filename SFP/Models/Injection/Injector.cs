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


        if (!Properties.Settings.Default.InjectJS && !Properties.Settings.Default.InjectCSS)
        {
            Log.Logger.Warn("No injection type is enabled, skipping injection");
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

        await ProcessFrame(page.MainFrame);
    }

    private static async Task ProcessFrame(Frame frame)
    {
        if (frame.Url.StartsWith("https://store.steampowered.com") ||
            frame.Url.StartsWith("https://steamcommunity.com"))
        {
            await InjectAsync(frame, "webkit", "Steam web", client: false);
            return;
        }

        string? title;
        try
        {
            title = await frame.GetTitleAsync();
        }
        catch (PuppeteerException e)
        {
            Log.Logger.Error("Unexpected error when trying to get frame title");
            Log.Logger.Debug("url: " + frame.Url);
            Log.Logger.Debug(e);
            return;
        }

        if (title == "Steam" || title == "Steam Settings" || title == "Sign in to Steam" || title == "GameOverview" || title == "Shutdown" || title == "OverlayBrowser_Browser" || title == "What's New Settings" || title.EndsWith("Menu") ||
            title.EndsWith(@"Supernav") || title.StartsWith("SP Overlay:") || title.StartsWith(@"notificationtoasts_"))
        {
            await InjectAsync(frame, @"libraryroot.custom", "Steam client");
            return;
        }

        if (title == "Steam Big Picture Mode" || title.StartsWith("QuickAccess_") || title.StartsWith("MainMenu_"))
        {
            await InjectAsync(frame, @"bigpicture.custom", "Steam Big Picture Mode");
            return;
        }

        try
        {
            if (await frame.QuerySelectorAsync(@".friendsui-container") != null)
            {
                await InjectAsync(frame, "friends.custom", "Friends and Chat");
            }
        }
        catch (PuppeteerException e)
        {
            Log.Logger.Error("Unexpected error when trying to query frame selector");
            Log.Logger.Debug("url: " + frame.Url);
            Log.Logger.Debug(e);
        }
    }

    private static async void Frame_Navigate(object? sender, FrameEventArgs e)
    {
        await ProcessFrame(e.Frame);
    }

    private static async Task InjectAsync(Frame frame, string fileRelativePath, string tabFriendlyName, bool client = true)
    {
        if (Properties.Settings.Default.InjectCSS)
        {
            await InjectCssAsync(frame, fileRelativePath, tabFriendlyName, client: client);
        }

        if (Properties.Settings.Default.InjectJS)
        {
            await InjectJsAsync(frame, fileRelativePath, tabFriendlyName, client: client);
        }
    }

    private static async Task InjectCssAsync(Frame frame, string cssFileRelativePath, string tabFriendlyName,
        bool retry = true, bool silent = false, bool client = true)
    {
        var cssInjectString =
            $$"""
                function injectCss() {
                    if (document.getElementById('{{frame.Id}}css') !== null) return;
                    const link = document.createElement('link');
                    link.id = '{{frame.Id}}css';
                    link.rel = 'stylesheet';
                    link.type = 'text/css';
                    link.href = 'https://steamloopback.host/{{cssFileRelativePath}}.css';
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
                Log.Logger.Info("Injected CSS into " + tabFriendlyName);
            }
        }
        catch (EvaluationFailedException e)
        {
            if (!silent && tabFriendlyName != "Steam web")
            {
                Log.Logger.Error(tabFriendlyName + " failed to inject css: " + e);
                Log.Logger.Info("Retrying...");
            }

            if (retry)
            {
                await InjectCssAsync(frame, cssFileRelativePath, tabFriendlyName, false, silent, client);
            }
        }
    }

    private static async Task InjectJsAsync(Frame frame, string jsFileRelativePath, string tabFriendlyName,
        bool retry = true, bool silent = false, bool client = true)
    {
        var jsInjectString =
            $$"""
                function injectJs() {
                    if (document.getElementById('{{frame.Id}}js') !== null) return;
                    const script = document.createElement('script');
                    script.id = '{{frame.Id}}js';
                    script.type = 'text/javascript';
                    script.src = 'https://steamloopback.host/{{jsFileRelativePath}}.js';
                    document.head.append(script);
                }
                if ((document.readyState === 'loading') && '{{!client}}' === 'True') {
                    addEventListener('DOMContentLoaded', injectJs);
                } else {
                    injectJs();
                }
                """;
        try
        {
            await frame.EvaluateExpressionAsync(jsInjectString);
            if (!silent)
            {
                Log.Logger.Info("Injected JS into " + tabFriendlyName);
            }
        }
        catch (EvaluationFailedException e)
        {
            if (!silent && tabFriendlyName != "Steam web")
            {
                Log.Logger.Error(tabFriendlyName + " failed to inject js: " + e);
                Log.Logger.Info("Retrying...");
            }

            if (retry)
            {
                await InjectCssAsync(frame, jsFileRelativePath, tabFriendlyName, false, silent, client);
            }
        }
    }
}
