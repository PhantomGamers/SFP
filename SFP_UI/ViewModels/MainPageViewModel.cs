using System.Runtime.InteropServices;

using Avalonia.Notification;

using NLog;

using ReactiveUI;

using SFP;
using SFP.Models.FileSystemWatchers;

using SFP_UI.Models;

namespace SFP_UI.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        public static MainPageViewModel? Instance { get; private set; }

        public INotificationMessageManager Manager { get; } = new NotificationMessageManager();

        public MainPageViewModel()
        {
            Instance = this;
        }

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

        public void PrintLine(LogLevel level, string message)
        {
            Print(level, $"{message}\n");
        }

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

            if (SteamModel.SteamDir == null)
            {
                LogModel.Logger.Warn("Steam Directory unknown. Please set it and try again.");
                if (Instance != null)
                {
                    Instance.ButtonsEnabled = true;
                }
                return;
            }

            bool cacheFilesPatched = false;
            if (SFP.Properties.Settings.Default.ShouldPatchFriends)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var cacheFiles = await Task.Run(() => SFP.ChromeCache.BlockFile.Parser.FindCacheFilesWithName(new DirectoryInfo(SteamModel.CacheDir), "friends.css"));
                    if (!SFP.Properties.Settings.Default.ScanOnly)
                    {
                        foreach (FileInfo? cacheFile in cacheFiles)
                        {
                            cacheFilesPatched |= await Task.Run(() => SFP.ChromeCache.BlockFile.Patcher.PatchFile(cacheFile));
                        }
                    }
                }
                else
                {
                    LogModel.Logger.Info($"Friends patching is not supported on {RuntimeInformation.RuntimeIdentifier}.");
                    //SFP.Models.ChromeCache.Patcher.PatchFilesInDirWithName(new DirectoryInfo(SteamModel.CacheDir), "friends.css");
                }

                await Task.Run(() => LocalFileModel.Patch(new FileInfo(Path.Join(SteamModel.ClientUIDir, "css", "friends.css")), uiDir: SteamModel.ClientUIDir));
            }

            if (SFP.Properties.Settings.Default.ShouldPatchLibrary)
            {
                var dir = new DirectoryInfo(SteamModel.SteamUICSSDir);
                await Task.Run(() => LocalFileModel.PatchAll(dir, "libraryroot.custom.css"));
            }

            if (cacheFilesPatched && SFP.Properties.Settings.Default.RestartSteamOnPatch)
            {
                await Task.Run(() => SteamModel.RestartSteam());
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

            if (SteamModel.SteamDir == null)
            {
                LogModel.Logger.Warn("Steam Directory unknown. Please set it and try again.");
                if (Instance != null)
                {
                    Instance.ButtonsEnabled = true;
                }
                return;
            }

            if (!SFP.Properties.Settings.Default.ShouldScanFriends && !SFP.Properties.Settings.Default.ShouldScanLibrary)
            {
                LogModel.Logger.Warn("No scan targets enabled");
                if (Instance != null)
                {
                    Instance.ButtonsEnabled = true;
                }
                return;
            }

            await FSWModel.StartFileWatchers();

            LogModel.Logger.Info(FSWModel.WatchersActive ? "Scanner started" : "Scanner could not be started");
            if (Instance != null)
            {
                Instance.ScannerActive = FSWModel.WatchersActive;
                Instance.ButtonsEnabled = true;
            }
        }

        public static async void OnStopScanCommand()
        {
            if (Instance != null)
            {
                Instance.ButtonsEnabled = false;
            }

            await Task.Run(() => FSWModel.StopFileWatchers());

            LogModel.Logger.Info(!FSWModel.WatchersActive ? "Scanner stopped" : "Scanner could not be stopped");

            if (Instance != null)
            {
                Instance.ScannerActive = FSWModel.WatchersActive;
                Instance.ButtonsEnabled = true;
            }
        }

        public static async void OnResetSteamCommand()
        {
            if (Instance != null)
            {
                Instance.ButtonsEnabled = false;
            }

            await Task.Run(SteamModel.ResetSteam);

            if (Instance != null)
            {
                Instance.ButtonsEnabled = true;
            }
        }

        public static void OnOpenFriendsCustomCssCommand()
        {
            string file = Path.Join(SteamModel.ClientUIDir, "friends.custom.css");
            try
            {
                if (!File.Exists(file))
                {
                    File.Create(file).Dispose();
                }
                UtilsModel.OpenUrl(file);
            }
            catch (Exception e)
            {
                LogModel.Logger.Warn($"Could not open friends.custom.css");
                LogModel.Logger.Error(e);
            }
        }

        public static void OnOpenLibraryrootCustomCssCommand()
        {
            string file = Path.Join(SteamModel.SteamUIDir, "libraryroot.custom.css");
            try
            {
                if (!File.Exists(file))
                {
                    File.Create(file).Dispose();
                }
                UtilsModel.OpenUrl(file);
            }
            catch (Exception e)
            {
                LogModel.Logger.Warn($"Could not open libraryroot.custom.css");
                LogModel.Logger.Error(e);
            }
        }
    }
}
