#region

using System.Text;
using System.Text.RegularExpressions;

using PuppeteerSharp;

using SFP.Models.Injection.Config;
using SFP.Properties;

#endregion

namespace SFP.Models.Injection;

public static partial class Injector
{
    private static IBrowser? s_browser;
    private static bool s_manualDisconnect;
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    public static bool IsInjected { get => field && s_browser != null; private set; }

    private static string PreferredColorScheme { get; set; } = "light";

    public static string[] ColorNames { get; } =
    [
        "SystemAccentColor",
        "SystemAccentColorLight1",
        "SystemAccentColorLight2",
        "SystemAccentColorLight3",
        "SystemAccentColorDark1",
        "SystemAccentColorDark2",
        "SystemAccentColorDark3"
    ];

    private static string ColorsCss { get; set; } = string.Empty;

    public static event EventHandler? InjectionStateChanged;

    public static async Task StartInjectionAsync(bool noError = false)
    {
        if (s_browser is { IsConnected: true })
        {
            Log.Logger.Warn("Injection already started, skipping injection");
            return;
        }

        if (!await Semaphore.WaitAsync(TimeSpan.Zero))
        {
            Log.Logger.Warn("Injection already in progress, skipping injection");
            return;
        }

        if (!Settings.Default.InjectJS && !Settings.Default.InjectCSS)
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

            string browserEndpoint = (await BrowserEndpoint.GetBrowserEndpointAsync()).WebSocketDebuggerUrl!;
            ConnectOptions options = new()
            {
                BrowserWSEndpoint = browserEndpoint,
                DefaultViewport = null,
                EnqueueAsyncMessages = true,
                EnqueueTransportMessages = true
            };

            Log.Logger.Info("Connecting to " + browserEndpoint);
            s_browser = await Puppeteer.ConnectAsync(options);
            s_browser.Disconnected += OnDisconnected;
            Log.Logger.Info("Connected");
            s_browser.TargetCreated += Browser_TargetUpdate;
            s_browser.TargetChanged += Browser_TargetUpdate;
            await InjectAsync();
            IsInjected = true;
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
            Semaphore.Release();
        }
    }

    private static async Task InjectAsync()
    {
        if (s_browser == null)
        {
            Log.Logger.Warn("Inject was called but CEF instance is not connected");
            return;
        }

        IPage[] pages = await s_browser.PagesAsync();
        Log.Logger.Info("Found " + pages.Length + " pages");

        _ = SfpConfig.GetConfig();
        IEnumerable<Task> processTasks = pages.Select(ProcessPage);

        await Task.WhenAll(processTasks);
    }

    public static void StopInjection()
    {
        if (s_browser?.IsConnected ?? false)
        {
            Log.Logger.Info("Disconnecting from Steam instance");
        }

        IsInjected = false;
        s_manualDisconnect = true;
        s_browser?.Disconnect();
        s_browser = null;
        InjectionStateChanged?.Invoke(null, EventArgs.Empty);
    }

    // injection after reload occurs before content is fully loaded, needs investigation
    public static async Task Reload()
    {
        if (s_browser == null)
        {
            return;
        }

        IPage[] pages = await s_browser.PagesAsync();
        foreach (IPage page in pages)
        {
            try
            {
                string title = await page.MainFrame.GetTitleAsync();
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

#pragma warning disable EPC27
    private static async void OnDisconnected(object? sender, EventArgs e)
#pragma warning restore EPC27
    {
        try
        {
            Log.Logger.Info("Disconnected from Steam instance");
            bool manualDisconnect = s_manualDisconnect;
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
        catch (Exception ex)
        {
            Log.Logger.Error("Error in OnDisconnected event handler");
            Log.Logger.Debug(ex);
        }
    }

#pragma warning disable EPC27
    private static async void Browser_TargetUpdate(object? sender, TargetChangedArgs e)
#pragma warning restore EPC27
    {
        try
        {
            IPage page = await e.Target.PageAsync();
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
        catch (Exception err)
        {
            Log.Logger.Error("Unexpected error in Browser_TargetUpdate event handler");
            Log.Logger.Debug(err);
        }
    }

    private static async Task ProcessPage(IPage? page)
    {
        if (page == null)
        {
            return;
        }

        if (Settings.Default.UseAppTheme)
        {
            await UpdateColorInPage(page);
            await UpdateSystemAccentColorsInPage(page);
        }

        page.FrameNavigated -= Frame_Navigate;
        page.FrameNavigated += Frame_Navigate;

        await ProcessFrame(page.MainFrame);
    }

    private static async Task ProcessFrame(IFrame frame)
    {
        SfpConfig config = SfpConfig.GetConfig();
        PatchEntry[] patches = config.Patches as PatchEntry[] ?? [.. config.Patches];

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

            if (frame.Url.StartsWith("devtools://", StringComparison.InvariantCultureIgnoreCase))
            {
                title = frame.Url;
            }

            await DumpFrame(frame, title);

            foreach (PatchEntry patch in patches)
            {
                string regex = patch.MatchRegexString;
                if (title.Equals("SharedJSContext", StringComparison.InvariantCultureIgnoreCase) &&
                    !regex.Contains("SharedJSContext", StringComparison.InvariantCultureIgnoreCase))
                {
                    // only inject into SharedJSContext when it is explicitly desired
                    continue;
                }

                if (regex.StartsWith('.') || regex.StartsWith('#') || regex.StartsWith('['))
                {
                    try
                    {
                        if (await frame.QuerySelectorAsync(regex) == null)
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
                    switch (config.IsFromMillennium)
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
            string url = GetDomainRegex().Match(frame.Url).Groups[1].Value;
            await DumpFrame(frame, url);
            if (!config.IsFromMillennium)
            {
                IEnumerable<PatchEntry> httpPatches = patches.Where(p =>
                    p.MatchRegexString.TrimStart('^').StartsWith("http", StringComparison.InvariantCultureIgnoreCase));
                PatchEntry[] patchEntries = httpPatches as PatchEntry[] ?? [.. httpPatches];
                PatchEntry? patch = patchEntries.FirstOrDefault(p => p.MatchRegex.IsMatch(frame.Url));
                if (patch != null)
                {
                    // needed to accept including css and js from steamloopback.host
                    // only needed for css in certain instances, needs investigation
                    await SetBypassCsp(frame);
                    await InjectAsync(frame, patch, url);
                }
            }
            else
            {
                PatchEntry? patch = patches.FirstOrDefault(p => p.MatchRegex.IsMatch(frame.Url));
                if (patch != null)
                {
                    await SetBypassCsp(frame);
                    await InjectAsync(frame, patch, url);
                }
            }
        }
    }

    private static async Task DumpFrame(IFrame frame, string? fileName)
    {
        if (Settings.Default.DumpPages)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                Log.Logger.Debug("Empty frame title, skipping dump");
                return;
            }

            try
            {
                Directory.CreateDirectory("dumps");
                string content = await frame.GetContentAsync();
                string dumpsPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "dumps");
                await File.WriteAllTextAsync(Path.Join(dumpsPath, fileName + ".html"), content);
            }
            catch (PuppeteerException e)
            {
                Log.Logger.Error("Unexpected error when trying to get frame content");
                Log.Logger.Debug("url: " + frame.Url);
                Log.Logger.Debug("title: " + fileName);
                Log.Logger.Debug(e);
            }
            catch (Exception e)
            {
                Log.Logger.Error(e);
            }
        }
    }

    private static async Task SetBypassCsp(IFrame frame)
    {
        Task<IPage>? pageTask = s_browser?.Targets().FirstOrDefault(t => t.TargetId == frame.Id)?.PageAsync();
        if (pageTask == null)
        {
            return;
        }

        IPage? page = await pageTask;
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

#pragma warning disable EPC27
    private static async void Frame_Navigate(object? sender, FrameEventArgs e)
#pragma warning restore EPC27
    {
        try
        {
            await ProcessFrame(e.Frame);
        }
        catch (Exception ex)
        {
            Log.Logger.Error("Error in Frame_Navigate event handler");
            Log.Logger.Debug(ex);
        }
    }

    private static async Task InjectAsync(IFrame frame, PatchEntry patch, string tabFriendlyName)
    {
        if (Settings.Default.InjectCSS && !string.IsNullOrWhiteSpace(patch.TargetCss))
        {
            if (!patch.TargetCss.EndsWith(".css"))
            {
                Log.Logger.Info("Target CSS file does not end in .css for patch " + patch.MatchRegexString);
            }
            else
            {
                await InjectResourceAsync(frame, patch.TargetCss, tabFriendlyName, patch.MatchRegexString);
            }
        }

        if (Settings.Default.InjectJS && !string.IsNullOrWhiteSpace(patch.TargetJs))
        {
            if (!patch.TargetJs.EndsWith(".js"))
            {
                Log.Logger.Info("Target Js file does not end in .js for patch " + patch.MatchRegexString);
            }
            else
            {
                await InjectResourceAsync(frame, patch.TargetJs, tabFriendlyName, patch.MatchRegexString);
            }
        }
    }

    private static async Task InjectResourceAsync(IFrame frame, string fileRelativePath, string tabFriendlyName,
        string patchName)
    {
        string relativeSkinDir = Steam.GetRelativeSkinDir().Replace('\\', '/');
        if (!string.IsNullOrWhiteSpace(relativeSkinDir))
        {
            relativeSkinDir += '/';
        }

        string resourceType = fileRelativePath.EndsWith(".css", StringComparison.InvariantCultureIgnoreCase)
            ? "css"
            : "js";
        fileRelativePath = $"{relativeSkinDir}{fileRelativePath}";
        bool isFrameWebkit = IsFrameWebkit(frame);

        string injectString =
            $$"""
                  function inject() {
                      if (document.getElementById('{{frame.Id}}{{resourceType}}') !== null) return;
                      const element = document.createElement('{{(resourceType == "css" ? "link" : "script")}}');
                      element.id = '{{frame.Id}}{{resourceType}}';
                      {{(resourceType == "css" ? "element.rel = 'stylesheet';" : "")}}
                      element.type = '{{(resourceType == "css" ? "text/css" : "module")}}';
                      element.{{(resourceType == "css" ? "href" : "src")}} = 'https://steamloopback.host/{{fileRelativePath}}';
                      document.head.append(element);
                      if ('{{isFrameWebkit}}' === 'True' && typeof SteamClient.BrowserView.RegisterForMessageFromParent !== 'undefined') {
                          fetch('https://steamloopback.host', {signal: AbortSignal.timeout(100),mode: 'no-cors'})
                          .catch(e=>{
                              location.reload();
                          })
                      }
                  }
                  if ((document.readyState === 'loading') && '{{isFrameWebkit}}' === 'True') {
                      addEventListener('DOMContentLoaded', inject);
                  } else {
                      inject();
                  }
              """;
        try
        {
            if (!isFrameWebkit && resourceType.Equals("js", StringComparison.InvariantCultureIgnoreCase))
            {
                await Task.Delay(500);
            }

            await frame.EvaluateExpressionAsync(injectString);
            Log.Logger.Info(
                $"Injected {Path.GetFileName(fileRelativePath)} into {tabFriendlyName} from patch {patchName}");
        }
        catch (PuppeteerException e)
        {
            if (!tabFriendlyName.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
            {
                Log.Logger.Error($"Failed to inject {resourceType} into {tabFriendlyName}");
                Log.Logger.Debug(e);
            }
        }
    }

    private static bool IsFrameWebkit(IFrame frame)
    {
        return !frame.Url.StartsWith("https://steamloopback.host", StringComparison.InvariantCultureIgnoreCase) &&
               !frame.Url.StartsWith("devtools://", StringComparison.InvariantCultureIgnoreCase);
    }

    private static async Task UpdateColorInPage(IPage page)
    {
        try
        {
            await page.EmulateMediaFeaturesAsync([
                new MediaFeatureValue { MediaFeature = MediaFeature.PrefersColorScheme, Value = PreferredColorScheme }
            ]);
        }
        catch (PuppeteerException e)
        {
            Log.Logger.Error(e);
        }
    }

    public static async Task UpdateColorScheme(string? colorScheme = null)
    {
        if (s_browser == null || (!Settings.Default.UseAppTheme && colorScheme == null))
        {
            return;
        }

        string tmpColorScheme = PreferredColorScheme;
        PreferredColorScheme = colorScheme ?? PreferredColorScheme;

        IPage[] pages = await s_browser.PagesAsync();
        IEnumerable<Task> processTasks = pages.Select(UpdateColorInPage);
        await Task.WhenAll(processTasks);

        PreferredColorScheme = tmpColorScheme;
    }

    public static void SetColorScheme(string themeVariant)
    {
        PreferredColorScheme = themeVariant.ToLower() switch
        {
            "dark" => "dark",
            _ => "light"
        };
    }

    public static void SetAccentColors(IEnumerable<string> colors)
    {
        string[] colorsArr = colors as string[] ?? [.. colors];
        StringBuilder colorsCss = new();
        colorsCss.Append(":root { ");
        for (int i = 0; i < 7; i++)
        {
            colorsCss.Append($"--{ColorNames[i]}: {colorsArr[i]}; ");
        }

        colorsCss.Append('}');
        ColorsCss = colorsCss.ToString();
    }

    public static async Task UpdateSystemAccentColors(bool useAccentColors = true)
    {
        if (s_browser == null || (!Settings.Default.UseAppTheme && useAccentColors))
        {
            return;
        }

        IPage[] pages = await s_browser.PagesAsync();
        IEnumerable<Task> processTasks = useAccentColors
            ? pages.Select(UpdateSystemAccentColorsInPage)
            : pages.Select(async page =>
            {
                string injectString =
                    $$"""
                      function injectAcc() {
                                              var element = document.getElementById('SystemAccentColorInjection');
                                              if (element) {
                                                  element.parentNode.removeChild(element);
                                              }
                                          }
                                          if ((document.readyState === 'loading') && '{{IsFrameWebkit(page.MainFrame)}}' === 'True') {
                                              addEventListener('DOMContentLoaded', injectAcc);
                                          } else {
                                              injectAcc();
                                          }

                      """;
                await page.EvaluateExpressionAsync(injectString);
            });
        await Task.WhenAll(processTasks);
    }

    private static async Task UpdateSystemAccentColorsInPage(IPage page)
    {
        string injectString =
            $$"""
              function injectAcc() {
                              var element = document.getElementById('SystemAccentColorInjection');
                              if (element) {
                                  element.parentNode.removeChild(element);
                              }
                              element = document.createElement('style');
                              element.id = 'SystemAccentColorInjection';
                              element.innerHTML = `{{ColorsCss}}`;
                              document.head.append(element);
                          }
                          if ((document.readyState === 'loading') && '{{IsFrameWebkit(page.MainFrame)}}' === 'True') {
                              addEventListener('DOMContentLoaded', injectAcc);
                          } else {
                              injectAcc();
                          }

              """;
        await page.EvaluateExpressionAsync(injectString);
    }

    [GeneratedRegex(@"^(?:https?:\/\/)?(?:[^@\/\n]+@)?(?:www\.)?([^:\/?\n]+)")]
    private static partial Regex GetDomainRegex();
}