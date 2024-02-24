#region

using System.Reflection;
using System.Text.Json.Serialization;
using Semver;
using SFP_UI.ViewModels;
using SFP.Models;
#if RELEASE
using System.Net.Http.Headers;
using Flurl.Http;
#endif
#if DEBUG
using System.Text.Json;
// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
#endif

#endregion

namespace SFP_UI.Models;

internal static class UpdateChecker
{
    public static readonly SemVersion Version = SemVersion.Parse(Assembly.GetEntryAssembly()!
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
        .InformationalVersion
        .Split('+')[0], SemVersionStyles.Strict);

    public static async Task CheckForUpdates()
    {
#if DEBUG
        return;
#endif

#pragma warning disable CS0162
        Log.Logger.Info("Checking for updates...");
        try
        {
            var semver = await GetLatestVersionAsync(Version.IsPrerelease);
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
    private static async Task<SemVersion> GetLatestVersionAsync(bool preRelease)
#pragma warning restore CS1998
    {
#if DEBUG
        var responseBody = $"{{\"tag_name\":\"{Version.WithMinor(Version.Minor + 1)}\"}}";
#pragma warning disable IL2026
        var release = JsonSerializer.Deserialize<Release>(responseBody);
#pragma warning restore IL2026
#else
        Release release;

        if (!preRelease)
        {
            release = await new Uri("https://api.github.com/repos/phantomgamers/sfp/releases/latest")
                .WithHeader("User-Agent", new ProductInfoHeaderValue("SFP", Version.ToString()))
                .GetJsonAsync<Release>()
                .ConfigureAwait(false);
        }
        else
        {
            var releases = await new Uri("https://api.github.com/repos/phantomgamers/sfp/releases")
                .WithHeader("User-Agent", new ProductInfoHeaderValue("SFP", Version.ToString()))
                .GetJsonAsync<Release[]>()
                .ConfigureAwait(false);
            release = releases[0];
        }

#endif
        return SemVersion.Parse(release.TagName, SemVersionStyles.Strict);
    }
}

internal struct Release
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    [JsonPropertyName("tag_name")] public string TagName { get; set; }
}
