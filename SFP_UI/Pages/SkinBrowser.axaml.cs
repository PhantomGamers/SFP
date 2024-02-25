#region

using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using AsyncImageLoader.Loaders;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Newtonsoft.Json;
using SFP.Models;

#endregion

namespace SFP_UI.Pages;

public partial class SkinBrowserPage : UserControl
{
    [Obsolete("Obsolete")]
    public SkinBrowserPage()
    {
        InitializeComponent();
        InitializeAsync();
    }

    [Obsolete("Obsolete")]
    private async void InitializeAsync()
    {
        var skins = await GetSkins();
        foreach (var card in skins!.Select(skin => new Card(skin)))
        {
            CardsContainer.Items.Add(card);
        }
    }

    public class SkinInfo
    {
        public string? Download { get; init; }
        public string? Author { get; init; }
        public string? GithubPage { get; init; }
        public string? LatestCommitDate { get; init; }
        public string? HeaderImage { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
    }

    private static async Task<List<SkinInfo>?> GetSkins()
    {
        //const string ApiUrl = "insert api url here";
        var client = new HttpClient();
        try
        {
            //var data = await client.GetStringAsync(ApiUrl);
            var data = await File.ReadAllTextAsync("skins.json");
            var json = JsonConvert.DeserializeObject<List<dynamic>>(data);

            return json!.Select(skin => new SkinInfo
            {
                Download = skin["download"],
                Author = skin["data"]["github"]["owner"],
                GithubPage = $"https://github.com/{skin["data"]["github"]["owner"]}/{skin["data"]["github"]["repo"]}/",
                LatestCommitDate = skin["commit_data"]["committedDate"],
                HeaderImage = skin["header_image"],
                Name = skin["name"],
                Description = skin["description"]
            })
                .ToList();
        }
        catch (HttpRequestException e)
        {
            Log.Logger.Error($"Error: {e.Message}");
        }
        finally
        {
            client.Dispose();
        }

        return null;
    }
}

public class Card : UserControl
{
    private readonly string? _githubUrl;
    private readonly SkinBrowserPage.SkinInfo _skinInfo;
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

    private static bool IsInstalled(SkinBrowserPage.SkinInfo skinInfo) =>
        Path.Exists(Path.Combine(Steam.SkinsDir, skinInfo.Name!, "skin_info.json"));

    private static bool IsUpToDate(SkinBrowserPage.SkinInfo skinInfo) =>
        JsonConvert.DeserializeObject<SkinBrowserPage.SkinInfo>(
            File.ReadAllText(Path.Combine(Steam.SkinsDir, skinInfo.Name!, "skin_info.json")))?.LatestCommitDate ==
        skinInfo.LatestCommitDate;

    [Obsolete("Obsolete")]
    public Card(SkinBrowserPage.SkinInfo skinInfo)
    {
        _githubUrl = skinInfo.GithubPage;
        _skinInfo = skinInfo;
        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
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
            Text = skinInfo.Author,
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

    [Obsolete("Obsolete")]
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
            using (var client = new WebClient())
            {
                await client.DownloadFileTaskAsync(skinInfo.Download!, targetPath + ".zip");
            }

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
            var skinInfoData = JsonConvert.SerializeObject(skinInfo, Formatting.Indented);
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

    [Obsolete("Obsolete")]
    private async void DownloadButton_Click(object sender, RoutedEventArgs e) => await DownloadSkin(_skinInfo);
}
