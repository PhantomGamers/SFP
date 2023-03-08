using System.Runtime.InteropServices;

//using Avalonia.Notification;

using NLog;

using ReactiveUI;

using SFP.Models;
using SFP.Models.ChromeCache.BlockFile;
using SFP.Models.FileSystemWatchers;

namespace SFP_UI.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        public static MainPageViewModel? Instance { get; private set; }

        public MainPageViewModel() => Instance = this;

        private bool _scannerActive = false;

        public bool ScannerActive
        {
            get => _scannerActive;
            private set => this.RaiseAndSetIfChanged(ref _scannerActive, value);
        }

        private static string s_output = string.Empty;

        public string Output
        {
            get => s_output;
            private set => this.RaiseAndSetIfChanged(ref s_output, value);
        }

        private int _caretIndex;

        public int CaretIndex
        {
            get => _caretIndex;
            private set => this.RaiseAndSetIfChanged(ref _caretIndex, value);
        }

        private bool _buttonsEnabled = true;

        public bool ButtonsEnabled
        {
            get => _buttonsEnabled;
            set => this.RaiseAndSetIfChanged(ref _buttonsEnabled, value);
        }

        public void PrintLine(LogLevel level, string message) => Print(level, $"{message}\n");

        public void Print(LogLevel level, string message)
        {
            Output = string.Concat(Output, $"[{DateTime.Now}][{level}] {message}");
            CaretIndex = Output.Length;
        }

        public static async void OnPatchCommand()
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
            if (SFP.Properties.Settings.Default.ShouldPatchFriends)
            {
                if (OperatingSystem.IsWindows())
                {
                    var cacheFiles = await Task.Run(() => Parser.FindCacheFilesWithName(new DirectoryInfo(Steam.CacheDir), "friends.css"));
                    if (!SFP.Properties.Settings.Default.ScanOnly)
                    {
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

                _ = await Task.Run(() => LocalFile.Patch(new FileInfo(Path.Join(Steam.ClientUIDir, "css", "friends.css")), uiDir: Steam.ClientUIDir));
            }

            if (SFP.Properties.Settings.Default.ShouldPatchLibrary)
            {
                var dir = new DirectoryInfo(Steam.SteamUICSSDir);
                await Task.Run(() => LocalFile.PatchAll(dir, "libraryroot.custom.css"));
            }

            if (SFP.Properties.Settings.Default.ShouldPatchResources)
            {
                await Task.Run(() => Resource.ReplaceAllFiles());
            }

            if (cacheFilesPatched && SFP.Properties.Settings.Default.RestartSteamOnPatch)
            {
                await Task.Run(() => Steam.RestartSteam());
            }

            if (Instance != null)
            {
                Instance.ButtonsEnabled = true;
            }
        }

        public static async void OnScanCommand()
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

            if (!SFP.Properties.Settings.Default.ShouldScanFriends && !SFP.Properties.Settings.Default.ShouldScanLibrary)
            {
                Log.Logger.Warn("No scan targets enabled");
                if (Instance != null)
                {
                    Instance.ButtonsEnabled = true;
                }
                return;
            }

            await SFP.Models.FileSystemWatchers.FileSystemWatcher.StartFileWatchers();

            Log.Logger.Info(SFP.Models.FileSystemWatchers.FileSystemWatcher.WatchersActive ? "Scanner started" : "Scanner could not be started");
            if (Instance != null)
            {
                Instance.ScannerActive = SFP.Models.FileSystemWatchers.FileSystemWatcher.WatchersActive;
                Instance.ButtonsEnabled = true;
            }
        }

        public static async void OnStopScanCommand()
        {
            if (Instance != null)
            {
                Instance.ButtonsEnabled = false;
            }

            await Task.Run(() => SFP.Models.FileSystemWatchers.FileSystemWatcher.StopFileWatchers());

            Log.Logger.Info(!SFP.Models.FileSystemWatchers.FileSystemWatcher.WatchersActive ? "Scanner stopped" : "Scanner could not be stopped");

            if (Instance != null)
            {
                Instance.ScannerActive = SFP.Models.FileSystemWatchers.FileSystemWatcher.WatchersActive;
                Instance.ButtonsEnabled = true;
            }
        }

        public static async void OnResetSteamCommand()
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

        public static void OnOpenFriendsCustomCssCommand()
        {
            string file = Path.Join(Steam.ClientUIDir, "friends.custom.css");
            try
            {
                if (!File.Exists(file))
                {
                    File.Create(file).Dispose();
                }
                Utils.OpenUrl(file);
            }
            catch (Exception e)
            {
                Log.Logger.Warn($"Could not open friends.custom.css");
                Log.Logger.Error(e);
            }
        }

        public static void OnOpenLibraryrootCustomCssCommand()
        {
            string file = Path.Join(Steam.SteamUIDir, "libraryroot.custom.css");
            try
            {
                if (!File.Exists(file))
                {
                    File.Create(file).Dispose();
                }
                Utils.OpenUrl(file);
            }
            catch (Exception e)
            {
                Log.Logger.Warn($"Could not open libraryroot.custom.css");
                Log.Logger.Error(e);
            }
        }
    }
}
