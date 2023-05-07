namespace SFP.Models.Injection.Config;

public class PatchEntry
{
    public string? MatchRegex { get; init; }
    public string? TargetFile { get; init; }
}

public static class PatchConfig
{
    public static IEnumerable<PatchEntry> DefaultPatches { get; } = new[]
    {
        new PatchEntry { MatchRegex = "https://store.steampowered.com", TargetFile = "webkit"},
        new PatchEntry { MatchRegex = "https://steamcommunity.com", TargetFile = "webkit"},
        new PatchEntry { MatchRegex = "^Steam$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegex = "^Steam Settings$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegex = "^GameOverview$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegex = "^Shutdown$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegex = "^What's New Settings$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegex = "^GameNotes$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegex = "^Settings$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegex = "^SoundtrackPlayer$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegex = "^ScreenshotManager$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegex = "^Achievements$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegex = "Menu$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegex = @"Supernav$", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegex = @"^notificationtoasts_", TargetFile = "libraryroot.custom"},
        new PatchEntry { MatchRegex = "^Steam Big Picture Mode$", TargetFile = "bigpicture.custom"},
        new PatchEntry { MatchRegex = "^QuickAccess_", TargetFile = "bigpicture.custom"},
        new PatchEntry { MatchRegex = "^MainMenu_", TargetFile = "bigpicture.custom"},
        new PatchEntry { MatchRegex = "Friends", TargetFile = "friends.custom"}
    };
}
