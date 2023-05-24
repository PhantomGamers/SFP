#region

using System.Text.RegularExpressions;
using PuppeteerSharp;
using SFP.Models.Injection.Config;

#endregion

namespace SFP.Models.Injection;

public static partial class Injector
{
    private static Browser? s_browser;
    private static bool s_isInjected;
    private static bool s_manualDisconnect;
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
            Log.Logger.Info("Initial injection finished");
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

        var pages = await s_browser.PagesAsync();
        Log.Logger.Info("Found " + pages.Length + " pages");

        var processTasks = pages.Select(ProcessPage);

        await Task.WhenAll(processTasks);
    }

    public static void StopInjection()
    {
        if (s_browser?.IsConnected ?? false)
        {
            Log.Logger.Info("Disconnecting from Steam instance");
        }
        s_isInjected = false;
        s_manualDisconnect = true;
        s_browser?.Disconnect();
        s_browser = null;
        InjectionStateChanged?.Invoke(null, EventArgs.Empty);
    }

    // injection after reload occurs before content is fully loaded, needs investigation
    public static async void Reload()
    {
        if (s_browser == null)
        {
            return;
        }

        var pages = await s_browser.PagesAsync();
        foreach (var page in pages)
        {
            try
            {
                var title = await page.MainFrame.GetTitleAsync();
                if (title != "SharedJSContext")
                {
                    continue;
                }
                await page.ReloadAsync();
                break;
            }
            catch (PuppeteerException)
            {
                // ignored
            }
        }
    }

    private static async void OnDisconnected(object? sender, EventArgs e)
    {
        Log.Logger.Info("Disconnected from Steam instance");
        var manualDisconnect = s_manualDisconnect;
        StopInjection();
        if (manualDisconnect)
        {
            s_manualDisconnect = false;
            return;
        }

        await Task.Delay(500);
        if (!Steam.IsSteamWebHelperRunning)
        {
            return;
        }
        Log.Logger.Warn("Unexpected disconnect, trying to reconnect to Steam instance");
        await Steam.TryInject();
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
        var config = SfpConfig.GetConfig();
        var patches = config.Patches as PatchEntry[] ?? config.Patches.ToArray();

        if (!IsFrameWebkit(frame))
        {
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

            foreach (var patch in patches)
            {
                var regex = patch.MatchRegexString;
                if (regex.StartsWith('.') || regex.StartsWith('#') || regex.StartsWith('['))
                {
                    try
                    {
                        if (await frame.QuerySelectorAsync(patch.MatchRegexString) == null)
                        {
                            continue;
                        }
                        await InjectAsync(frame, patch, title);
                        return;
                    }
                    catch (PuppeteerException e)
                    {
                        Log.Logger.Error("Unexpected error when trying to query frame selector");
                        Log.Logger.Debug("url: " + frame.Url);
                        Log.Logger.Debug(e);
                    }
                }
                else
                {
                    switch (config._isFromMillennium)
                    {
                        case false when patch.MatchRegex.IsMatch(title):
                        case true when regex == title:
                            await InjectAsync(frame, patch, title);
                            return;
                    }
                }
            }
        }
        else
        {
            // needed to accept including css and js from steamloopback.host
            // only needed for css in certain instances, needs investigation
            await SetBypassCsp(frame);
            var url = GetDomainRegex().Match(frame.Url).Groups[1].Value;
            if (!config._isFromMillennium)
            {
                var httpPatches = patches.Where(p => p.MatchRegexString.ToLower().StartsWith("http"));
                var patchEntries = httpPatches as PatchEntry[] ?? httpPatches.ToArray();
                if (patchEntries.Any(p => p.MatchRegex.IsMatch(frame.Url)))
                {
                    foreach (var patch in patchEntries)
                    {
                        await InjectAsync(frame, patch, url);
                        return;
                    }
                }
            }
            else
            {
                if (patches.Any(p => p.MatchRegex.IsMatch(frame.Url)))
                {
                    foreach (var patch in patches)
                    {
                        await InjectAsync(frame, patch, url);
                        return;
                    }
                }
            }
        }
    }

    private static async Task SetBypassCsp(Frame frame)
    {
        var pageTask = s_browser?.Targets().FirstOrDefault(t => t.TargetId == frame.Id)?.PageAsync();
        if (pageTask == null)
        {
            return;
        }
        var page = await pageTask;
        if (page == null)
        {
            return;
        }
        try
        {
            await page.SetBypassCSPAsync(true);
        }
        catch (PuppeteerException e)
        {
            Log.Logger.Warn("Failed to bypass content security policy");
            Log.Logger.Debug(e);
        }
    }

    private static async void Frame_Navigate(object? sender, FrameEventArgs e)
    {
        await ProcessFrame(e.Frame);
    }

    private static async Task InjectAsync(Frame frame, PatchEntry patch, string tabFriendlyName)
    {
        if (Properties.Settings.Default.InjectCSS)
        {
            if (string.IsNullOrWhiteSpace(patch.TargetCss) || !patch.TargetCss.EndsWith(".css"))
            {
                Log.Logger.Info("Target CSS file does not end in .css for patch " + patch.MatchRegexString);
            }
            else
            {
                await InjectResourceAsync(frame, patch.TargetCss, tabFriendlyName, patch.MatchRegexString);
            }
        }

        if (Properties.Settings.Default.InjectJS)
        {
            if (string.IsNullOrWhiteSpace(patch.TargetJs) || !patch.TargetJs.EndsWith(".js"))
            {
                Log.Logger.Info("Target Js file does not end in .js for patch " + patch.MatchRegexString);
            }
            else
            {
                await InjectResourceAsync(frame, patch.TargetJs, tabFriendlyName, patch.MatchRegexString);
            }
        }
    }

    private static async Task InjectResourceAsync(Frame frame, string fileRelativePath, string tabFriendlyName, string patchName)
    {
        var relativeSkinDir = Steam.GetRelativeSkinDir().Replace('\\', '/');
        if (!string.IsNullOrWhiteSpace(relativeSkinDir))
        {
            relativeSkinDir += '/';
        }
        var resourceType = fileRelativePath.EndsWith(".css") ? "css" : "js";
        fileRelativePath = $"{relativeSkinDir}{fileRelativePath}";

        var injectString =
            $@"function inject() {{
    if (document.getElementById('{frame.Id}{resourceType}') !== null) return;
    const element = document.createElement('{(resourceType == "css" ? "link" : "script")}');
    element.id = '{frame.Id}{resourceType}';
    {(resourceType == "css" ? "element.rel = 'stylesheet';" : "")}
    element.type = '{(resourceType == "css" ? "text/css" : "module")}';
    element.{(resourceType == "css" ? "href" : "src")} = 'https://steamloopback.host/{fileRelativePath}';
    document.head.append(element);
}}
if ((document.readyState === 'loading') && '{IsFrameWebkit(frame)}' === 'True') {{
    addEventListener('DOMContentLoaded', inject);
}} else {{
    inject();
}}
";
        try
        {
            if (!IsFrameWebkit(frame) && resourceType == "js")
            {
                await Task.Delay(500);
            }
            await frame.EvaluateExpressionAsync(injectString);
            Log.Logger.Info($"Injected {fileRelativePath} into {tabFriendlyName} from patch {patchName}");
        }
        catch (PuppeteerException e)
        {
            if (!tabFriendlyName.StartsWith("http"))
            {
                Log.Logger.Error($"Failed to inject {resourceType} into {tabFriendlyName}");
                Log.Logger.Debug(e);
            }
        }
    }

    private static bool IsFrameWebkit(Frame frame)
    {
        return !frame.Url.StartsWith("https://steamloopback.host");
    }

    [GeneratedRegex(@"^(?:https?:\/\/)?(?:[^@\/\n]+@)?(?:www\.)?([^:\/?\n]+)")]
    private static partial Regex GetDomainRegex();
}
