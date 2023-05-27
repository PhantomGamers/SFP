#region

using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;

#endregion

namespace SFP.Models;

public static class Utils
{
    [SupportedOSPlatform("windows")]
    public static object? GetRegistryData(string aKey, string aValueName)
    {
        using var registryKey = Registry.CurrentUser.OpenSubKey(aKey);
        object? value = null;
        var regValue = registryKey?.GetValue(aValueName);
        if (regValue != null)
        {
            value = regValue;
        }

        return value;
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

    public static List<string> GetCommandLine(Process? process)
    {
        if (process == null)
        {
            return new List<string>();
        }

        if (OperatingSystem.IsWindows())
        {
            return Windows.Utils.GetCommandLine(process);
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return Unix.Utils.GetCommandLine(process);
        }

        return new List<string>();
    }
}
