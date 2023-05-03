#region

#if RELEASE
using System.Net.Http.Headers;
using Flurl.Http;
#endif
using System.Reflection;
using System.Text.Json.Serialization;
using Semver;
using SFP_UI.ViewModels;
using SFP.Models;
#if DEBUG
using System.Text.Json;
#endif

#endregion

namespace SFP_UI.Models;

internal static class UpdateChecker
{
    public static readonly SemVersion Version = SemVersion.Parse(Assembly.GetEntryAssembly()!
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
        .InformationalVersion, SemVersionStyles.Strict);

    public static async Task CheckForUpdates()
    {
#if DEBUG
        return;
#endif

#pragma warning disable CS0162
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
#pragma warning restore CS0162
    }

#pragma warning disable CS1998
    private static async Task<SemVersion> GetLatestVersionAsync()
#pragma warning restore CS1998
    {
#if DEBUG
        string responseBody = $"{{\"tag_name\":\"{Version.WithMinor(Version.Minor + 1)}\"}}";
#pragma warning disable IL2026
        Release release = JsonSerializer.Deserialize<Release>(responseBody);
#pragma warning restore IL2026
#else
        Release release =
            await new Uri("https://api.github.com/repos/phantomgamers/sfp/releases/latest")
                .WithHeader("User-Agent", new ProductInfoHeaderValue("SFP", Version.ToString()))
                .GetJsonAsync<Release>();
#endif
        return SemVersion.Parse(release.TagName, SemVersionStyles.Strict);
    }
}

internal struct Release
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    [JsonPropertyName("tag_name")] public string TagName { get; set; }
}
