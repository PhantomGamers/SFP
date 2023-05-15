#region

using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using FileWatcherEx;
using NLog;
using NLog.Targets;
using SFP.Models;
using SFP_UI.Targets;
using SFP_UI.Views;

#endregion

namespace SFP_UI;

internal static class Program
{
    private static FileStream? s_fs;
    private static FileSystemWatcherEx? s_fw;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (!EnforceSingleInstance())
        {
            return;
        }
        LogManager.AutoShutdown = true;
        Target.Register("OutputControl", typeof(OutputControlTarget));
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        _ = new Settings();
        _ = BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
    }

    private static bool EnforceSingleInstance()
    {
        var tempPath = Path.GetTempPath();
        try
        {
            s_fs = new FileStream(Path.Combine(tempPath, "sfp_ui"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            File.Create(Path.Combine(tempPath, "sfp_ui_open"));
            return false;
        }
        _ = Task.Run(WatchInstanceFile);
        return true;
    }

    private static void WatchInstanceFile()
    {
        var tempPath = Path.GetTempPath();
        s_fw = new FileSystemWatcherEx(tempPath) { Filter = "sfp_ui_open" };
        s_fw.OnCreated += OnInstanceFileChanged;
        s_fw.OnChanged += OnInstanceFileChanged;
        s_fw.Start();
    }

    private static async void OnInstanceFileChanged(object? sender, FileChangedEvent e)
    {
        await Dispatcher.UIThread.InvokeAsync(MainWindow.ShowWindow);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace()
        .UseReactiveUI();

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) =>
        Log.Logger.Error(e.ExceptionObject);
}
