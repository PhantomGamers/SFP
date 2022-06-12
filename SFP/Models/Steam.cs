using System.Diagnostics;

using SFP.Models.FileSystemWatchers;

namespace SFP.Models
{
    public class Steam
    {
        public static string SteamUIDir => Path.Join(SteamDir, "steamui");

        public static string SteamUICSSDir => Path.Join(SteamUIDir, "css");

        public static string ClientUIDir => Path.Join(SteamDir, "clientui");

        public static string ClientUICSSDir => Path.Join(ClientUIDir, "css");

        public static string SkinDir => Path.Join(SteamDir, "skins", SkinName);

        public static string ResourceDir => Path.Join(SteamDir, "resource");

        private static string? SkinName => GetRegistryData(@"Software\Valve\Steam", "SkinV5")?.ToString();

        private static int RunningGameID => (int)(GetRegistryData(@"Software\Valve\Steam", "RunningAppID") ?? -1);

        private static string? RunningGameName => GetRegistryData(@"Software\Valve\Steam\Apps\" + RunningGameID, "Name")?.ToString();

        private static bool IsGameRunning => RunningGameID > 0;

        private static bool IsSteamRunning => SteamProcess is not null;

        private static Process? SteamProcess => Process.GetProcessesByName("steam").FirstOrDefault();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeSmell", "ERP022:Unobserved exception in generic exception handler", Justification = "ok if null")]
        private static object? GetRegistryData(string key, string valueName)
        {
            if (OperatingSystem.IsWindows())
            {
                return Utils.GetRegistryData(key, valueName);
            }

            try
            {
                dynamic reg = Gameloop.Vdf.VdfConvert.Deserialize(File.ReadAllText(Path.Join(SteamRootDir, "registry.vdf")));
                string kn = @$"HKCU/{key.Replace('\\', '/')}/{valueName}";
                dynamic currentVal = reg.Value;
                foreach (string keyPart in kn.Split('/'))
                {
                    currentVal = currentVal[keyPart];
                }
                return currentVal;
            }
            catch
            {
                return null;
            }
        }

        public static string? SteamRootDir
        {
            get
            {
                if (OperatingSystem.IsWindows())
                {
                    return SteamDir;
                }
                else if (OperatingSystem.IsLinux())
                {
                    return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam");
                }
                else if (OperatingSystem.IsMacOS())
                {
                    return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Steam");
                }
                return null;
            }
        }

        public static string? SteamDir
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.SteamDirectory))
                {
                    return Properties.Settings.Default.SteamDirectory;
                }

                if (OperatingSystem.IsWindows())
                {
                    return GetRegistryData(@"SOFTWARE\Valve\Steam", "SteamPath")?.ToString()?.Replace(@"/", @"\");
                }

                if (OperatingSystem.IsLinux())
                {
                    return Path.Join(SteamRootDir, "steam");
                }

                // OSX
                return Path.Join(SteamRootDir, "Steam.AppBundle", "Steam", "Contents", "MacOS");
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

                if (OperatingSystem.IsWindows())
                {
                    return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Steam", "htmlcache", "Cache");
                }

                if (OperatingSystem.IsLinux())
                {
                    return Path.Join(SteamDir, "config", "htmlcache", "Cache");
                }

                // OSX
                return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Steam", "config", "htmlcache", "Cache");
            }
        }

        public static string SteamExe => OperatingSystem.IsWindows() ? Path.Join(SteamDir, "Steam.exe") : "steam";

        public static async Task ResetSteam()
        {
            Log.Logger.Info("Resetting Steam");
            if (!Directory.Exists(SteamUICSSDir))
            {
                Log.Logger.Warn($"Missing directory {SteamUICSSDir}");
            }

            if (!Directory.Exists(ClientUICSSDir))
            {
                Log.Logger.Warn($"Missing directory {ClientUICSSDir}");
            }

            if (!Directory.Exists(CacheDir))
            {
                Log.Logger.Warn($"Missing directory {CacheDir}");
            }

            if (!Directory.Exists(SteamUICSSDir) && !Directory.Exists(ClientUICSSDir) && !Directory.Exists(CacheDir))
            {
                return;
            }

            if (IsGameRunning)
            {
                string? gameName = RunningGameName ?? "Game";
                Log.Logger.Warn($"{gameName} is running, aborting reset process... Close the game and try again.");
                return;
            }

            bool steamState = IsSteamRunning;
            if (steamState && !ShutDownSteam())
            {
                return;
            }

            bool scannerState = FileSystemWatchers.FileSystemWatcher.WatchersActive;
            await FileSystemWatchers.FileSystemWatcher.StopFileWatchers();

            try
            {
                if (Directory.Exists(SteamUICSSDir))
                {
                    Log.Logger.Info($"Deleting {SteamUICSSDir}");
                    Directory.Delete(SteamUICSSDir, true);
                }

                if (Directory.Exists(ClientUICSSDir))
                {
                    Log.Logger.Info($"Deleting {ClientUICSSDir}");
                    Directory.Delete(ClientUICSSDir, true);
                }

                if (Directory.Exists(CacheDir))
                {
                    Log.Logger.Info($"Deleting {CacheDir}");
                    Directory.Delete(CacheDir, true);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Debug(ex);
                Log.Logger.Warn("Could not delete files because they were in use. Manually shut down Steam and try again.");
            }

            if (scannerState)
            {
                await FileSystemWatchers.FileSystemWatcher.StartFileWatchers();
            }

            if (steamState)
            {
                StartSteam(Properties.Settings.Default.SteamLaunchArgs.Replace("-noverifyfiles", string.Empty));
            }
        }

        private static bool ShutDownSteam()
        {
            Log.Logger.Info("Shutting down Steam");
            _ = Process.Start(SteamExe, "-shutdown");
            Process? proc = SteamProcess;
            if (proc != null && !proc.WaitForExit((int)TimeSpan.FromSeconds(30).TotalMilliseconds))
            {
                Log.Logger.Warn("Could not shut down Steam. Manually shut down Steam and try again.");
                return false;
            }
            return true;
        }

        private static void StartSteam(string? args = null)
        {
            args ??= Properties.Settings.Default.SteamLaunchArgs;
            Log.Logger.Info("Starting Steam");
            _ = Process.Start(SteamExe, args);
        }

        public static void RestartSteam()
        {
            if (ShutDownSteam())
            {
                StartSteam();
            }
        }
    }
}
