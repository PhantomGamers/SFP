using System.Runtime.InteropServices;

namespace SFP
{
    public class LinkModel
    {
        private static readonly Dictionary<string, string> s_hardLinks = new();

        public static FileInfo GetLink(FileInfo file)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return file;
            }

            if (s_hardLinks.ContainsKey(file.FullName))
            {
                return new FileInfo(s_hardLinks[file.FullName]);
            }

            return CreateHardLink(file);
        }

        public static FileInfo CreateHardLink(FileInfo file)
        {
            string? linkPath = Path.Join(file.DirectoryName, "SFP");
            Directory.CreateDirectory(linkPath);
            string? linkName = Path.Join(linkPath, file.Name);

            if (File.Exists(linkName))
            {
                if (!s_hardLinks.ContainsValue(linkName))
                {
                    s_hardLinks.Add(file.FullName, linkName);
                    return new FileInfo(linkName);
                }
            }

            NativeModel.CreateHardLink(linkName, file.FullName, IntPtr.Zero);
            s_hardLinks.Add(file.FullName, linkName);
            return new FileInfo(linkName);
        }

        public static bool RemoveHardLink(string fileName)
        {
            if (s_hardLinks.ContainsKey(fileName))
            {
                try
                {
                    File.Delete(s_hardLinks[fileName]);
                }
                catch
                {
                    LogModel.Logger.Warn($"Could not delete {s_hardLinks[fileName]} which links to {fileName}");
                    return false;
                }

                s_hardLinks.Remove(fileName);
                return true;
            }
            return false;
        }

        public static bool RemoveAllHardLinks()
        {
            bool result = true;
            foreach (string? file in s_hardLinks.Keys)
            {
                result &= RemoveHardLink(file);
            }
            return result;
        }
    }
}
