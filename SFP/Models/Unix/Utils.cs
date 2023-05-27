using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace SFP.Models.Unix;

[SupportedOSPlatform("linux")]
[SupportedOSPlatform("osx")]
public static class Utils
{
    public static List<string> GetCommandLine(Process process)
    {
        var processName = process.ProcessName;
        var command = $"pgrep -x {processName} | xargs -r ps -o command -p | tail -n 1";
        var output = RunCommand(command);
        var lines = output.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return lines.Select(line => line.Trim()).Where(arg => !string.IsNullOrWhiteSpace(arg)).ToList();
    }

    [SuppressMessage("CodeSmell", "ERP022:Unobserved exception in a generic exception handler")]
    private static string RunCommand(string command)
    {
        var output = string.Empty;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Log.Logger.Error("Error executing command: " + ex);
        }

        return output;
    }
}
