#region

using System.Reactive;
using System.Runtime.InteropServices;
using NLog;
using ReactiveUI;
using SFP.Models;
using SFP.Models.ChromeCache.BlockFile;
using FileSystemWatcher = SFP.Models.FileSystemWatchers.FileSystemWatcher;
using Settings = SFP.Properties.Settings;

#endregion

namespace SFP_UI.ViewModels;

public class MainPageViewModel : ViewModelBase
{
    private string _output = string.Empty;

    private bool _buttonsEnabled = true;

    private int _caretIndex;

    private bool _scannerActive;

    private string _updateNotificationContent = string.Empty;
    private bool _updateNotificationIsOpen;

    public MainPageViewModel() => Instance = this;
    public static MainPageViewModel? Instance { get; private set; }

    public ReactiveCommand<Unit, Unit> PatchCommand { get; } = ReactiveCommand.CreateFromTask(OnPatchCommand);
    public ReactiveCommand<Unit, Unit> StartScanCommand { get; } = ReactiveCommand.CreateFromTask(OnScanCommand);
    public ReactiveCommand<Unit, Unit> StopScanCommand { get; } = ReactiveCommand.CreateFromTask(OnStopScanCommand);
    public ReactiveCommand<Unit, Unit> ResetSteamCommand { get; } = ReactiveCommand.CreateFromTask(OnResetSteamCommand);

    public ReactiveCommand<Unit, Unit> OpenFriendsCustomCssCommand { get; } =
        ReactiveCommand.CreateFromTask(OnOpenFriendsCustomCssCommand);

    public ReactiveCommand<Unit, Unit> OpenLibraryRootCustomCssCommand { get; } =
        ReactiveCommand.CreateFromTask(OnOpenLibraryRootCustomCssCommand);

    public ReactiveCommand<string, Unit> UpdateNotificationViewCommand { get; } =
        ReactiveCommand.Create<string>(Utils.OpenUrl);

    public bool UpdateNotificationIsOpen
    {
        get => _updateNotificationIsOpen;
        set => this.RaiseAndSetIfChanged(ref _updateNotificationIsOpen, value);
    }

    public string UpdateNotificationContent
    {
        get => _updateNotificationContent;
        set => this.RaiseAndSetIfChanged(ref _updateNotificationContent, value);
    }

    public bool ScannerActive
    {
        get => _scannerActive;
        private set => this.RaiseAndSetIfChanged(ref _scannerActive, value);
    }

    public string Output
    {
        get => _output;
        private set => this.RaiseAndSetIfChanged(ref _output, value);
    }

    public int CaretIndex
    {
        get => _caretIndex;
        private set => this.RaiseAndSetIfChanged(ref _caretIndex, value);
    }

    public bool ButtonsEnabled
    {
        get => _buttonsEnabled;
        set => this.RaiseAndSetIfChanged(ref _buttonsEnabled, value);
    }

    public void PrintLine(LogLevel level, string message) => Print(level, $"{message}\n");

    private void Print(LogLevel level, string message)
    {
        Output = string.Concat(Output, $"[{DateTime.Now}][{level}] {message}");
        CaretIndex = Output.Length;
    }

