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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeSmell", "ERP022:Unobserved exception in generic exception handler", Justification = "Unsupported on certain OSes")]
        public static void UpdateNotificationManagerColors()
        {
            if (Views.MainWindow.Instance?.Theme is FluentAvaloniaTheme theme
                && s_builder is NotificationMessageBuilder builder)
            {
                try
                {
                    Color? accentColor = theme.TryGetResource("SystemAccentColor", out object? acc) ? (Color)acc : null;
                    builder.Message.AccentBrush = Brush.Parse(accentColor.ToString());

                    Color? backgroundColor = theme.TryGetResource("SolidBackgroundFillColorBase", out object? bg) ? (Color)bg : null;
                    builder.Message.Background = Brush.Parse(backgroundColor.ToString());

                    Color? foregroundColor = theme.TryGetResource("TextFillColorPrimary", out object? fg) ? (Color)fg : null;
                    builder.Message.Foreground = Brush.Parse(foregroundColor.ToString());
                }
                catch
                {
                    builder.Message.AccentBrush = Brush.Parse("#1751C3");
                    builder.Message.Background = Brush.Parse("#333");
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
