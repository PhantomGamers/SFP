using System.Text;

using SFP.Models.ChromeCache.Simple;

namespace SFP.Models.ChromeCache
{
    public class Patcher
    {
        public static void PatchFilesInDirWithName(DirectoryInfo dir, string name)
        {
            if (!dir.Exists)
            {
                Log.Logger.Warn($"{dir.FullName} does not exist");
                return;
            }

            FileInfo[] dirFiles = dir.GetFiles();
            Log.Logger.Info($"Found {dirFiles.Length} files");
            foreach (FileInfo file in dirFiles.Where(f => f.Name.EndsWith("_0") && Parser.FileContainsName(f, name)))
            {
                Log.Logger.Info($"Found {name} in {file.Name}");
                _ = Task.Run(() => Simple.Patcher.PatchSimpleFile(Parser.GetSimpleFile(file)));
            }
        }

        public static async Task<(byte[], bool)> PatchFriendsCSS(byte[] data, string fileName, bool alertOnPatched = false)
        {
            if (!Utils.IsGZipHeader(data))
            {
                Log.Logger.Warn($"{fileName} is not a valid gzip file");
                return (data, false);
            }
            byte[] bytes = await Utils.DecompressBytes(data);

            byte[] patchedTextBytes = Encoding.UTF8.GetBytes(LocalFile.PATCHED_TEXT);
            if (bytes.Length > patchedTextBytes.Length && Encoding.UTF8.GetString(bytes[0..patchedTextBytes.Length]) == LocalFile.PATCHED_TEXT)
            {
                if (!alertOnPatched)
                {
                    Log.Logger.Info($"{fileName} is already patched.");
                }
                return (data, false);
            }
            File.WriteAllBytes(Path.Join(Steam.ClientUIDir, "friends.original.css"), bytes);

            const string appendText =
                LocalFile.PATCHED_TEXT + "@import url(\"https://steamloopback.host/friends.original.css\");\n@import url(\"https://steamloopback.host/friends.custom.css\");\n{";
            byte[] append = Encoding.UTF8.GetBytes(appendText);
            bytes = append.Concat(bytes).Concat(Encoding.UTF8.GetBytes("}")).ToArray();

            string? customFile = Path.Join(Steam.ClientUIDir, "friends.custom.css");
            if (!File.Exists(customFile))
            {
                File.Create(customFile).Dispose();
            }

            bytes = await Utils.CompressBytes(bytes);
            return (bytes, true);
        }
    }
}
