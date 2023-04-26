#region

using System.Net.Http.Headers;
using System.Reflection;
#if !DEBUG
using Newtonsoft.Json.Linq;
#endif
using Semver;
using SFP.Models;
using SFP_UI.ViewModels;

#endregion

namespace SFP_UI.Models;

internal static class UpdateChecker
{
    public static readonly SemVersion Version = SemVersion.Parse(Assembly.GetEntryAssembly()!
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
        .InformationalVersion, SemVersionStyles.Strict);

    private static readonly HttpClient s_client = new();

    static UpdateChecker() =>
        s_client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SFP", Version.ToString()));

    public static async Task CheckForUpdates()
    {
        Log.Logger.Info("Checking for updates...");
#if DEBUG
        SemVersion? semver = new(0);
#else
            SemVersion? semver = await GetLatestVersionAsync();
#endif

#if !DEBUG
            if (SemVersion.ComparePrecedence(Version, semver) < 0)
#else
        if (true)
#endif
        {
            Log.Logger.Info($"There is an update available! Your version: {Version} Latest version: {semver}");
            MainPageViewModel.Instance.UpdateNotificationContent =
                $"Your version: {Version}{Environment.NewLine}Latest version: {semver}";
            MainPageViewModel.Instance.UpdateNotificationIsOpen = true;
        }
        else
        {
            Log.Logger.Info("You are running the latest version.");
        }
    }

#if !DEBUG
    private static async Task<SemVersion> GetLatestVersionAsync()
    {
        try
        {
            string responseBody =
                await s_client.GetStringAsync("https://api.github.com/repos/phantomgamers/sfp/releases/latest");
            JObject json = JObject.Parse(responseBody);

            if (SemVersion.TryParse(json["tag_name"]?.ToString() ?? string.Empty, SemVersionStyles.Strict,
                    out SemVersion? semver))
            {
                return semver;
            }
        }
        catch (HttpRequestException e)
        {
            Log.Logger.Error("Could not fetch latest version!");
            Log.Logger.Error($"Exception: {e.Message}");
        }

        return new SemVersion(-1);
    }
#endif
}
