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
    public static bool IsSteamWebHelperRunning => SteamWebHelperProcess is not null;
    public static bool IsSteamRunning => SteamProcess is not null;
    private static Process? SteamWebHelperProcess => Process.GetProcessesByName("steamwebhelper").FirstOrDefault();
    private static Process? SteamProcess => Process.GetProcessesByName("steam").FirstOrDefault();
    private static FileSystemWatcherEx? s_watcher;
    private static Process? s_steamProcess;
    public static event EventHandler? SteamStarted;
    public static event EventHandler? SteamStopped;
    private static readonly SemaphoreSlim s_semaphore = new(1, 1);

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

    private static void ShutDownSteam(Process steamProcess)
    {
        if (steamProcess.HasExited)
        {
            return;
        }
        Log.Logger.Info("Shutting down Steam");
        _ = Process.Start(SteamExe, "-shutdown");
    }

    public static async Task StartSteam(string? args = null)
    {
        if (IsSteamRunning)
        {
            return;
        }
        args ??= Properties.Settings.Default.SteamLaunchArgs;
        if (!args.Contains("-cef-enable-debugging"))
        {
            args += " -cef-enable-debugging";
            args = args.Trim();
        }
        Log.Logger.Info("Starting Steam");
        _ = Process.Start(SteamExe, args);
        if (Properties.Settings.Default.InjectOnSteamStart)
        {
            await TryInject();
        }
    }

    public static async Task RestartSteam(string? args = null)
    {
        if (IsSteamRunning)
        {
            s_steamProcess = SteamProcess;
            s_steamProcess!.EnableRaisingEvents = true;
            s_steamProcess.Exited += OnSteamExited;
            ShutDownSteam(s_steamProcess);
        }
        else
        {
            await StartSteam(args);
        }
    }

    private static async void OnSteamExited(object? sender, EventArgs e)
    {
        s_steamProcess?.Dispose();
        s_steamProcess = null;
        await StartSteam();
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
        s_watcher.OnDeleted += OnCrashFileDeleted;
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
        SteamStarted?.Invoke(null, EventArgs.Empty);
        await TryInject();
    }

    private static void OnCrashFileDeleted(object? sender, FileChangedEvent e)
    {
        SteamStopped?.Invoke(null, EventArgs.Empty);
    }

    public static async Task TryInject()
    {
        if (!await s_semaphore.WaitAsync(TimeSpan.Zero))
        {
            return;
        }

        try
        {
            if (!IsSteamRunning)
            {
                return;
            }

            if (OperatingSystem.IsWindows() && Properties.Settings.Default.ForceSteamArgs)
            {
                bool argumentMissing = Properties.Settings.Default.SteamLaunchArgs.Split(' ')
#pragma warning disable CA1416
                    .Any(arg => !Windows.Utils.GetCommandLine(SteamProcess!).Contains(arg));
#pragma warning restore CA1416

                if (argumentMissing)
                {
                    await RestartSteam();
                }
            }

            while (!IsSteamWebHelperRunning)
            {
                if (!IsSteamRunning)
                {
                    return;
                }
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            await Injector.StartInjectionAsync(true);
        }
        finally
        {
            s_semaphore.Release();
        }
    }
}
