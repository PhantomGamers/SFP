using System.Runtime.InteropServices;

namespace SFP.Models.ChromeCache.Simple
{
    public class Parser
    {
        public static readonly int SimpleFileHeaderSize = Marshal.SizeOf(typeof(SimpleFileHeader));
        public static readonly int SimpleFileEOFSize = Marshal.SizeOf(typeof(SimpleFileEOF));

        private static readonly FileStreamOptions fso = new()
        {
            Access = FileAccess.Read,
            BufferSize = 1024,
            Mode = FileMode.Open,
            Options = FileOptions.SequentialScan,
            Share = FileShare.ReadWrite
        };

        public static bool FileContainsName(FileInfo file, string name)
        {
            using FileStream? fs = file.Open(fso);
            using var br = new BinaryReader(fs);
            SimpleFileHeader header = UtilsModel.ByteArrayToStructure<SimpleFileHeader>(br.ReadBytes(SimpleFileHeaderSize));
            string? key = new(br.ReadChars((int)header.key_length));
            return key?.Contains(name) ?? false;
        }

        internal static SimpleFile GetSimpleFile(FileInfo file)
        {
            using FileStream? fs = file.Open(fso);
            using var br = new BinaryReader(fs);
            SimpleFileHeader header = UtilsModel.ByteArrayToStructure<SimpleFileHeader>(br.ReadBytes(SimpleFileHeaderSize));
            string? key = new(br.ReadChars((int)header.key_length));

            fs.Seek(-SimpleFileEOFSize, SeekOrigin.End);
            SimpleFileEOF eof0 = UtilsModel.ByteArrayToStructure<SimpleFileEOF>(br.ReadBytes(SimpleFileEOFSize));
            fs.Seek(-SimpleFileEOFSize, SeekOrigin.End);

            byte[]? sha256 = Array.Empty<byte>();
            if (eof0.HasSHA256())
            {
                fs.Seek(-32, SeekOrigin.Current);
                sha256 = br.ReadBytes(32);
                fs.Seek(-32, SeekOrigin.Current);
            }

            fs.Seek(-eof0.stream_size, SeekOrigin.Current);
            byte[]? eof0_data = br.ReadBytes((int)eof0.stream_size);
            fs.Seek(-eof0.stream_size, SeekOrigin.Current);

            fs.Seek(-SimpleFileEOFSize, SeekOrigin.Current);
            SimpleFileEOF eof1 = UtilsModel.ByteArrayToStructure<SimpleFileEOF>(br.ReadBytes(SimpleFileEOFSize));
            fs.Seek(-SimpleFileEOFSize, SeekOrigin.Current);

            fs.Seek(-eof1.stream_size, SeekOrigin.Current);
            byte[]? eof1_data = br.ReadBytes((int)eof1.stream_size);

            return new(header, key, eof0_data, eof1_data, eof0, eof1, sha256, file);
        }
    }
}
