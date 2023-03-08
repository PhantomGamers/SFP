using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using SFP.Models;
using SFP.Models.FileSystemWatchers;

using SFP_UI.Models;
using SFP_UI.ViewModels;
using SFP_UI.Views;

namespace SFP_UI
{
    public partial class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

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
                    desktop.MainWindow.Title += $" v{UpdateChecker.Version}";
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Log.Logger.Info($"Initializing SFP version {UpdateChecker.Version}");
                    });
                }
                catch (Exception e)
                {
                    Log.Logger.Error(e);
                };
            }

            if (SFP.Properties.Settings.Default.CheckForUpdates)
            {
                await Dispatcher.UIThread.InvokeAsync(UpdateChecker.CheckForUpdates);
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
            _ = HardLink.RemoveAllHardLinks();
            await SFP.Models.FileSystemWatchers.FileSystemWatcher.StopFileWatchers();
        }
    }
}
