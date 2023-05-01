#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Gameloop.Vdf;

#endregion

namespace SFP.Models;

public static class Steam
{
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
