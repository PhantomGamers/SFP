#region

using System.Runtime.InteropServices;

#endregion

namespace SFP.Models.ChromeCache.Simple;

public static class Parser
{
    private static readonly int s_simpleFileHeaderSize = Marshal.SizeOf(typeof(SimpleFileHeader));
    private static readonly int s_simpleFileEofSize = Marshal.SizeOf(typeof(SimpleFileEof));

    private static readonly FileStreamOptions s_fso = new()
    {
        Access = FileAccess.Read,
        BufferSize = 1024,
        Mode = FileMode.Open,
        Options = FileOptions.SequentialScan,
        Share = FileShare.ReadWrite
    };

    public static bool FileContainsName(FileInfo file, string name)
    {
        using FileStream fs = file.Open(s_fso);
        using BinaryReader br = new(fs);
        SimpleFileHeader header = Utils.ByteArrayToStructure<SimpleFileHeader>(br.ReadBytes(s_simpleFileHeaderSize));
        string key = new(br.ReadChars((int)header.key_length));
        return key.Contains(name);
    }

    internal static SimpleFile GetSimpleFile(FileInfo file)
    {
        using FileStream fs = file.Open(s_fso);
        using BinaryReader br = new(fs);
        SimpleFileHeader header = Utils.ByteArrayToStructure<SimpleFileHeader>(br.ReadBytes(s_simpleFileHeaderSize));
        string key = new(br.ReadChars((int)header.key_length));

        _ = fs.Seek(-s_simpleFileEofSize, SeekOrigin.End);
        SimpleFileEof eof0 = Utils.ByteArrayToStructure<SimpleFileEof>(br.ReadBytes(s_simpleFileEofSize));
        _ = fs.Seek(-s_simpleFileEofSize, SeekOrigin.End);

        byte[] sha256 = Array.Empty<byte>();
        if (eof0.HasSHA256())
        {
            _ = fs.Seek(-32, SeekOrigin.Current);
            sha256 = br.ReadBytes(32);
            _ = fs.Seek(-32, SeekOrigin.Current);
        }

        _ = fs.Seek(-eof0.stream_size, SeekOrigin.Current);
        byte[] eof0Data = br.ReadBytes((int)eof0.stream_size);
        _ = fs.Seek(-eof0.stream_size, SeekOrigin.Current);

        _ = fs.Seek(-s_simpleFileEofSize, SeekOrigin.Current);
        SimpleFileEof eof1 = Utils.ByteArrayToStructure<SimpleFileEof>(br.ReadBytes(s_simpleFileEofSize));
        _ = fs.Seek(-s_simpleFileEofSize, SeekOrigin.Current);

        _ = fs.Seek(-eof1.stream_size, SeekOrigin.Current);
        byte[] eof1Data = br.ReadBytes((int)eof1.stream_size);

        return new SimpleFile(header, key, eof0Data, eof1Data, eof0, eof1, sha256, file);
    }
}
