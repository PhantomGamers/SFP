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
                LogModel.Logger.Warn($"{dir.FullName} does not exist");
                return;
            }

            FileInfo[] dirFiles = dir.GetFiles();
            LogModel.Logger.Info($"Found {dirFiles.Length} files");
            foreach (FileInfo file in dirFiles.Where(f => f.Name.EndsWith("_0") && Parser.FileContainsName(f, name)))
            {
                LogModel.Logger.Info($"Found {name} in {file.Name}");
                _ = Task.Run(() => Simple.Patcher.PatchSimpleFile(Parser.GetSimpleFile(file)));
            }
        }

        public static async Task<(byte[], bool)> PatchFriendsCSS(byte[] data, string fileName, bool alertOnPatched = false)
        {
            if (!UtilsModel.IsGZipHeader(data))
            {
                LogModel.Logger.Warn($"{fileName} is not a valid gzip file");
                return (data, false);
            }
            byte[] bytes = await UtilsModel.DecompressBytes(data);

            byte[] patchedTextBytes = Encoding.UTF8.GetBytes(LocalFileModel.PATCHED_TEXT);
            if (bytes.Length > patchedTextBytes.Length && Encoding.UTF8.GetString(bytes[0..patchedTextBytes.Length]) == LocalFileModel.PATCHED_TEXT)
            {
                if (!alertOnPatched)
                {
                    LogModel.Logger.Info($"{fileName} is already patched.");
                }
                return (data, false);
            }
            File.WriteAllBytes(Path.Join(SteamModel.ClientUIDir, "friends.original.css"), bytes);

            const string appendText =
                LocalFileModel.PATCHED_TEXT + "@import url(\"https://steamloopback.host/friends.original.css\");\n@import url(\"https://steamloopback.host/friends.custom.css\");\n{";
            byte[] append = Encoding.UTF8.GetBytes(appendText);
            bytes = append.Concat(bytes).Concat(Encoding.UTF8.GetBytes("}")).ToArray();

            string? customFile = Path.Join(SteamModel.ClientUIDir, "friends.custom.css");
            if (!File.Exists(customFile))
            {
                File.Create(customFile).Dispose();
            }

            bytes = await UtilsModel.CompressBytes(bytes);
            return (bytes, true);
        }
    }
}
