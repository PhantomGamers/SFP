#region

using Avalonia;
using Avalonia.ReactiveUI;
using NLog;
using NLog.Targets;
using SFP.Models;
using SFP_UI.Targets;

#endregion

namespace SFP_UI;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        LogManager.AutoShutdown = true;
        Target.Register("OutputControl", typeof(OutputControlTarget));
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        _ = new Settings();
        _ = BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace()
        .UseReactiveUI();

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) =>
        Log.Logger.Error(e.ExceptionObject);
}
