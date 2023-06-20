using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using SFP.Models;
using SFP.Models.Injection.Config;
using SFP.Properties;

namespace SFP_UI.Models;

public class OpenFileModel
{
    public static void PopulateOpenFileDropDownButton(object itemsCollection)
    {

        var sfpConfig = SfpConfig.GetConfig();
        var targetCssFiles = new HashSet<string>();
        var targetJsFiles = new HashSet<string>();
        var skinDir = Steam.SkinDir;

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

        switch (itemsCollection)
        {
            case IList<NativeMenuItemBase> nativeMenuItems:
                {
                    nativeMenuItems.Clear();
                    nativeMenuItems.Add(new NativeMenuItem { Header = "steamui/skins/", Command = OpenDir, CommandParameter = Steam.SkinsDir });
                    nativeMenuItems.Add(new NativeMenuItem { Header = "Active Skin: " + Settings.Default.SelectedSkin + '/', Command = OpenDir, CommandParameter = Steam.SkinDir });
                    nativeMenuItems.Add(new NativeMenuItemSeparator());

                    foreach (var cssFile in targetCssFiles)
                    {
                        nativeMenuItems.Add(new NativeMenuItem { Header = Path.GetFileName(cssFile), Command = OpenFile, CommandParameter = Path.Join(skinDir, cssFile) });
                    }

                    nativeMenuItems.Add(new NativeMenuItemSeparator());

                    foreach (var jsFile in targetJsFiles)
                    {
                        nativeMenuItems.Add(new NativeMenuItem { Header = Path.GetFileName(jsFile), Command = OpenFile, CommandParameter = Path.Join(skinDir, jsFile) });
                    }
                    break;
                }
            case ItemCollection avaloniaItems:
                {
                    avaloniaItems.Clear();
                    avaloniaItems.Add(new MenuItem { Header = "steamui/skins/", Command = OpenDir, CommandParameter = Steam.SkinsDir });
                    avaloniaItems.Add(new MenuItem { Header = "Active Skin: " + Settings.Default.SelectedSkin + '/', Command = OpenDir, CommandParameter = Steam.SkinDir });
                    avaloniaItems.Add(new Separator());

                    foreach (var cssFile in targetCssFiles)
                    {
                        avaloniaItems.Add(new MenuItem { Header = Path.GetFileName(cssFile), Command = OpenFile, CommandParameter = Path.Join(skinDir, cssFile) });
                    }

                    avaloniaItems.Add(new Separator());

                    foreach (var jsFile in targetJsFiles)
                    {
                        avaloniaItems.Add(new MenuItem { Header = Path.GetFileName(jsFile), Command = OpenFile, CommandParameter = Path.Join(skinDir, jsFile) });
                    }
                    break;
                }
        }
    }

    private static ReactiveCommand<string, Unit> OpenFile { get; } = ReactiveCommand.CreateFromTask<string>(OpenFileImpl);
    private static ReactiveCommand<string, Unit> OpenDir { get; } = ReactiveCommand.Create<string>(OpenDirImpl);


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

    private static async Task OpenFileImpl(string relativeFilePath)
    {
        await OpenPath(relativeFilePath, false);
    }

    private static void OpenDirImpl(string relativeDirPath)
    {
        OpenPath(relativeDirPath, true).Wait();
    }

}
