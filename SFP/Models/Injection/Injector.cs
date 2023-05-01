#region

using PuppeteerSharp;

#endregion

namespace SFP.Models.Injection;

public static class Injector
{
    private static PuppeteerSharp.Browser? s_browser;
    private static readonly bool webkit_hooked = false;

    public static async Task InjectAsync()
    {
        try
        {
            string browser = (await Browser.GetBrowserAsync()).WebSocketDebuggerUrl!;
            ConnectOptions options = new() { BrowserWSEndpoint = browser, DefaultViewport = null };

            Log.Logger.Info("Connecting to " + browser);
            s_browser = await Puppeteer.ConnectAsync(options);
            Log.Logger.Info("Connected");
            Target[]? targets = s_browser.Targets();
            Log.Logger.Info("Found " + targets.Length + " targets");
            foreach (Target? target in targets)
            {
                Page? page = await target.PageAsync();
                await ProcessPage(page);
            }

            s_browser.TargetCreated += Browser_TargetUpdate;
            s_browser.TargetChanged += Browser_TargetUpdate;
        }
        catch (Exception e)
        {
            Log.Logger.Error(e);
        }
    }

    private static async void Browser_TargetUpdate(object? sender, TargetChangedArgs e)
    {
        Page? page = await e.Target.PageAsync();
        await ProcessPage(page);
    }

    private static async Task ProcessPage(Page? page)
    {
        if (page == null)
        {
            return;
        }

        if (page.Url.Contains("store.steampowered.com") || page.Url.Contains("steamcommunity.com") || page.Url.Contains("!--/library/"))
        {
            if (!webkit_hooked)
            {
                page.DOMContentLoaded += Page_Load;
            }
            await InjectCssAsync(page, "webkit.css", "Steam web", client: false);
            return;
        }

        string? title = await page.GetTitleAsync();

        if (title == "Steam Big Picture Mode" || title.StartsWith("QuickAccess_") || title.StartsWith("MainMenu_") ||
            title.StartsWith(@"notificationtoasts_"))
        {
            await InjectCssAsync(page, @"bigpicture.custom.css", "Steam Big Picture Mode", silent: true);
            return;
        }


        if (title == "Steam" || title.EndsWith("Menu") || title.EndsWith(@"Supernav"))
        {
            await InjectCssAsync(page, @"libraryroot.custom.css", "Steam client");
        }
        else if (title.StartsWith("Friends List"))
        {
            await InjectCssAsync(page, "friends.custom.css", "Friends List");
        }
    }

    private static async void Page_Load(object? sender, EventArgs _)
    {
        if (sender is Page page)
        {
            await InjectCssAsync(page, "webkit.css", "Steam web", client: false);
        }
    }

    private static async Task InjectCssAsync(Page page, string cssFileRelativePath, string tabFriendlyName,
        bool retry = true, bool silent = false, bool client = true)
    {
        string cssInjectString =
            $$"""
                function injectCss() {
                    if (document.getElementById('{{page.Target.TargetId}}') !== null) return;
                    const style = document.createElement('style');
                    style.id = '{{page.Target.TargetId}}';
                    document.head.append(style);
                    style.textContent = `@import url('https://steamloopback.host/{{cssFileRelativePath}}');`;
                }
                if ((document.readyState === 'loading') && '{{!client}}' === 'True') {
                    addEventListener('DOMContentLoaded', injectCss);
                } else {
                    injectCss();
                }
                """;
        try
        {
            await page.EvaluateExpressionAsync(cssInjectString);
            if (!silent)
            {
                Log.Logger.Info("Injected into " + tabFriendlyName);
            }
        }
        catch (EvaluationFailedException e)
        {
            if (!silent)
            {
                Log.Logger.Error(tabFriendlyName + " failed to inject: " + e);
                Log.Logger.Info("Retrying...");
            }

            if (retry)
            {
                await InjectCssAsync(page, cssFileRelativePath, tabFriendlyName, false, silent, client);
            }
        }
    }
}