    public static async Task OnPatchCommand()
    {
        if (Instance != null)
        {
            Instance.ButtonsEnabled = false;
        }

        if (Steam.SteamDir == null)
        {
            Log.Logger.Warn("Steam Directory unknown. Please set it and try again.");
            if (Instance != null)
            {
                Instance.ButtonsEnabled = true;
            }

            return;
        }

        bool cacheFilesPatched = false;
        if (Settings.Default.ShouldPatchFriends)
        {
            if (OperatingSystem.IsWindows())
            {
                List<FileInfo> cacheFiles = await Task.Run(() =>
                    Parser.FindCacheFilesWithName(new DirectoryInfo(Steam.CacheDir), "friends.css"));
                if (!Settings.Default.ScanOnly)
                {
                    // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                    foreach (FileInfo? cacheFile in cacheFiles)
                    {
                        cacheFilesPatched |= await Task.Run(() => Patcher.PatchFile(cacheFile));
                    }
                }
            }
            else
            {
                Log.Logger.Info($"Friends patching is not supported on {RuntimeInformation.RuntimeIdentifier}.");
                //SFP.Models.ChromeCache.Patcher.PatchFilesInDirWithName(new DirectoryInfo(SteamModel.CacheDir), "friends.css");
            }

            _ = await Task.Run(() => LocalFile.Patch(new FileInfo(Path.Join(Steam.ClientUiDir, "css", "friends.css")),
                uiDir: Steam.ClientUiDir));
        }

        if (Settings.Default.ShouldPatchLibrary)
        {
            DirectoryInfo dir = new(Steam.SteamUiCssDir);
            await Task.Run(() => LocalFile.PatchAll(dir, @"libraryroot.custom.css"));
        }

        if (Settings.Default.ShouldPatchResources)
        {
            await Task.Run(Resource.ReplaceAllFiles);
        }

        if (cacheFilesPatched && Settings.Default.RestartSteamOnPatch)
        {
            await Task.Run(Steam.RestartSteam);
        }

        if (Instance != null)
        {
            Instance.ButtonsEnabled = true;
        }
    }

    public static async Task OnScanCommand()
    {
        if (Instance != null)
        {
            Instance.ButtonsEnabled = false;
        }

        if (Steam.SteamDir == null)
        {
            Log.Logger.Warn("Steam Directory unknown. Please set it and try again.");
            if (Instance != null)
            {
                Instance.ButtonsEnabled = true;
            }

            return;
        }

        if (!Settings.Default.ShouldScanFriends && !Settings.Default.ShouldScanLibrary)
        {
            Log.Logger.Warn("No scan targets enabled");
            if (Instance != null)
            {
                Instance.ButtonsEnabled = true;
            }

            return;
        }

        await FileSystemWatcher.StartFileWatchers();

        Log.Logger.Info(FileSystemWatcher.WatchersActive ? "Scanner started" : "Scanner could not be started");
        if (Instance != null)
        {
            Instance.ScannerActive = FileSystemWatcher.WatchersActive;
            Instance.ButtonsEnabled = true;
        }
    }

    private static async Task OnStopScanCommand()
    {
        if (Instance != null)
        {
            Instance.ButtonsEnabled = false;
        }

        await Task.Run(FileSystemWatcher.StopFileWatchers);

        Log.Logger.Info(!FileSystemWatcher.WatchersActive ? "Scanner stopped" : "Scanner could not be stopped");

        if (Instance != null)
        {
            Instance.ScannerActive = FileSystemWatcher.WatchersActive;
            Instance.ButtonsEnabled = true;
        }
    }

    private static async Task OnResetSteamCommand()
    {
        if (Instance != null)
        {
            Instance.ButtonsEnabled = false;
        }

        await Task.Run(Steam.ResetSteam);

        if (Instance != null)
        {
            Instance.ButtonsEnabled = true;
        }
    }

    private static async Task OnOpenFriendsCustomCssCommand()
    {
        string file = Path.Join(Steam.ClientUiDir, "friends.custom.css");
        try
        {
            if (!File.Exists(file))
            {
                await File.Create(file).DisposeAsync();
            }

            Utils.OpenUrl(file);
        }
        catch (Exception e)
        {
            Log.Logger.Warn("Could not open friends.custom.css");
            Log.Logger.Error(e);
        }
    }

    private static async Task OnOpenLibraryRootCustomCssCommand()
    {
        string file = Path.Join(Steam.SteamUiDir, @"libraryroot.custom.css");
        try
        {
            if (!File.Exists(file))
            {
                await File.Create(file).DisposeAsync();
            }

            Utils.OpenUrl(file);
        }
        catch (Exception e)
        {
            Log.Logger.Warn("Could not open libraryroot.custom.css");
            Log.Logger.Error(e);
        }
    }
}
