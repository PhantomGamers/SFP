using FileWatcherEx;

using SFP.Models.FileSystemWatchers;

namespace SFP.Models
{
    public class ResourceModel
    {
        private const string PatchedText = "// PATCHED\n";

        private static readonly DelayedWatcher s_watcher = new(SteamModel.ResourceDir, OnPostEviction, GetKey)
        {
            IncludeSubdirectories = true
        };

        private static string s_overrideDir => Path.Join(SteamModel.SkinDir, "override");
        private static (bool, string?) GetKey(FileChangedEvent e) => (true, Path.GetRelativePath(SteamModel.ResourceDir!, e.FullPath));
        private static async void OnPostEviction(FileChangedEvent e) => await ReplaceFile(e.FullPath, GetCustomPath(e.FullPath), true);
        private static string GetCustomPath(string filePath) => Path.Combine(s_overrideDir, Path.GetRelativePath(SteamModel.ResourceDir!, filePath));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeSmell", "ERP022:Unobserved exception in generic exception handler", Justification = "exception handled")]
        private static async Task<bool> ReplaceFile(string oldFile, string replacementFile, bool alertOnPatched = false)
        {
            if (!File.Exists(oldFile))
            {
                if (!alertOnPatched)
                {
                    LogModel.Logger.Warn($"{oldFile} does not exist");
                }
                return false;
            }

            if (!File.Exists(replacementFile))
            {
                if (!alertOnPatched)
                {
                    LogModel.Logger.Warn($"{replacementFile} does not exist");
                }
                return false;
            }

            try
            {
                string? file = await File.ReadAllTextAsync(oldFile);
                if (file.StartsWith(PatchedText))
                {
                    if (!alertOnPatched)
                    {
                        LogModel.Logger.Info($"{oldFile} is already patched.");
                    }
                    return false;
                }
                file = await File.ReadAllTextAsync(replacementFile);
                file = PatchedText + file;
                await File.WriteAllTextAsync(oldFile, file);
                LogModel.Logger.Info($"Patched {oldFile}");
                return true;
            }
            catch
            {
                LogModel.Logger.Info($"Could not patch file {oldFile}");
            }
            return false;
        }

        public static async Task ReplaceAllFiles()
        {
            if (Directory.Exists(s_overrideDir))
            {
                await ReplaceAllFiles(Directory.EnumerateFiles(s_overrideDir, "*.*", SearchOption.AllDirectories), s_overrideDir);
            }
        }

        private static async Task ReplaceAllFiles(IEnumerable<string> customFiles, string customPath, bool silent = false)
        {
            if (!silent)
            {
                LogModel.Logger.Info("Patching resource files...");
            }
            List<Task> tasks = new();
            foreach (string? file in customFiles)
            {
                string? relativeFile = Path.GetRelativePath(customPath, file);
                string? steamFile = Path.Join(SteamModel.ResourceDir, relativeFile);
                if (!File.Exists(steamFile))
                {
                    if (!silent)
                    {
                        LogModel.Logger.Info($"{steamFile} does not exist, skipping...");
                    }
                    continue;
                }
                tasks.Add(ReplaceFile(steamFile, file));
            }
            await Task.WhenAll(tasks);
        }

        public static void Watch() => s_watcher.Start(SteamModel.ResourceDir);
        public static void StopWatching() => s_watcher.Stop();
    }
}
