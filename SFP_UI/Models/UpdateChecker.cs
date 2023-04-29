#region

using System.Net.Http.Headers;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        // #if DEBUG
        // return;
        // #endif

        Log.Logger.Info("Checking for updates...");
        SemVersion semver = await GetLatestVersionAsync();
        if (SemVersion.ComparePrecedence(Version, semver) < 0)
        {
            MainPageViewModel.Instance?.ShowUpdateNotification(Version, semver);
        }
        else
        {
            Log.Logger.Info("You are running the latest version.");
        }
    }

#pragma warning disable CS1998
    private static async Task<SemVersion> GetLatestVersionAsync()
#pragma warning restore CS1998
    {
#if DEBUG
        string responseBody = $"{{\"tag_name\":\"{Version.WithMinor(Version.Minor + 1)}\"}}";
#else
        string responseBody =
            await s_client.GetStringAsync("https://api.github.com/repos/phantomgamers/sfp/releases/latest");
#endif
        Release release = JsonConvert.DeserializeObject<Release>(responseBody);

        return SemVersion.TryParse(release.TagName, SemVersionStyles.Strict,
            out SemVersion? semver) ? semver : new SemVersion(-1);
    }
}

internal struct Release
{
    [JsonProperty("tag_name")]
    public string TagName { get; set; }
}
