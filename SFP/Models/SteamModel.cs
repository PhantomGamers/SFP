using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace SFP
{
    public class SteamModel
    {
        public static string SteamUIDir => Path.Join(SteamDir, "steamui");

        public static string SteamUICSSDir => Path.Join(SteamUIDir, "css");

        public static string ClientUIDir => Path.Join(SteamDir, "clientui");

        private static int RunningGameID => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                          ? (int)UtilsModel.GetRegistryData(@"SOFTWARE\Valve\Steam", "RunningAppID")
                                          : -1;

        [SupportedOSPlatform("windows")]
        private static string RunningGameName => UtilsModel.GetRegistryData(@"SOFTWARE\Valve\Steam\Apps\" + RunningGameID, "Name").ToString();

        private static bool IsGameRunning => RunningGameID > 0;

        private static bool IsSteamRunning => SteamProcess is not null;

        private static Process? SteamProcess => Process.GetProcessesByName("Steam").FirstOrDefault();

        public static string? SteamDir
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.SteamDirectory))
                {
                    return Properties.Settings.Default.SteamDirectory;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return UtilsModel.GetRegistryData(@"SOFTWARE\Valve\Steam", "SteamPath")?.ToString()?.Replace(@"/", @"\");
                }

                return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "steam");
            }
        }

        public static string CacheDir
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.CacheDirectory))
                {
                    return Properties.Settings.Default.CacheDirectory;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Steam", "htmlcache", "Cache");
                }

                return Path.Join(SteamDir, "config", "htmlcache", "Cache");
            }
        }

        public static string SteamExe
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return Path.Join(SteamDir, "Steam.exe");
                }

                return "steam";
            }
        }

        public static async Task ResetSteam()
        {
            LogModel.Logger.Info("Resetting Steam");
            if (!Directory.Exists(SteamUICSSDir))
            {
                LogModel.Logger.Warn($"Missing directory {SteamUICSSDir}");
            }

            if (!Directory.Exists(CacheDir))
            {
                LogModel.Logger.Warn($"Missing directory {CacheDir}");
            }

            if (!Directory.Exists(SteamUICSSDir) && !Directory.Exists(CacheDir))
            {
                return;
            }

            if (IsGameRunning)
            {
                var gameName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? RunningGameName : "Game";
                LogModel.Logger.Warn($"{gameName} is running, aborting reset process... Close the game and try again.");
                return;
            }

            var steamState = IsSteamRunning;
            if (steamState)
            {
                LogModel.Logger.Info($"Shutting down Steam");
                if (!ShutDownSteam())
                {
                    LogModel.Logger.Warn("Could not shut down Steam. Manually shut down Steam and try again.");
                    return;
                }
            }

            var scannerState = FSWModel.WatchersActive;
            FSWModel.RemoveAllWatchers();

            try
            {
                if (Directory.Exists(SteamUICSSDir))
                {
                    LogModel.Logger.Info($"Deleting {SteamUICSSDir}");
                    Directory.Delete(SteamUICSSDir, true);
                }

                if (Directory.Exists(CacheDir))
                {
                    LogModel.Logger.Info($"Deleting {CacheDir}");
                    Directory.Delete(CacheDir, true);
                }
            }
            catch (Exception ex)
            {
                LogModel.Logger.Debug(ex);
                LogModel.Logger.Warn($"Could not delete files because they were in use. Manually shut down Steam and try again.");
            }

            if (scannerState)
            {
                await FSWModel.StartFileWatchers();
            }

            if (steamState)
            {
                LogModel.Logger.Info("Starting Steam");
                StartSteam();
            }
        }

        private static bool ShutDownSteam()
        {
            Process.Start(SteamExe, "-shutdown");
            var proc = SteamProcess;
            if (proc != null && !proc.WaitForExit((int)TimeSpan.FromSeconds(30).TotalMilliseconds))
            {
                return false;
            }
            return true;
        }

        private static void StartSteam()
        {
            Process.Start(SteamExe, Properties.Settings.Default.SteamLaunchArgs);
        }

        public static void RestartSteam()
        {
            LogModel.Logger.Info("Shutting down Steam");
            ShutDownSteam();
            LogModel.Logger.Info("Starting Steam");
            StartSteam();
        }
    }
}
