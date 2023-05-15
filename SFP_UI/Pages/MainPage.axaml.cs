#region

using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using SFP.Models;
using SFP.Models.Injection.Config;
using SFP_UI.ViewModels;
using Settings = SFP.Properties.Settings;

#endregion

namespace SFP_UI.Pages;

public partial class MainPage : UserControl
{
    public MainPage()
    {
        InitializeComponent();
        DataContext = new MainPageViewModel();
        OpenFileDropDownButton.Flyout!.Opened += (sender, args) => PopulateOpenFileDropDownButton();
    }


    private ReactiveCommand<string, Unit> OpenFileCommand { get; } = ReactiveCommand.CreateFromTask<string>(OpenFile);
    private ReactiveCommand<string, Unit> OpenDirCommand { get; } = ReactiveCommand.Create<string>(OpenDir);

    private void PopulateOpenFileDropDownButton()
    {
        if (OpenFileDropDownButton.Flyout is not MenuFlyout flyout)
        {
            return;
        }
        var sfpConfig = SfpConfig.GetConfig();
        var targetCssFiles = new HashSet<string>();
        var targetJsFiles = new HashSet<string>();
        var skinDir = Steam.GetSkinDir();
        foreach (var patch in sfpConfig.Patches)
        {
            // only add if not empty
            if (!string.IsNullOrWhiteSpace(patch.TargetCss))
            {
                targetCssFiles.Add(patch.TargetCss);
            }

            if (!string.IsNullOrWhiteSpace(patch.TargetJs))
            {
                targetJsFiles.Add(patch.TargetJs);
            }
        }

        flyout.Items.Clear();
        flyout.Items.Add(new MenuItem
        {
            Header = "skins",
            Command = OpenDirCommand,
            CommandParameter = Steam.SkinsDir
        });
        flyout.Items.Add(new MenuItem
        {
            Header = Settings.Default.SelectedSkin,
            Command = OpenDirCommand,
            CommandParameter = Steam.GetSkinDir()
        });
        flyout.Items.Add(new Separator());
        foreach (var cssFile in targetCssFiles)
        {
            flyout.Items.Add(new MenuItem
            {
                Header = Path.GetFileName(cssFile),
                Command = OpenFileCommand,
                CommandParameter = Path.Join(skinDir, cssFile)
            });
        }
        flyout.Items.Add(new Separator());
        foreach (var jsFile in targetJsFiles)
        {
            flyout.Items.Add(new MenuItem
            {
                Header = Path.GetFileName(jsFile),
                Command = OpenFileCommand,
                CommandParameter = Path.Join(skinDir, jsFile)
            });
        }
    }

    private static async Task OpenPath(string path, bool isDirectory)
    {
        try
        {
            if (isDirectory)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            else
            {
                if (!File.Exists(path))
                {
                    await File.Create(path).DisposeAsync();
                }
            }

            Utils.OpenUrl(path);
        }
        catch (Exception e)
        {
            Log.Logger.Warn("Could not open " + path);
            Log.Logger.Debug(e);
        }
    }

    private static async Task OpenFile(string relativeFilePath)
    {
        await OpenPath(relativeFilePath, false);
    }

    private static void OpenDir(string relativeDirPath)
    {
        OpenPath(relativeDirPath, true).Wait();
    }
}
