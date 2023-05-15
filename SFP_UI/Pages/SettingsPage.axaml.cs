#region

using System.Text;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
        DataContext = new SettingsPageViewModel(AppThemeComboBox);
        PopulateSteamSkinComboBox();
        SteamSkinComboBox.DropDownOpened += (sender, args) => PopulateSteamSkinComboBox();
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
        var selectedSkin = SFP.Properties.Settings.Default.SelectedSkin;
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

    private void SteamSkinComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SFP.Properties.Settings.Default.SelectedSkin = SteamSkinComboBox.SelectedValue?.ToString();
        _ = SfpConfig.GetConfig(true);
        Injector.Reload();
    }
}
