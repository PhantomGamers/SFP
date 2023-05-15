#region

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

#endregion

namespace SFP.Models.Injection.Config;

public class PatchEntry
{
    [JsonIgnore] private Regex? _matchRegex;

    public string MatchRegexString { get; init; } = string.Empty;
    public string TargetCss { get; init; } = string.Empty;
    public string TargetJs { get; init; } = string.Empty;

    [JsonIgnore] public Regex MatchRegex => _matchRegex ??= new Regex(MatchRegexString, RegexOptions.Compiled);
}

public class SfpConfig
{
    public bool UseDefaultPatches { get; init; }
    public IEnumerable<PatchEntry> Patches { get; init; } = GetDefaultPatches();

    [JsonIgnore] public bool _isFromMillennium;

    [JsonIgnore]
    private static readonly IReadOnlyCollection<PatchEntry> s_defaultPatches = new[]
    {
        new PatchEntry { MatchRegexString = "https://store.steampowered.com", TargetCss = "webkit.css", TargetJs = "webkit.js" },
        new PatchEntry { MatchRegexString = "https://steamcommunity.com", TargetCss = "webkit.css", TargetJs = "webkit.js" },
        new PatchEntry { MatchRegexString = "^Steam$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = "^Steam Settings$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = "^Properties -", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = "^GameOverview$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = "^Shutdown$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = "^OverlayBrowser_Browser$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = "^SP Overlay:", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = "^What's New Settings$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = "^GameNotes$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = "^Settings$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = "^SoundtrackPlayer$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = "^ScreenshotManager$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = "^Screenshot Manager$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = "^Achievements$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = "^Add Non-Steam Game$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = "^Game Servers$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = "^Players$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = "Menu$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = @"Supernav$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = @"^notificationtoasts_", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = @"^SteamBrowser_Find$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = @"^OverlayTab\d+_Find$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new PatchEntry { MatchRegexString = "^Steam Big Picture Mode$", TargetCss = "bigpicture.custom.css", TargetJs = "bigpicture.custom.js" },
        new PatchEntry { MatchRegexString = "^QuickAccess_", TargetCss = "bigpicture.custom.css", TargetJs = "bigpicture.custom.js" },
        new PatchEntry { MatchRegexString = "^MainMenu_", TargetCss = "bigpicture.custom.css", TargetJs = "bigpicture.custom.js" },
        new PatchEntry { MatchRegexString = "Friends", TargetCss = "friends.custom.css", TargetJs = "friends.custom.js" }
    };

    [JsonIgnore]
    private static SfpConfig? s_sfpConfig;
    [JsonIgnore] public static SfpConfig DefaultConfig { get; } = new();
    [ExcludeFromCodeCoverage]
    private static IEnumerable<PatchEntry> GetDefaultPatches() => s_defaultPatches;

    public static SfpConfig GetConfig(bool overWrite = false)
    {
        if (s_sfpConfig != null && !overWrite)
            return s_sfpConfig;

        var skinDir = Steam.GetSkinDir();
        var sfpConfigPath = Path.Combine(skinDir, "skin.json");
        var millenniumConfigPath = Path.Combine(skinDir, "config.json");

        try
        {
            SfpConfig? json;
            if (File.Exists(sfpConfigPath))
            {
                var jsonBytes = File.ReadAllBytes(sfpConfigPath);
                json = JsonSerializer.Deserialize<SfpConfig>(jsonBytes);
                if (json?.UseDefaultPatches ?? true)
                {
                    s_sfpConfig = DefaultConfig;
                    Log.Logger.Info("Using default SFP skin config");
                }
                else
                {
                    s_sfpConfig = json;
                    Log.Logger.Info("Using skin.json from SFP skin");
                }
            }
            else if (File.Exists(millenniumConfigPath))
            {
                var jsonBytes = File.ReadAllBytes(millenniumConfigPath);
                var millenniumConfig = JsonSerializer.Deserialize<MillenniumConfig>(jsonBytes);
                if (millenniumConfig == null)
                {
                    Log.Logger.Warn("Failed to parse config.json, result was null, using default config");
                    s_sfpConfig = DefaultConfig;
                }
                else
                {
                    json = FromMillenniumConfig(millenniumConfig);
                    s_sfpConfig = json;
                    s_sfpConfig._isFromMillennium = true;
                    Log.Logger.Info("Using config.json from Millennium skin");
                }
            }
            else
            {
                s_sfpConfig = DefaultConfig;
                Log.Logger.Info("Using default SFP skin config");
            }
        }
        catch (Exception e)
        {
            Log.Logger.Warn("Failed to parse " + sfpConfigPath + ", using default config");
            Log.Logger.Error(e);
            s_sfpConfig = DefaultConfig;
        }
        return s_sfpConfig;
    }


    public static SfpConfig FromMillenniumConfig(MillenniumConfig millenniumConfig)
    {
        return new SfpConfig
        {
            UseDefaultPatches = false,
            Patches = millenniumConfig.Patch
                .Where(p => !string.IsNullOrWhiteSpace(p.Url) && (!string.IsNullOrWhiteSpace(p.Css) || !string.IsNullOrWhiteSpace(p.Js)))
                .Select(p => new PatchEntry
                {
                    MatchRegexString = p.Url,
                    TargetCss = p.Css,
                    TargetJs = p.Js
                }).ToArray()
        };
    }
}
