using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

using Avalonia.Media;
using Avalonia.Notification;

using FluentAvalonia.Styling;

using Newtonsoft.Json.Linq;

using Semver;

using SFP;

using SFP_UI.ViewModels;

namespace SFP_UI.Models
{
    internal class UpdateCheckModel
    {

        public static readonly SemVersion Version = SemVersion.Parse(Assembly.GetEntryAssembly()!
                                                              .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                                                              .InformationalVersion, SemVersionStyles.Strict);

        private static NotificationMessageBuilder? s_builder;
        private static readonly HttpClient s_client = new();

        static UpdateCheckModel()
        {
            s_client.DefaultRequestHeaders.UserAgent.Add(new("SFP", Version.ToString()));
        }

        public static async Task CheckForUpdates()
        {
            LogModel.Logger.Info("Checking for updates...");
            SemVersion? semver = await GetLatestVersionAsync();

            if (SemVersion.ComparePrecedence(Version, semver) < 0
                && MainPageViewModel.Instance?.Manager is INotificationMessageManager manager)
            {
                LogModel.Logger.Info($"There is an update available! Your version: {Version} Latest version: {semver}");
                s_builder = manager.CreateMessage()
                                   .Animates(true)
                                   .HasBadge("Info")
                                   .HasMessage("There is an update available!")
                                   .Dismiss().WithButton("Open download page", button =>
                                   {
                                       UtilsModel.OpenUrl("https://github.com/phantomgamers/sfp/releases/latest");
                                   })
                                   .Dismiss().WithButton("Dismiss", button => { });
                UpdateNotificationManagerColors();
                s_builder.Queue();
            }
            else
            {
                LogModel.Logger.Info("You are running the latest version.");
            }
        }

        public static void UpdateNotificationManagerColors()
        {
            if (Views.MainWindow.Instance?.Theme is FluentAvaloniaTheme theme
                && s_builder is NotificationMessageBuilder builder)
            {
                if (theme.TryGetResource("AccentFillColorSelectedTextBackgroundBrush", out object? accentColor))
                {
                    LogModel.Logger.Info("accentColor");
                    builder.Message.AccentBrush = (IBrush)accentColor;
                }
                else
                {
                    builder.Message.AccentBrush = Brush.Parse("#1751C3");
                }

                if (theme.TryGetResource("SolidBackgroundFillColorBaseBrush", out object? backgroundColor))
                {
                    LogModel.Logger.Info("backgroundColor");
                    builder.Message.Background = (IBrush)backgroundColor;
                }
                else
                {
                    builder.Message.Background = Brush.Parse("#333");
                }

                if (theme.TryGetResource("DefaultTextForegroundThemeBrush", out object? foregroundColor))
                {
                    LogModel.Logger.Info("foregroundColor");
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
                LogModel.Logger.Error("Could not fetch latest version!");
                LogModel.Logger.Error("Exception: {0}", e.Message);
            }

            return new(-1);
        }
    }
}
