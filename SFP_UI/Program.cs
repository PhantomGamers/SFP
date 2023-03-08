using Avalonia;
using Avalonia.ReactiveUI;
using FluentAvalonia.UI.Windowing;
using SFP.Models;

namespace SFP_UI
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            NLog.LogManager.AutoShutdown = true;
            NLog.Targets.Target.Register("OutputControl", typeof(Targets.OutputControlTarget));
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledException);
            _ = new Settings();
            _ = BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        private static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
                                                                 .UsePlatformDetect()
                                                                 .LogToTrace()
                                                                 .UseReactiveUI()
                                                                 .UseFAWindowing();

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) => Log.Logger.Error(e.ExceptionObject);
    }
}
