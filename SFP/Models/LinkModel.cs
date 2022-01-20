namespace SFP
{
    public class LinkModel
    {
        private static readonly Dictionary<string, string> _hardLinks = new();

        public static FileInfo GetLink(FileInfo file)
        {
            if (_hardLinks.ContainsKey(file.FullName))
            {
                return new FileInfo(_hardLinks[file.FullName]);
            }

            return CreateHardLink(file);
        }

        public static FileInfo CreateHardLink(FileInfo file)
        {
            var linkPath = Path.Join(file.DirectoryName, "SFP");
            Directory.CreateDirectory(linkPath);
            var linkName = Path.Join(linkPath, file.Name);

            if (File.Exists(linkName))
            {
                if (!_hardLinks.ContainsValue(linkName))
                {
                    _hardLinks.Add(file.FullName, linkName);
                    return new FileInfo(linkName);
                }
            }

            NativeModel.CreateHardLink(linkName, file.FullName, IntPtr.Zero);
            _hardLinks.Add(file.FullName, linkName);
            return new FileInfo(linkName);
        }

        public static bool RemoveHardLink(string fileName)
        {
            if (_hardLinks.ContainsKey(fileName))
            {
                try
                {
                    File.Delete(_hardLinks[fileName]);
                }
                catch
                {
                    LogModel.Logger.Warn($"Could not delete {_hardLinks[fileName]} which links to {fileName}");
                    return false;
                }

                _hardLinks.Remove(fileName);
                return true;
            }
            return false;
        }

        public static bool RemoveAllHardLinks()
        {
            var result = true;
            foreach (var file in _hardLinks.Keys)
            {
                result &= RemoveHardLink(file);
            }
            return result;
        }
    }
}
