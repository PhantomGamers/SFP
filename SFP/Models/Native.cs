#region

using System.Runtime.InteropServices;

#endregion

namespace SFP.Models;

internal static partial class Native
{
    [LibraryImport("Kernel32.dll",
        StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CreateHardLinkW(
        string lpFileName,
        string lpExistingFileName,
        IntPtr lpSecurityAttributes
    );
}
