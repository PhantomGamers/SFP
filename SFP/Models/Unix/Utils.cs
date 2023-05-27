using System.Diagnostics;
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
        var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        return lines.Select(line => line.Trim()).Where(arg => !string.IsNullOrWhiteSpace(arg)).ToList();
    }

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
