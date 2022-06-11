using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using SFP;
using SFP.Models.FileSystemWatchers;

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
                    desktop.MainWindow.Title += $" v{UpdateCheckModel.Version}";
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        LogModel.Logger.Info($"Initializing SFP version {UpdateCheckModel.Version}");
                    });
                }
                catch (Exception e)
                {
                    LogModel.Logger.Error(e);
                };
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

        private async void OnProcessExit(object? sender, EventArgs e)
        {
            LinkModel.RemoveAllHardLinks();
            await FSWModel.StopFileWatchers();
            Models.ThemeChangeDetection.Linux.MonitorProcess?.Kill();
        }
    }
}
