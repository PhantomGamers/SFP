using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

using Avalonia.Notification;

using Newtonsoft.Json.Linq;

using Semver;

using SFP;

using SFP_UI.ViewModels;

namespace SFP_UI.Models
{
    internal class UpdateCheckModel
    {
        private static readonly HttpClient client = new();

        public static readonly SemVersion Version = SemVersion.Parse(Assembly.GetEntryAssembly()!
                                                              .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                                                              .InformationalVersion, SemVersionStyles.Strict);

        static UpdateCheckModel()
        {
            client.DefaultRequestHeaders.UserAgent.Add(new("SFP", Version.ToString()));
        }

        public static async Task CheckForUpdates()
        {
            LogModel.Logger.Info("Checking for updates...");
            var semver = await GetLatestVersionAsync();

            if (semver > Version)
            {
                MainPageViewModel.Instance!.Manager
                                 .CreateMessage()
                                 .Accent("#1751C3")
                                 .Animates(true)
                                 .Background("#333")
                                 .HasBadge("Info")
                                 .HasMessage(
                                     "There is an update available!")
                                 .Dismiss().WithButton("Open download page", button =>
                                 {
                                     OpenUrl("https://github.com/phantomgamers/sfp/releases/latest");
                                 })
                                 .Dismiss().WithButton("Dismiss", button => { })
                                 .Queue();
            }
        }

        private static async Task<SemVersion> GetLatestVersionAsync()
        {
            try
            {
                var responseBody = await client.GetStringAsync("https://api.github.com/repos/phantomgamers/sfp/releases/latest");
                var json = JObject.Parse(responseBody);

                if (SemVersion.TryParse(json["tag_name"]?.ToString() ?? string.Empty, SemVersionStyles.Strict, out var semver))
                {
                    return semver;
                }
            }
            catch (HttpRequestException e)
            {
                LogModel.Logger.Error("Could not fetch latest version!");
                LogModel.Logger.Error("Exception: {0}", e.Message);
            }

            return new(-1);
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
