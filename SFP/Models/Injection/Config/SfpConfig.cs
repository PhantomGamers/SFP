#region

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

#endregion

namespace SFP.Models.Injection.Config;

public class PatchEntry : IEquatable<PatchEntry>
{
    public string MatchRegexString { get; init; } = string.Empty;
    public string TargetCss { get; init; } = string.Empty;
    public string TargetJs { get; init; } = string.Empty;

    [field: JsonIgnore][JsonIgnore] public Regex MatchRegex => field ??= new Regex(MatchRegexString, RegexOptions.Compiled);

    public bool Equals(PatchEntry? entry)
    {
        return entry != null && MatchRegexString == entry.MatchRegexString && TargetCss == entry.TargetCss &&
               TargetJs == entry.TargetJs;
    }

    public override bool Equals(object? obj)
    {
        return this.GetType() == obj?.GetType() && Equals(obj as PatchEntry);
    }

    public override int GetHashCode()
    {
        return MatchRegexString.GetHashCode() ^ TargetCss.GetHashCode() ^ TargetJs.GetHashCode();
    }
}

public class SfpConfig
{
    [JsonIgnore]
    private static readonly IReadOnlyCollection<PatchEntry> DefaultPatches =
    [
        new()
        {
            MatchRegexString = "https://.*.steampowered.com", TargetCss = "webkit.css", TargetJs = "webkit.js"
        },
        new() { MatchRegexString = "https://steamcommunity.com", TargetCss = "webkit.css", TargetJs = "webkit.js" },
        new()
        {
            MatchRegexString = "^Steam$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js"
        },
        new()
        {
            MatchRegexString = "^OverlayBrowser_Browser$",
            TargetCss = "libraryroot.custom.css",
            TargetJs = "libraryroot.custom.js"
        },
        new()
        {
            MatchRegexString = "^SP Overlay:", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js"
        },
        new()
        {
            MatchRegexString = "Supernav$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js"
        },
        new()
        {
            MatchRegexString = "^notificationtoasts_",
            TargetCss = "notifications.custom.css",
            TargetJs = "notifications.custom.js"
        },
        new()
        {
            MatchRegexString = "^SteamBrowser_Find$",
            TargetCss = "libraryroot.custom.css",
            TargetJs = "libraryroot.custom.js"
        },
        new()
        {
            MatchRegexString = @"^OverlayTab\d+_Find$",
            TargetCss = "libraryroot.custom.css",
            TargetJs = "libraryroot.custom.js"
        },
        new()
        {
            MatchRegexString = "^Steam Big Picture Mode$",
            TargetCss = "bigpicture.custom.css",
            TargetJs = "bigpicture.custom.js"
        },
        new()
        {
            MatchRegexString = "^QuickAccess_", TargetCss = "bigpicture.custom.css", TargetJs = "bigpicture.custom.js"
        },
        new()
        {
            MatchRegexString = "^MainMenu_", TargetCss = "bigpicture.custom.css", TargetJs = "bigpicture.custom.js"
        },
        // Friends List and Chat
        new()
        {
            MatchRegexString = ".friendsui-container", TargetCss = "friends.custom.css", TargetJs = "friends.custom.js"
        },
        new() { MatchRegexString = "Menu$", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js" },
        new()
        {
            // Steam Dialog popups (Settings, Game Properties, etc)
            MatchRegexString = ".ModalDialogPopup", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js"
        },
        new()
        {
            // Sign In Page
            MatchRegexString = ".FullModalOverlay", TargetCss = "libraryroot.custom.css", TargetJs = "libraryroot.custom.js"
        }
    ];

    [JsonIgnore] private static SfpConfig? s_sfpConfig;

    [JsonIgnore] public bool IsFromMillennium;
    public bool UseDefaultPatches { get; init; }
    public IEnumerable<PatchEntry> Patches { get; init; } = GetDefaultPatches();
    [JsonIgnore] public static SfpConfig DefaultConfig { get; } = new();

    private static IEnumerable<PatchEntry> GetDefaultPatches()
    {
        return DefaultPatches;
    }

    public static SfpConfig GetConfig(bool overWrite = false)
    {
        if (s_sfpConfig != null && !overWrite)
        {
            return s_sfpConfig;
        }

        var skinDir = Steam.SkinDir;
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
                    var patches = json?.Patches.Concat(DefaultConfig.Patches).Distinct() ?? DefaultConfig.Patches;
                    s_sfpConfig = new SfpConfig { Patches = patches };
                    Log.Logger.Info("Using default SFP skin config as base");
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
                    s_sfpConfig.IsFromMillennium = true;
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
            Patches = [.. millenniumConfig.Patch
                .Where(p => !string.IsNullOrWhiteSpace(p.Url) &&
                            (!string.IsNullOrWhiteSpace(p.Css) || !string.IsNullOrWhiteSpace(p.Js)))
                .Select(p => new PatchEntry { MatchRegexString = p.Url, TargetCss = p.Css, TargetJs = p.Js })]
        };
    }
}