using System.Diagnostics;
using System.Management;
using System.Reflection;
using System.Runtime.Versioning;
using WindowsShortcutFactory;
using File = System.IO.File;

namespace SFP.Models.Windows;

[SupportedOSPlatform("windows")]
public static class Utils
{
    public static bool SetAppRunOnLaunch(bool runOnLaunch)
    {
        string? processPath = Environment.ProcessPath;
        if (processPath == null)
        {
            Log.Logger.Error("Could not get process path, startup shortcut not created.");
            return false;
        }

        string processName = Path.GetFileNameWithoutExtension(processPath);
        string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string shortcutAddress = Path.Combine(startupFolder, processName + ".lnk");

        if (File.Exists(shortcutAddress) || !runOnLaunch)
        {
            File.Delete(shortcutAddress);
        }

        if (!runOnLaunch)
        {
            return true;
        }

        var shortcut = new WindowsShortcut { Path = processPath };
        shortcut.Save(shortcutAddress);
        return true;
    }

    public static List<string> GetCommandLine(Process process)
    {
        using ManagementObjectSearcher searcher = new("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id);
        using ManagementObjectCollection objects = searcher.Get();
        string? commandLine = objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
        return commandLine != null ? commandLine.Split(' ').ToList() : new List<string>();
    }
}
