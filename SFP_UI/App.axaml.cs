using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using SFP;

using SFP_UI.Models;
using SFP_UI.ViewModels;
using SFP_UI.Views;

namespace SFP_UI
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };

                try
                {
                    LogModel.Logger.Info($"Version is {UpdateCheckModel.Version}");
                    desktop.MainWindow.Title += $" v{UpdateCheckModel.Version}";
                }
                catch { };
            }

            if (SFP.Properties.Settings.Default.CheckForUpdates)
            {
                await Dispatcher.UIThread.InvokeAsync(UpdateCheckModel.CheckForUpdates);
            }

            if (SettingsPageViewModel.Instance != null)
            {
                await Dispatcher.UIThread.InvokeAsync(SettingsPageViewModel.Instance.OnReloadCommand);
                await Dispatcher.UIThread.InvokeAsync(SettingsPageViewModel.OnSaveCommand);
            }

            if (SFP.Properties.Settings.Default.ShouldPatchOnStart)
            {
                await Dispatcher.UIThread.InvokeAsync(MainPageViewModel.OnPatchCommand);
            }

            if (SFP.Properties.Settings.Default.ShouldScanOnStart)
            {
                await Dispatcher.UIThread.InvokeAsync(MainPageViewModel.OnScanCommand);
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnProcessExit(object? sender, EventArgs e)
        {
            LinkModel.RemoveAllHardLinks();
            FSWModel.RemoveAllWatchers();
            if (MainWindow.Instance != null)
            {
                MainWindow.Instance.trayIcon.IsVisible = false;
                MainWindow.Instance.trayIcon.Dispose();
            }
        }
    }
}
