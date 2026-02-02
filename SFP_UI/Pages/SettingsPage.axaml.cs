#region

using System.Text;

using Avalonia.Controls;

using SFP.Models;
using SFP.Models.Injection;
using SFP.Models.Injection.Config;
using SFP.Properties;

using SFP_UI.ViewModels;

#endregion

namespace SFP_UI.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        DataContext = new SettingsPageViewModel();
        PopulateSteamSkinComboBox();
        SteamSkinComboBox.DropDownOpened += (_, _) => PopulateSteamSkinComboBox();
        SteamSkinComboBox.SelectionChanged += SteamSkinComboBox_SelectionChanged;
    }

    private void PopulateSteamSkinComboBox()
    {
        var selectedItem = SteamSkinComboBox.SelectedItem;
        SteamSkinComboBox.SelectionChanged -= SteamSkinComboBox_SelectionChanged;
        SteamSkinComboBox.Items.Clear();
        SteamSkinComboBox.Items.Add("steamui");
        var skinDir = Steam.SkinsDir;
        if (!string.IsNullOrWhiteSpace(skinDir))
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var subDirectory in Directory.EnumerateDirectories(skinDir))
                {
                    if (subDirectory.EndsWith("steamui", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Clear();
                        sb.Append("skins");
                        sb.Append(Path.DirectorySeparatorChar);
                        sb.Append(Path.GetFileName(subDirectory));
                        SteamSkinComboBox.Items.Add(sb.ToString());
                    }
                    else
                    {
                        SteamSkinComboBox.Items.Add(Path.GetFileName(subDirectory));
                    }
                }
            }
            catch (Exception e)
            {
                if (Directory.Exists(skinDir))
                {
                    Log.Logger.Warn("Failed to detect Steam skins");
                    Log.Logger.Debug(e);
                }
            }
        }
        var selectedSkin = Settings.Default.SelectedSkin;
        var skinSet = new HashSet<object>(SteamSkinComboBox.Items.Cast<object>());
        if (skinSet.Contains(selectedSkin))
        {
            SteamSkinComboBox.SelectedItem = selectedSkin;
        }
        else if (selectedItem != null && skinSet.Contains(selectedItem))
        {
            SteamSkinComboBox.SelectedItem = selectedItem;
        }
        else
        {
            SteamSkinComboBox.SelectedIndex = 0;
        }
        SteamSkinComboBox.SelectionChanged += SteamSkinComboBox_SelectionChanged;
    }

#pragma warning disable EPC27
    private async void SteamSkinComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
#pragma warning restore EPC27
    {
        try
        {
            var value = SteamSkinComboBox.SelectedValue?.ToString();
            Log.Logger.Info("Switching to skin {Skin}", value);
            Settings.Default.SelectedSkin = value;
            Settings.Default.Save();
            _ = Steam.GetRelativeSkinDir(force: true);
            _ = SfpConfig.GetConfig(true);
            await Injector.Reload();
        }
        catch (Exception ex)
        {
            Log.Logger.Error("Error in SteamSkinComboBox_SelectionChanged event handler");
            Log.Logger.Debug(ex);
        }
    }
}