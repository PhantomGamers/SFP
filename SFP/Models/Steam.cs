#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using FileWatcherEx;
using Gameloop.Vdf;
using SFP.Models.Injection;

#endregion

namespace SFP.Models;

public static class Steam
{
    private static bool IsSteamRunning => SteamWebHelperProcess is not null;

    private static Process? SteamWebHelperProcess => Process.GetProcessesByName("steamwebhelper").FirstOrDefault();

    private static FileSystemWatcherEx? s_watcher;

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
        Process? proc = SteamWebHelperProcess;
        if (proc == null || proc.WaitForExit((int)TimeSpan.FromSeconds(30).TotalMilliseconds))
        {
            return true;
        }

        Log.Logger.Warn("Could not shut down Steam. Manually shut down Steam and try again.");
        return false;
    }

    public static void StartSteam(string? args = null)
    {
        args ??= Properties.Settings.Default.SteamLaunchArgs;
        if (!args.Contains("--cef-enable-debugging"))
        {
            args += " --cef-enable-debugging";
            args = args.Trim();
        }
        Log.Logger.Info("Starting Steam");
        _ = Process.Start(SteamExe, args);
    }

    public static void StartMonitorSteam()
    {
        if (s_watcher != null || string.IsNullOrWhiteSpace(SteamDir))
        {
            return;
        }
        s_watcher = new FileSystemWatcherEx(SteamDir)
        {
            Filter = ".crash"
        };
        s_watcher.OnCreated += OnCrashFileCreated;
        s_watcher.Start();
    }

    public static void StopMonitorSteam()
    {
        s_watcher?.Stop();
        s_watcher?.Dispose();
        s_watcher = null;
    }

    private static async void OnCrashFileCreated(object? sender, FileChangedEvent e)
    {
        while (!IsSteamRunning)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }


        await Injector.StartInjectionAsync();
    }
}
