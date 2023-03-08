using System.Net.Http.Headers;
using System.Reflection;

using Avalonia.Media;
//
using FluentAvalonia.Styling;

using Newtonsoft.Json.Linq;

using Semver;

using SFP.Models;

using SFP_UI.ViewModels;

namespace SFP_UI.Models
{
    internal static class UpdateChecker
    {

        public static readonly SemVersion Version = SemVersion.Parse(Assembly.GetEntryAssembly()!
                                                              .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                                                              .InformationalVersion, SemVersionStyles.Strict);

        private static readonly HttpClient s_client = new();

        static UpdateChecker() => s_client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SFP", Version.ToString()));

        public static async Task CheckForUpdates()
        {
            Log.Logger.Info("Checking for updates...");
            SemVersion? semver = await GetLatestVersionAsync();

            if (SemVersion.ComparePrecedence(Version, semver) < 0)
            {
                Log.Logger.Info($"There is an update available! Your version: {Version} Latest version: {semver}");
            }
            else
            {
                Log.Logger.Info("You are running the latest version.");
            }
        }

        private static async Task<SemVersion> GetLatestVersionAsync()
        {
            try
            {
                string? responseBody = await s_client.GetStringAsync("https://api.github.com/repos/phantomgamers/sfp/releases/latest");
                var json = JObject.Parse(responseBody);

                if (SemVersion.TryParse(json["tag_name"]?.ToString() ?? string.Empty, SemVersionStyles.Strict, out SemVersion? semver))
                {
                    return semver;
                }
            }
            catch (HttpRequestException e)
            {
                Log.Logger.Error("Could not fetch latest version!");
                Log.Logger.Error("Exception: {0}", e.Message);
            }

            return new SemVersion(-1);
        }
    }
}
