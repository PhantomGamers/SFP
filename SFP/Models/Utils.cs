#region

using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

#endregion

namespace SFP.Models;

public static class Utils
{
    public static bool IsGZipHeader(IReadOnlyList<byte> arr) => arr is [31, 139, ..];

    public static async Task<byte[]> CompressBytes(IEnumerable<byte> bytes)
    {
        using MemoryStream compressedMemoryStream = new();
        using MemoryStream originalMemoryStream = new(bytes.ToArray());
        await using (GZipStream compressor = new(compressedMemoryStream, CompressionLevel.Fastest))
        {
            await originalMemoryStream.CopyToAsync(compressor);
        }

        return compressedMemoryStream.ToArray();
    }

    public static async Task<byte[]> DecompressBytes(IEnumerable<byte> bytes)
    {
        using MemoryStream compressedMemoryStream = new(bytes.ToArray());
        using MemoryStream originalMemoryStream = new();
        await using (GZipStream decompressor = new(compressedMemoryStream, CompressionMode.Decompress))
        {
            await decompressor.CopyToAsync(originalMemoryStream);
        }

        return originalMemoryStream.ToArray();
    }

    [SupportedOSPlatform("windows")]
    public static object? GetRegistryData(string aKey, string aValueName)
    {
        using RegistryKey? registryKey = Registry.CurrentUser.OpenSubKey(aKey);
        object? value = null;
        object? regValue = registryKey?.GetValue(aValueName);
        if (regValue != null)
        {
            value = regValue;
        }

        return value;
    }

    // from https://stackoverflow.com/a/41836532
    public static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
    {
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public static byte[] StructureToByteArray<T>(T structure) where T : struct
    {
        GCHandle handle = GCHandle.Alloc(structure, GCHandleType.Pinned);
        int size = Marshal.SizeOf(typeof(T));
        byte[] array = new byte[size];
        try
        {
            Marshal.StructureToPtr(structure, handle.AddrOfPinnedObject(), true);
            Marshal.Copy(handle.AddrOfPinnedObject(), array, 0, size);
            return array;
        }
        finally
        {
            handle.Free();
        }
    }

    public static void OpenUrl(string url)
    {
        try
        {
            _ = Process.Start(url);
        }
        catch (Exception e)
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (OperatingSystem.IsWindows())
            {
                url = url.Replace("&", "^&");
                _ = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (OperatingSystem.IsLinux())
            {
                _ = Process.Start("xdg-open", url);
            }
            else if (OperatingSystem.IsMacOS())
            {
                _ = Process.Start("open", url);
            }
            else
            {
                Log.Logger.Error(e);
                throw;
            }
        }
    }
}
