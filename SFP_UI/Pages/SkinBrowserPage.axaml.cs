#region

using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using AsyncImageLoader.Loaders;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Flurl.Http;
using SFP.Models;
using SFP_UI.Models;

#endregion

namespace SFP_UI.Pages;

public partial class SkinBrowserPage : UserControl
{
    public SkinBrowserPage()
    {
        InitializeComponent();
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            var skins = await GetSkins();
            foreach (var card in skins!.Select(skin => new Card(skin)))
            {
                CardsContainer.Items.Add(card);
            }
        }
        catch (Exception e)
        {
            Log.Logger.Error("Failed to load available skins");
            Log.Logger.Debug(e);
        }
    }

    private static async Task<List<SkinInfo>?> GetSkins()
    {
        //const string ApiUrl = "insert api url here";
        try
        {
            //var data = await ApiUrl.GetStringAsync();
            var data = await File.ReadAllTextAsync("skins.json");
            var skinInfos = JsonSerializer.Deserialize(data, SFPUIJsonContext.Default.SkinInfoArray);


            return skinInfos?.ToList() ?? [];
        }
        catch (HttpRequestException e)
        {
            Log.Logger.Error($"Error: {e.Message}");
        }

        return [];
    }

    public class SkinInfo
    {
        [JsonPropertyName("header_image")] public string? HeaderImage { get; init; }

        public string? Download { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }

        [JsonPropertyName("commit_data")] public CommitInfo? CommitData { get; init; }

        public DataInfo? Data { get; init; }

        [JsonIgnore] public string GithubPage => $"https://github.com/{Data?.Github?.Owner}/{Data?.Github?.Repo}/";
    }

    public class GithubInfo
    {
        public string? Owner { get; init; }
        public string? Repo { get; init; }
    }

    public class DataInfo
    {
        public GithubInfo? Github { get; init; }
    }

    public class CommitInfo
    {
        public string? CommittedDate { get; init; }
    }
}

public class Card : UserControl
{
    private readonly string? _githubUrl;
    private readonly SkinBrowserPage.SkinInfo _skinInfo;

