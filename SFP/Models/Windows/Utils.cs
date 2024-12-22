#region

using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;
using WindowsShortcutFactory;
using WmiLight;

#endregion

namespace SFP.Models.Windows;

[SupportedOSPlatform("windows")]
public static class Utils
{
    public static void SetAppRunOnLaunch(bool runOnLaunch)
    {
        var processPath = Environment.ProcessPath;
        if (processPath == null)
        {
            Log.Logger.Error("Could not get process path, startup shortcut not created.");
            return;
        }

        var processName = Path.GetFileNameWithoutExtension(processPath);
        var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        var shortcutAddress = Path.Combine(startupFolder, processName + ".lnk");

        if (File.Exists(shortcutAddress) || !runOnLaunch)
        {
            File.Delete(shortcutAddress);
        }

        if (!runOnLaunch)
        {
            return;
        }

        using WindowsShortcut shortcut = new() { Path = processPath };
        shortcut.Save(shortcutAddress);
    }

    public static List<string> GetCommandLine(Process process)
    {
        using WmiConnection con = new();
        var query = con.CreateQuery("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id);
        var commandLine = query.SingleOrDefault()?["CommandLine"]?.ToString();
        return commandLine != null
            ? [.. commandLine.ToLower().Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)]
            : [];
    }

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
}
