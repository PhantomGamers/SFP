using System.Net.Http.Headers;
using System.Reflection;

using Newtonsoft.Json.Linq;

using Semver;

using SFP.Models;

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
#if DEBUG
            SemVersion? semver = new(0);
#else
            SemVersion? semver = await GetLatestVersionAsync();
#endif

            if (SemVersion.ComparePrecedence(Version, semver) < 0)
            {
                Log.Logger.Info($"There is an update available! Your version: {Version} Latest version: {semver}");
            }
            else
            {
                Log.Logger.Info("You are running the latest version.");
            }
        }
    }
}
