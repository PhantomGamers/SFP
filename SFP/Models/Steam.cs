#region

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using FileWatcherEx;

using SFP.Models.Injection;
using SFP.Properties;

#endregion

namespace SFP.Models;

public static class Steam
{
    private static FileSystemWatcherEx? s_watcher;
    private static Process? s_steamProcess;
    private static bool s_injectOnce;
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private static readonly int ProcessAmount = OperatingSystem.IsWindows() ? 3 : OperatingSystem.IsMacOS() ? 4 : 6;
    public static bool IsSteamWebHelperRunning => SteamWebHelperProcesses.Length > ProcessAmount;
    public static bool IsSteamRunning => SteamProcess is not null;

    private static Process[] SteamWebHelperProcesses => [.. Process.GetProcessesByName(SteamWebHelperProcName).Where(p => p.ProcessName.Equals(SteamWebHelperProcName, StringComparison.OrdinalIgnoreCase))];

    private static Process? SteamProcess => Process.GetProcessesByName(SteamProcName)
        .FirstOrDefault(p => p.ProcessName.Equals(SteamProcName, StringComparison.OrdinalIgnoreCase));

    private static string SteamWebHelperProcName => OperatingSystem.IsMacOS() ? "Steam Helper" : "steamwebhelper";

    private static string SteamProcName => OperatingSystem.IsMacOS() ? "steam_osx" : "steam";

    internal static string MillenniumPath => Path.Join(SteamDir, "User32.dll");

    private static string? SteamRootDir => GetSteamRootDir();

    public static string? SteamDir => GetSteamDir();

    private static string SteamUiDir => Path.Join(SteamDir, "steamui");

    public static string SkinDir => Path.Join(SteamUiDir, GetRelativeSkinDir());

    public static string SkinsDir => Path.Join(SteamUiDir, "skins");

    private static string SteamExe =>
        Path.Join(SteamDir,
            OperatingSystem.IsWindows()
                ? "Steam.exe"
                : OperatingSystem.IsLinux()
                    ? "steam.sh"
                    : "steam_osx");

