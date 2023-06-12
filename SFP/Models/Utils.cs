#region

using System.Diagnostics;

#endregion

namespace SFP.Models;

public static class Utils
{
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
                return;
            }

            url = $@"""{url}""";

            if (OperatingSystem.IsLinux())
            {
                _ = Process.Start("xdg-open", url);
            }
            else if (OperatingSystem.IsMacOS())
            {
                _ = Process.Start("open", $"-t {url}");
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
            Log.Logger.Warn("Could not get command line, process does not exist.");
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