    public Card(SkinBrowserPage.SkinInfo skinInfo)
    {
        _githubUrl = skinInfo.GithubPage;
        _skinInfo = skinInfo;
        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Image = new Image
        {
            Stretch = Stretch.UniformToFill,
            Height = 200,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top
        };
        stackPanel.Children.Add(Image);
        Title = new TextBlock
        {
            Text = skinInfo.Name,
            FontWeight = FontWeight.Bold,
            FontSize = 16,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        stackPanel.Children.Add(Title);
        Author = new TextBlock
        {
            Text = skinInfo.Data?.Github?.Owner,
            FontStyle = FontStyle.Italic,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        stackPanel.Children.Add(Author);
        Description = new TextBlock
        {
            Text = skinInfo.Description,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        stackPanel.Children.Add(Description);
        GitHubButton = new Button
        {
            Content = "GitHub",
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        GitHubButton.Click += GitHubButton_Click;
        stackPanel.Children.Add(GitHubButton);
        DownloadButton = new Button
        {
            Content = IsInstalled(skinInfo) ? IsUpToDate(skinInfo) ? "Up To Date" : "Update Available" : "Download",
            Background =
                IsInstalled(skinInfo)
                    ? IsUpToDate(skinInfo) ? Brushes.BlueViolet : Brushes.Violet
                    : Brushes.DarkViolet,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        DownloadButton.Click += DownloadButton_Click!;
        stackPanel.Children.Add(DownloadButton);
        RemoveButton = new Button
        {
            Content = "Remove",
            Background = Brushes.MediumVioletRed,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = IsInstalled(skinInfo)
        };
        RemoveButton.Click += (_, _) =>
        {
            Directory.Delete(Path.Combine(Steam.SkinsDir, skinInfo.Name!), true);
            DownloadButton.Content = "Download";
            DownloadButton.Background = Brushes.DarkViolet;
            RemoveButton.IsVisible = false;
            Log.Logger.Info($"Skin: {skinInfo.Name} was removed.");
        };
        stackPanel.Children.Add(RemoveButton);
        Content = stackPanel;
        LoadImageAsync(skinInfo.HeaderImage);
    }

    private Image Image { get; }
    private TextBlock Title { get; }
    private TextBlock Author { get; }
    private TextBlock Description { get; }
    private Button GitHubButton { get; }
    private Button DownloadButton { get; }
    private Button RemoveButton { get; }


    private async void LoadImageAsync(string? imageUrl)
    {
        try
        {
            var loader = new BaseWebImageLoader();
            try
            {
                var bitmap = await loader.ProvideImageAsync(imageUrl!);
                Image.Source = bitmap;
            }
            catch (HttpRequestException e)
            {
                Log.Logger.Error($"Error: {e.Message}");
            }
            catch (FileNotFoundException)
            {
                Image.Source = null;
            }
            finally
            {
                loader.Dispose();
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error($"Error loading image: {ex}");
        }
    }

    private static bool IsInstalled(SkinBrowserPage.SkinInfo skinInfo)
    {
        return Path.Exists(Path.Combine(Steam.SkinsDir, skinInfo.Name!, "skin_info.json"));
    }

    private static bool IsUpToDate(SkinBrowserPage.SkinInfo skinInfo)
    {
        var data = File.ReadAllText(Path.Combine(Steam.SkinsDir, skinInfo.Name!, "skin_info.json"));
        var localSkinInfo = JsonSerializer.Deserialize(data, SFPUIJsonContext.Default.SkinInfo);
        return localSkinInfo?.CommitData?.CommittedDate ==
               skinInfo.CommitData?.CommittedDate;
    }

    private void GitHubButton_Click(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = _githubUrl, UseShellExecute = true });
    }

    private static void Cleanup(SkinBrowserPage.SkinInfo skinInfo)
    {
        var deletionPaths = new List<string>
        {
            Path.Combine(Steam.SkinsDir, skinInfo.Name!),
            Path.Combine(Steam.SkinsDir, $"{skinInfo.Name!}-temp-extraction"),
            Path.Combine(Steam.SkinsDir, skinInfo.Name!) + ".zip"
        };
        foreach (var path in deletionPaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"Failed to delete file or directory {path} during cleanup");
                Log.Logger.Debug(ex);
            }
        }
    }

    private async Task DownloadSkin(SkinBrowserPage.SkinInfo skinInfo)
    {
        if (IsInstalled(skinInfo) || Path.Exists(Path.Combine(Steam.SkinsDir, skinInfo.Name!)))
        {
            if (IsInstalled(skinInfo) && IsUpToDate(skinInfo))
            {
                DownloadButton.Content = "Up To Date";
                return;
            }

            DownloadButton.Content = "Update Available";
        }

        Cleanup(skinInfo);
        var targetPath = Path.Combine(Steam.SkinsDir, Path.GetFileName(skinInfo.Name!));
        string? extractedFolderPath = null;
        try
        {
            await skinInfo.Download!.DownloadFileAsync(targetPath + ".zip");
            extractedFolderPath = Path.Combine(Steam.SkinsDir, $"{skinInfo.Name}-temp-extraction");
            Directory.CreateDirectory(extractedFolderPath);
            ZipFile.ExtractToDirectory(targetPath + ".zip", extractedFolderPath);
            File.Delete(targetPath + ".zip");
            var directories = Directory.GetDirectories(extractedFolderPath);
            if (directories.Length != 1)
            {
                throw new Exception("Unexpected folder structure in the zip file.");
            }
            var extractedFolder = directories[0];
            var newFolderName = Path.Combine(Path.GetDirectoryName(extractedFolder)!, skinInfo.Name!);
            Directory.Move(extractedFolder, newFolderName);
            Directory.Move(newFolderName, Path.Combine(Steam.SkinsDir, skinInfo.Name!));
            Directory.Delete(extractedFolderPath, true);
            var skinInfoFilePath = Path.Combine(Steam.SkinsDir, skinInfo.Name!, "skin_info.json");
            var skinInfoData = JsonSerializer.Serialize(skinInfo, SFPUIJsonContext.Default.SkinInfo);
            await File.WriteAllTextAsync(skinInfoFilePath, skinInfoData);
            Log.Logger.Info($"Skin: {skinInfo.Name} downloaded and extracted successfully!");
            DownloadButton.Content = IsInstalled(skinInfo)
                ? IsUpToDate(skinInfo) ? "Up To Date" : "Update Available"
                : "Download";
            DownloadButton.Background = IsInstalled(skinInfo)
                ? IsUpToDate(skinInfo) ? Brushes.Violet : Brushes.BlueViolet
                : Brushes.DarkViolet;
            RemoveButton.IsVisible = true;
        }
        catch (Exception ex)
        {
            Log.Logger.Error($"Error downloading and extracting skin: {ex}");
            if (extractedFolderPath != null && Directory.Exists(extractedFolderPath))
            {
                Cleanup(skinInfo);
            }
        }
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        await DownloadSkin(_skinInfo);
    }
}
