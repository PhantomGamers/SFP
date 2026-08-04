#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

#endregion

namespace SFP.Models.Unix;

[SupportedOSPlatform("linux")]
[SupportedOSPlatform("osx")]
public static class Utils
{
    public static List<string> GetCommandLine(Process process)
    {
        string processName = process.ProcessName;
        string command =
            $"pgrep -x {processName} | xargs sh -c 'if [ -n \"$1\" ]; then ps -o command= -p \"$1\"; fi' _";
        string output = RunCommand(command);
        string[] lines = output.ToLower()
            .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return [.. lines];
    }

    [SuppressMessage("CodeSmell", "ERP022:Unobserved exception in a generic exception handler")]
    private static string RunCommand(string command)
    {
        string output = string.Empty;

        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = new();
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