#region

using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json.Serialization;
using Flurl.Http;
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

    public static async Task CheckForUpdates()
    {
        // #if DEBUG
        // return;
        // #endif

        Log.Logger.Info("Checking for updates...");

        try
        {
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
        catch (Exception e)
        {
            Log.Logger.Error("Failed to fetch latest version.");
            Log.Logger.Error(e);
        }
    }

#pragma warning disable CS1998
    private static async Task<SemVersion> GetLatestVersionAsync()
#pragma warning restore CS1998
    {
#if DEBUG
        string responseBody = $"{{\"tag_name\":\"{Version.WithMinor(Version.Minor + 1)}\"}}";
        Release release = JsonConvert.DeserializeObject<Release>(responseBody);
#else
        Release release =
            await new Uri("https://api.github.com/repos/phantomgamers/sfp/releases/latest")
                .WithHeader("User-Agent", new ProductInfoHeaderValue("SFP", Version.ToString()))
                .GetJsonAsync<Release>();
#endif
        Log.Logger.Info(release.TagName);
        return SemVersion.Parse(release.TagName, SemVersionStyles.Strict);
    }
}

internal struct Release
{
    [JsonPropertyName("tag_name")] public string TagName { get; set; }
}