    private static string? GetSteamRootDir()
    {
        return OperatingSystem.IsWindows()
            ? SteamDir
            : OperatingSystem.IsLinux()
            ? Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam")
            : OperatingSystem.IsMacOS()
            ? Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library",
                "Application Support", "Steam")
            : null;
    }

    private static string? GetSteamDir()
    {
        var dir = Settings.Default.SteamDirectory;
        return !string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir)
            ? dir
            : OperatingSystem.IsWindows()
            ? (GetRegistryData(@"SOFTWARE\Valve\Steam", "SteamPath")?.ToString()?.Replace("/", @"\"))
            : OperatingSystem.IsLinux()
            ? Path.Join(SteamRootDir, "steam")
            :
            // OSX
            Path.Join(SteamRootDir, "Steam.AppBundle", "Steam", "Contents", "MacOS");
    }

    private static string s_relativeSkinDir = GetRelativeSkinDir();

    public static string GetRelativeSkinDir(bool force = false)
    {
        if (!string.IsNullOrWhiteSpace(s_relativeSkinDir) && !force)
        {
            return s_relativeSkinDir;
        }

        string relativeSkinDir;
        var selectedSkin = Settings.Default.SelectedSkin;
        if (string.IsNullOrWhiteSpace(selectedSkin))
        {
            Log.Logger.Debug("SelectedSkin is null or empty.");
            relativeSkinDir = string.Empty;
        }
        else
        {
            switch (selectedSkin)
            {
                case "steamui":
                    Log.Logger.Debug("SelectedSkin is steamui.");
                    relativeSkinDir = string.Empty;
                    break;
                case "skins\\steamui":
                    relativeSkinDir = "skins/steamui";
                    break;
                default:
                    relativeSkinDir = $"skins/{selectedSkin}";
                    break;
            }
        }
        return s_relativeSkinDir = relativeSkinDir;
    }

    public static event EventHandler? SteamStarted;
    public static event EventHandler? SteamStopped;

    [SuppressMessage("CodeSmell", "ERP022:Unobserved exception in generic exception handler",
        Justification = "ok if null")]
    private static object? GetRegistryData(string key, string valueName)
    {
        if (OperatingSystem.IsWindows())
        {
            return Windows.Utils.GetRegistryData(key, valueName);
        }

        // unimplemented for other operating systems
        return -1;
    }

    private static void ShutDownSteam(Process steamProcess)
    {
        if (steamProcess.HasExited)
        {
            return;
        }

        if (!File.Exists(SteamExe))
        {
            Log.Logger.Error($"{SteamExe} does not exist. Please set the correct Steam path in the settings.");
            return;
        }

        Log.Logger.Info("Shutting down Steam");
        if (IsSteamWebHelperRunning)
        {
            _ = Process.Start(SteamExe, "-shutdown");
        }
        else
        {
            steamProcess.Kill();
        }
    }

    [SuppressMessage("CodeSmell", "ERP022:Unobserved exception in a generic exception handler")]
    public static Task StartSteam(string? args = null)
    {
        if (IsSteamRunning)
        {
            return Task.CompletedTask;
        }

        if (!File.Exists(SteamExe))
        {
            Log.Logger.Error($"{SteamExe} does not exist. Please set the correct Steam path in the settings.");
            return Task.CompletedTask;
        }

        args ??= Settings.Default.SteamLaunchArgs.Trim();
        AppendArgs(ref args);

        if (OperatingSystem.IsWindows() && File.Exists(MillenniumPath))
        {
            Log.Logger.Warn("Millennium Patcher install detected, disabling Millennium patcher...");
            try
            {
                var newPath = $"{MillenniumPath}.disabled";
                File.Move(MillenniumPath, newPath, true);
            }
            catch (Exception e)
            {
                Log.Logger.Warn("Could not disable Millennium patcher, aborting as it is incompatible with SFP");
                Log.Logger.Error(e);
                return Task.CompletedTask;
            }
        }

        Log.Logger.Info("Starting Steam");
        var startInfo = new ProcessStartInfo(SteamExe, args) { UseShellExecute = true };
        _ = Process.Start(startInfo);
        return Task.CompletedTask;
    }

    public static async Task RunRestartSteam()
    {
        await Task.Run(() => RestartSteam());
    }

    private static async Task RestartSteam(string? args = null)
    {
        if (IsSteamRunning)
        {
            try
            {
                s_steamProcess = SteamProcess;
                s_steamProcess!.EnableRaisingEvents = true;
                s_steamProcess.Exited -= OnSteamExited;
                s_steamProcess.Exited += OnSteamExited;
                ShutDownSteam(s_steamProcess);
            }
            catch (Win32Exception e)
            {
                Log.Logger.Error("Could not shut down Steam, SFP does not have permission to interact with the Steam process.");
                Log.Logger.Error("Make sure Steam is not running as admin");
                Log.Logger.Debug(e);
            }
        }
        else
        {
            await StartSteam(args);
        }
    }

#pragma warning disable EPC27
    private static async void OnSteamExited(object? sender, EventArgs e)
#pragma warning restore EPC27
    {
        try
        {
            s_steamProcess?.Dispose();
            s_steamProcess = null;
            await StartSteam();
        }
        catch (Exception ex)
        {
            Log.Logger.Error("Error in OnSteamExited event handler");
            Log.Logger.Debug(ex);
        }
    }

    public static void StartMonitorSteam()
    {
        if (s_watcher != null)
        {
            return;
        }

        var dir = OperatingSystem.IsMacOS() ? SteamRootDir : SteamDir;

        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
        {
            Log.Logger.Warn($"Path {dir} does not exist, cannot monitor Steam state");
            return;
        }

        s_watcher = new FileSystemWatcherEx(dir) { Filter = ".crash" };
        s_watcher.OnCreated += OnCrashFileCreated;
        s_watcher.OnDeleted += OnCrashFileDeleted;
        s_watcher.OnChanged += OnCrashFileCreated;
        try
        {
            s_watcher.Start();
            Log.Logger.Info("Monitoring Steam state");
        }
        catch (Exception e)
        {
            Log.Logger.Error("Failed to start Steam monitorer");
            Log.Logger.Debug(e);
        }
    }

#pragma warning disable EPC27
    private static async void OnCrashFileCreated(object? sender, FileChangedEvent e)
#pragma warning restore EPC27
    {
        try
        {
            SteamStarted?.Invoke(null, EventArgs.Empty);
            if (!Settings.Default.InjectOnSteamStart && !s_injectOnce)
            {
                return;
            }
            s_injectOnce = false;
            await TryInject();
        }
        catch (Exception ex)
        {
            Log.Logger.Error("Error in OnCrashFileCreated event handler");
            Log.Logger.Debug(ex);
        }
    }

    private static void OnCrashFileDeleted(object? sender, FileChangedEvent e)
    {
        SteamStopped?.Invoke(null, EventArgs.Empty);
    }

    public static List<string> GetCommandLine()
    {
        return Utils.GetCommandLine(SteamProcess);
    }

    public static async Task RunTryInject()
    {
        Log.Logger.Info("Starting injection...");
        await Task.Run(TryInject);
    }

    public static async Task TryInject()
    {
        if (!await Semaphore.WaitAsync(TimeSpan.Zero))
        {
            Log.Logger.Warn("Injection already in progress");
            return;
        }

        try
        {
            if (!IsSteamRunning)
            {
                Log.Logger.Warn("Steam is not running, cannot inject");
                return;
            }

            if (Settings.Default.ForceSteamArgs)
            {
                var argumentsMissing = await CheckForMissingArgumentsAsync();
                if (argumentsMissing)
                {
                    return;
                }
            }

            while (!IsSteamWebHelperRunning)
            {
                if (!IsSteamRunning)
                {
                    Log.Logger.Warn("Steam is not running, cannot inject");
                    return;
                }
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            // wait a user specified amount of time to prevent injecting too early
            await Task.Delay(TimeSpan.FromSeconds(Settings.Default.InitialInjectionDelay));
            await Injector.StartInjectionAsync(true);
        }
        catch (Exception ex)
        {
            Log.Logger.Warn("Failed to inject");
            Log.Logger.Error(ex.ToString());
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private static async Task<bool> CheckForMissingArgumentsAsync()
    {
        if (!IsSteamRunning)
        {
            Log.Logger.Error("Steam is not running, cannot check arguments");
            return false;
        }

        var cmdLine = GetCommandLine();
        if (cmdLine.Count == 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            cmdLine = GetCommandLine();
            if (cmdLine.Count == 0)
            {
                Log.Logger.Error("Cannot check arguments. Steam process does not exist or is running with elevated permissions");
                return false;
            }
        }

        var args = Settings.Default.SteamLaunchArgs.Trim().ToLower();
        AppendArgs(ref args);

        var argumentMissing = args.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(arg => !cmdLine.Contains(arg));

        if (!argumentMissing)
        {
            return false;
        }

        s_injectOnce = true;
        Log.Logger.Info("Steam process detected with missing launch arguments, restarting...");
        await RestartSteam();
        return true;
    }

    private static void AppendArgs(ref string args)
    {
        const string debuggingString = "-cef-enable-debugging";
        const string bootstrapString = "-skipinitialbootstrap";
        const string portString = "-devtools-port";
        var argsList = args.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();

        if (!argsList.Contains(debuggingString))
        {
            argsList.Add(debuggingString);
        }

        if (OperatingSystem.IsMacOS() && !argsList.Contains(bootstrapString))
        {
            argsList.Add(bootstrapString);
        }

        if (!argsList.Contains(portString))
        {
            argsList.Add(portString + " " + Settings.Default.SteamCefPort);
        }

        args = string.Join(" ", argsList);
    }
}