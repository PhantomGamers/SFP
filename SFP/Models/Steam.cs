#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Gameloop.Vdf;
using FileSystemWatcher = SFP.Models.FileSystemWatchers.FileSystemWatcher;

#endregion

namespace SFP.Models;

public static class Steam
{
    public static string SteamUiDir => Path.Join(SteamDir, @"steamui");

    public static string SteamUiCssDir => Path.Join(SteamUiDir, "css");

    public static string ClientUiDir => Path.Join(SteamDir, @"clientui");

    public static string ClientUiCssDir => Path.Join(ClientUiDir, "css");

    public static string SkinDir => Path.Join(SteamDir, "skins", SkinName);

    public static string ResourceDir => Path.Join(SteamDir, "resource");

    private static string? SkinName => GetRegistryData(@"Software\Valve\Steam", "SkinV5")?.ToString();

    private static int RunningGameId => (int)(GetRegistryData(@"Software\Valve\Steam", "RunningAppID") ?? -1);

    private static string? RunningGameName =>
        GetRegistryData(@"Software\Valve\Steam\Apps\" + RunningGameId, "Name")?.ToString();

    private static bool IsGameRunning => RunningGameId > 0;

    private static bool IsSteamRunning => SteamProcess is not null;

    private static Process? SteamProcess => Process.GetProcessesByName("steam").FirstOrDefault();

    private static string? SteamRootDir
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                return SteamDir;
            }

            if (OperatingSystem.IsLinux())
            {
                return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam");
            }

            return OperatingSystem.IsMacOS()
                ? Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library",
                    "Application Support", "Steam")
                : null;
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

            return OperatingSystem.IsLinux()
                ? Path.Join(SteamRootDir, "steam")
                :
                // OSX
                Path.Join(SteamRootDir, "Steam.AppBundle", "Steam", "Contents", "MacOS");
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
                return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Steam",
                    @"htmlcache", "Cache");
            }

            return OperatingSystem.IsLinux()
                ? Path.Join(SteamDir, "config", @"htmlcache", "Cache")
                :
                // OSX
                Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library",
                    "Application Support", "Steam", "config", @"htmlcache", "Cache");
        }
    }

    private static string SteamExe => OperatingSystem.IsWindows() ? Path.Join(SteamDir, "Steam.exe") : "steam";

    [SuppressMessage("CodeSmell", "ERP022:Unobserved exception in generic exception handler",
        Justification = "ok if null")]
    private static object? GetRegistryData(string key, string valueName)
    {
        if (OperatingSystem.IsWindows())
        {
            return Utils.GetRegistryData(key, valueName);
        }

        try
        {
            dynamic reg = VdfConvert.Deserialize(File.ReadAllText(Path.Join(SteamRootDir, "registry.vdf")));
            string kn = @$"HKCU/{key.Replace('\\', '/')}/{valueName}";
            dynamic currentVal = reg.Value;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (string keyPart in kn.Split('/'))
            {
                currentVal = currentVal[keyPart];
            }

            return int.TryParse(currentVal.Value, out int val) ? val : -1;
        }
        catch
        {
            return -1;
        }
    }

    public static async Task ResetSteam()
    {
        Log.Logger.Info("Resetting Steam");
        if (!Directory.Exists(SteamUiCssDir))
        {
            Log.Logger.Warn($"Missing directory {SteamUiCssDir}");
        }

        if (!Directory.Exists(ClientUiCssDir))
        {
            Log.Logger.Warn($"Missing directory {ClientUiCssDir}");
        }

        if (!Directory.Exists(CacheDir))
        {
            Log.Logger.Warn($"Missing directory {CacheDir}");
        }

        if (!Directory.Exists(SteamUiCssDir) && !Directory.Exists(ClientUiCssDir) && !Directory.Exists(CacheDir))
        {
            return;
        }

        if (IsGameRunning)
        {
            string gameName = RunningGameName ?? "Game";
            Log.Logger.Warn($"{gameName} is running, aborting reset process... Close the game and try again.");
            return;
        }

        bool steamState = IsSteamRunning;
        if (steamState && !ShutDownSteam())
        {
            return;
        }

        bool scannerState = FileSystemWatcher.WatchersActive;
        await FileSystemWatcher.StopFileWatchers();

        try
        {
            if (Directory.Exists(SteamUiCssDir))
            {
                Log.Logger.Info($"Deleting {SteamUiCssDir}");
                Directory.Delete(SteamUiCssDir, true);
            }

            if (Directory.Exists(ClientUiCssDir))
            {
                Log.Logger.Info($"Deleting {ClientUiCssDir}");
                Directory.Delete(ClientUiCssDir, true);
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
            await FileSystemWatcher.StartFileWatchers();
        }

        if (steamState)
        {
            StartSteam(Properties.Settings.Default.SteamLaunchArgs.Replace(@"-noverifyfiles", string.Empty));
        }
    }

    private static bool ShutDownSteam()
    {
        Log.Logger.Info("Shutting down Steam");
        _ = Process.Start(SteamExe, "-shutdown");
        Process? proc = SteamProcess;
        if (proc == null || proc.WaitForExit((int)TimeSpan.FromSeconds(30).TotalMilliseconds))
        {
            return true;
        }

        Log.Logger.Warn("Could not shut down Steam. Manually shut down Steam and try again.");
        return false;
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
