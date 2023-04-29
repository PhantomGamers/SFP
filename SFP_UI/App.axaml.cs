#region

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using SFP.Models;
using SFP_UI.Models;
using SFP_UI.ViewModels;
using SFP_UI.Views;
using Settings = SFP.Properties.Settings;

#endregion

namespace SFP_UI;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel() };

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
            }
        }

        if (Settings.Default.CheckForUpdates)
        {
            await Dispatcher.UIThread.InvokeAsync(UpdateChecker.CheckForUpdates);
        }

        if (SettingsPageViewModel.Instance != null)
        {
            await Dispatcher.UIThread.InvokeAsync(SettingsPageViewModel.Instance.OnReloadCommand);
            await Dispatcher.UIThread.InvokeAsync(SettingsPageViewModel.OnSaveCommand);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
