using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SFP.Models.Injection.Config;

public class PatchEntry
{
    public string MatchRegexString { get; init; } = string.Empty;
    public string TargetFile { get; init; } = string.Empty;

    [JsonIgnore]
    private Regex? _matchRegex;
    [JsonIgnore]
    public Regex MatchRegex => _matchRegex ??= new Regex(MatchRegexString, RegexOptions.Compiled);
}

public static class PatchConfig
{
    public static IEnumerable<PatchEntry> DefaultPatches { get; } = new[]
    {
        new PatchEntry { MatchRegexString = "https://store.steampowered.com", TargetFile = "webkit"},
        new PatchEntry { MatchRegexString = "https://steamcommunity.com", TargetFile = "webkit"},
        new PatchEntry { MatchRegexString = "^Steam$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegexString = "^Steam Settings$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegexString = "^GameOverview$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegexString = "^Shutdown$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegexString = "^What's New Settings$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegexString = "^GameNotes$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegexString = "^Settings$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegexString = "^SoundtrackPlayer$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegexString = "^ScreenshotManager$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegexString = "^Screenshot Manager$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegexString = "^Achievements$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegexString = "^Add Non-Steam Game$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegexString = "^Game Servers$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegexString = "^Players$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegexString = "Menu$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegexString = @"Supernav$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegexString = @"^notificationtoasts_", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegexString = "^Steam Big Picture Mode$", TargetFile = "bigpicture.custom"},
        new PatchEntry { MatchRegexString = "^QuickAccess_", TargetFile = "bigpicture.custom"},
        new PatchEntry { MatchRegexString = "^MainMenu_", TargetFile = "bigpicture.custom"},
        new PatchEntry { MatchRegexString = "Friends", TargetFile = "friends.custom"}
    };
}
