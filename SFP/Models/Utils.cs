#region

using System.Diagnostics;
using System.Drawing;

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
            return [];
        }

        if (OperatingSystem.IsWindows())
        {
            return Windows.Utils.GetCommandLine(process);
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return Unix.Utils.GetCommandLine(process);
        }

        return [];
    }

    // ReSharper disable once InconsistentNaming
    public static string ConvertARGBtoRGBA(string argb)
    {
        if (!argb.StartsWith("#"))
        {
            var color = Color.FromName(argb);
            if (color is { A: 0, R: 0, G: 0, B: 0 })
            {
                Log.Logger.Warn("Could not get color from {ColorName}", argb);
                return argb;
            }
            argb = $"#{color.ToArgb():x8}";
        }

        if (argb.Length is not 9)
        {
            Log.Logger.Warn("Could not convert {ColorName} to RGBA, unexpected format", argb);
            return argb;
        }

        var alpha = argb.Substring(1, 2);
        var rgb = argb[3..];
        return "#" + rgb + alpha;
    }

}
