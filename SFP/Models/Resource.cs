using FileWatcherEx;

using SFP.Models.FileSystemWatchers;

namespace SFP.Models
{
    public class Resource
    {
        private const string PatchedText = "// PATCHED\n";

        private static readonly DelayedWatcher s_watcher = new(Steam.ResourceDir, OnPostEviction, GetKey)
        {
            IncludeSubdirectories = true
        };

        private static string s_overrideDir => Path.Join(Steam.SkinDir, "override");
        private static (bool, string?) GetKey(FileChangedEvent e) => (true, Path.GetRelativePath(Steam.ResourceDir!, e.FullPath));
        private static async void OnPostEviction(FileChangedEvent e) => await ReplaceFile(e.FullPath, GetCustomPath(e.FullPath), true);
        private static string GetCustomPath(string filePath) => Path.Combine(s_overrideDir, Path.GetRelativePath(Steam.ResourceDir!, filePath));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeSmell", "ERP022:Unobserved exception in generic exception handler", Justification = "exception handled")]
        private static async Task<bool> ReplaceFile(string oldFile, string replacementFile, bool alertOnPatched = false)
        {
            if (!File.Exists(oldFile))
            {
                if (!alertOnPatched)
                {
                    Log.Logger.Warn($"{oldFile} does not exist");
                }
                return false;
            }

            if (!File.Exists(replacementFile))
            {
                if (!alertOnPatched)
                {
                    Log.Logger.Warn($"{replacementFile} does not exist");
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
                        Log.Logger.Info($"{oldFile} is already patched.");
                    }
                    return false;
                }
                file = await File.ReadAllTextAsync(replacementFile);
                file = PatchedText + file;
                await File.WriteAllTextAsync(oldFile, file);
                Log.Logger.Info($"Patched {oldFile}");
                return true;
            }
            catch
            {
                Log.Logger.Info($"Could not patch file {oldFile}");
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
                Log.Logger.Info("Patching resource files...");
            }
            List<Task> tasks = new();
            foreach (string? file in customFiles)
            {
                string? relativeFile = Path.GetRelativePath(customPath, file);
                string? steamFile = Path.Join(Steam.ResourceDir, relativeFile);
                if (!File.Exists(steamFile))
                {
                    if (!silent)
                    {
                        Log.Logger.Info($"{steamFile} does not exist, skipping...");
                    }
                    continue;
                }
                tasks.Add(ReplaceFile(steamFile, file));
            }
            await Task.WhenAll(tasks);
        }

        public static void Watch() => s_watcher.Start(Steam.ResourceDir);
        public static void StopWatching() => s_watcher.Stop();
    }
}
