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
        var patches = PatchConfig.DefaultPatches;

        if (frame.Url.StartsWith("https://steamloopback.host"))
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
                if (patch.MatchRegexString.ToLower() == "friends")
                {
                    try
                    {
                        if (await frame.QuerySelectorAsync(@".friendsui-container") == null)
                        {
                            continue;
                        }
                        await InjectAsync(frame, patch.TargetFile, "Friends and Chat");
                        return;
                    }
                    catch (PuppeteerException e)
                    {
                        Log.Logger.Error("Unexpected error when trying to query frame selector");
                        Log.Logger.Debug("url: " + frame.Url);
                        Log.Logger.Debug(e);
                    }
                }
                else if (patch.MatchRegex.IsMatch(title))
                {
                    await InjectAsync(frame, patch.TargetFile, title);
                    return;
                }
            }
        }
        else
        {
            var httpPatches = patches.Where(p => p.MatchRegexString.ToLower().StartsWith("http"));
            var patchEntries = httpPatches as PatchEntry[] ?? httpPatches.ToArray();
            if (patchEntries.Any(p => p.MatchRegex.IsMatch(frame.Url)))
            {
                foreach (var patch in patchEntries)
                {
                    var url = GetDomainRegex().Match(frame.Url).Groups[1].Value;
                    await InjectAsync(frame, patch.TargetFile, url);
                    return;
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

    private static async Task InjectAsync(Frame frame, string fileRelativePath, string tabFriendlyName)
    {
        if (Properties.Settings.Default.InjectCSS)
        {
            await InjectResourceAsync(frame, fileRelativePath, tabFriendlyName, "css");
        }

        if (Properties.Settings.Default.InjectJS)
        {
            if (frame.Url.StartsWith("http"))
            {
                await SetBypassCsp(frame);
            }
            await InjectResourceAsync(frame, fileRelativePath, tabFriendlyName, "js");
        }
    }

    private static async Task InjectResourceAsync(Frame frame, string fileRelativePath, string tabFriendlyName, string resourceType)
    {
        var isUrl = frame.Url.StartsWith("http") && !frame.Url.StartsWith("https://steamloopback.host");
        var injectString =
            $@"
            function inject() {{
                if (document.getElementById('{frame.Id}{resourceType}') !== null) return;
                const element = document.createElement('{(resourceType == "css" ? "link" : "script")}');
                element.id = '{frame.Id}{resourceType}';
                {(resourceType == "css" ? "element.rel = 'stylesheet';" : "")}
                element.type = '{(resourceType == "css" ? "text/css" : "text/javascript")}';
                element.{(resourceType == "css" ? "href" : "src")} = 'https://steamloopback.host/{fileRelativePath}.{resourceType}';
                document.head.append(element);
            }}
            if ((document.readyState === 'loading') && '{isUrl}' === 'True') {{
                addEventListener('DOMContentLoaded', inject);
            }} else {{
                inject();
            }}
            ";
        try
        {
            await frame.EvaluateExpressionAsync(injectString);
            Log.Logger.Info($"Injected {resourceType.ToUpper()} into {tabFriendlyName}");
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

    [GeneratedRegex(@"^(?:https?:\/\/)?(?:[^@\/\n]+@)?(?:www\.)?([^:\/?\n]+)")]
    private static partial Regex GetDomainRegex();
}
