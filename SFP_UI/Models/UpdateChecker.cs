using System.Reflection;

using Avalonia.Media;
using Avalonia.Notification;

using FluentAvalonia.Styling;

using Newtonsoft.Json.Linq;

using Semver;

using SFP.Models;

using SFP_UI.ViewModels;

namespace SFP_UI.Models
{
    internal class UpdateChecker
    {

        public static readonly SemVersion Version = SemVersion.Parse(Assembly.GetEntryAssembly()!
                                                              .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                                                              .InformationalVersion, SemVersionStyles.Strict);

        private static NotificationMessageBuilder? s_builder;
        private static readonly HttpClient s_client = new();

        static UpdateChecker() => s_client.DefaultRequestHeaders.UserAgent.Add(new("SFP", Version.ToString()));

        public static async Task CheckForUpdates()
        {
            Log.Logger.Info("Checking for updates...");
            SemVersion? semver = await GetLatestVersionAsync();

            if (SemVersion.ComparePrecedence(Version, semver) < 0
                && MainPageViewModel.Instance?.Manager is INotificationMessageManager manager)
            {
                Log.Logger.Info($"There is an update available! Your version: {Version} Latest version: {semver}");
                s_builder = manager.CreateMessage()
                                   .Animates(true)
                                   .HasBadge("Info")
                                   .HasMessage("There is an update available!")
                                   .Dismiss().WithButton("Open download page", button =>
                                   {
                                       Utils.OpenUrl("https://github.com/phantomgamers/sfp/releases/latest");
                                   })
                                   .Dismiss().WithButton("Dismiss", button => { });
                UpdateNotificationManagerColors();
                _ = s_builder.Queue();
            }
            else
            {
                Log.Logger.Info("You are running the latest version.");
            }
        }

        public static void UpdateNotificationManagerColors()
        {
            if (Views.MainWindow.Instance?.Theme is FluentAvaloniaTheme theme
                && s_builder is NotificationMessageBuilder builder)
            {
                builder.Message.AccentBrush = theme.TryGetResource("AccentFillColorSelectedTextBackgroundBrush", out object? accentColor)
                    ? (IBrush)accentColor
                    : Brush.Parse("#1751C3");

                builder.Message.Background = theme.TryGetResource("SolidBackgroundFillColorBaseBrush", out object? backgroundColor)
                    ? (IBrush)backgroundColor
                    : Brush.Parse("#333");

                if (theme.TryGetResource("DefaultTextForegroundThemeBrush", out object? foregroundColor))
                {
                    builder.Message.Foreground = (IBrush)foregroundColor;
                }
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

            return new(-1);
        }
    }
}
