#region

using System.Text;
using SFP.Models.ChromeCache.Simple;

#endregion

namespace SFP.Models.ChromeCache;

public static class Patcher
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

    public static async Task<(byte[], bool)> PatchFriendsCss(byte[] data, string fileName, bool alertOnPatched = false)
    {
        if (!Utils.IsGZipHeader(data))
        {
            Log.Logger.Warn($"{fileName} is not a valid gzip file");
            return (data, false);
        }

        byte[] bytes = await Utils.DecompressBytes(data);

        byte[] patchedTextBytes = Encoding.UTF8.GetBytes(LocalFile.PatchedText);
        if (bytes.Length > patchedTextBytes.Length &&
            Encoding.UTF8.GetString(bytes[..patchedTextBytes.Length]) == LocalFile.PatchedText)
        {
            if (!alertOnPatched)
            {
                Log.Logger.Info($"{fileName} is already patched.");
            }

            return (data, false);
        }

        await File.WriteAllBytesAsync(Path.Join(Steam.ClientUiDir, "friends.original.css"), bytes);

        const string appendText =
            LocalFile.PatchedText +
            "@import url(\"https://steamloopback.host/friends.original.css\");\n@import url(\"https://steamloopback.host/friends.custom.css\");\n{";
        byte[] append = Encoding.UTF8.GetBytes(appendText);
        bytes = append.Concat(bytes).Concat("}"u8.ToArray()).ToArray();

        string customFile = Path.Join(Steam.ClientUiDir, "friends.custom.css");
        if (!File.Exists(customFile))
        {
            await File.Create(customFile).DisposeAsync();
        }

        bytes = await Utils.CompressBytes(bytes);
        return (bytes, true);
    }
}
