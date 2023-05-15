# SFP (Formerly SteamFriendsPatcher)

This utility allows you to apply themes and scripts to the new Steam beta client.

## Steam Stable Users Read

The latest version of this readme and program is intended for use on the Steam Beta as of 2023-04-23.

For the old version of SFP that works with Steam Stable, see [here](https://github.com/PhantomGamers/SFP/tree/0.0.14).

## Instructions

1. Download and extract the [latest zip file under Releases](https://github.com/PhantomGamers/SFP/releases/) for your operating system.
2. Run the SFP_UI application.
3. If you have .NET 7 installed, download the \_net7.zip release; otherwise, download the \_SelfContained.zip release.
4. By default, SFP_UI will automatically wait for Steam to start and inject, or inject if Steam is already running.
5. For full functionality, **SFP must be running with its injector started as long as Steam is running**.
6. Steam must be running with the `-cef-enable-debugging` argument for SFP to work.
7. If Steam is started with SFP, it will do this automatically. Otherwise, on Windows only, you can use the "Force Steam arguments" setting to automatically restart Steam with the chosen arguments if it does not already have them.
8. Use the "Open File" button in SFP to access the files where your custom skins and scripts are applied from.

## Features

### Steam Skinning

- Reads and applies custom CSS to Steam pages.
    - Customize which files apply to which pages in the skin config.
    - By default, SFP applies skins from:
        - Skins for the Steam client, library, and overlay go in `Steam/steamui/libraryroot.custom.css`.
        - Skins for the friends list and chat windows go in `Steam/steamui/friends.custom.css`.
        - Skins for the Steam store and community pages go in `Steam/steamui/webkit.css`.
        - Skins for Big Picture Mode go in `Steam/steamui/bigpicture.custom.css`.

### Scripting

- Reads and applies custom JavaScript to Steam pages.
    - Customize which files apply to which pages in the skin config.
    - By default, SFP applies scripts from:
        - Scripts for the Steam client, library, and overlay go in `Steam/steamui/libraryroot.custom.js`.
        - Scripts for the friends list and chat windows go in `Steam/steamui/friends.custom.js`.
        - Scripts for the Steam store and community pages go in `Steam/steamui/webkit.js`.
        - Scripts for Big Picture Mode go in `Steam/steamui/bigpicture.custom.js`.

### Skins and Scripts in Separate Folders

- Skins and scripts can also be added to their own folders within `Steam/steamui/skins` and then selected in SFP's settings.

### Enable JavaScript Injection

- JavaScript injection is disabled by default and must be enabled in SFP's settings.
- JavaScript code can potentially be malicious, so only install scripts from people you trust!

## Skin Authors

If you want to customize which Steam pages are skinned and which files are applied to each page, include a `skin.json` file in the root of your skin folder.

Example `skin.json`:

```json
{
  "Patches": [
    {
      "MatchRegexString": "https://store.steampowered.com",
      "TargetCss": "webkit.css",
      "TargetJs": "webkit.js"
    },
    {
      "MatchRegexString": "^Steam$",
      "TargetCss": "libraryroot.custom.css",
      "TargetJs": "libraryroot.custom.js"
    },
    {
      "MatchRegexString": "Menu$",
      "TargetCss": "libraryroot.custom.css"
    },
    {
      "MatchRegexString": "Supernav$",
      "TargetCss": "libraryroot.custom.css"
    },
    {
      "MatchRegexString": "Friends",
      "TargetCss": "friends.custom.css",
      "TargetJs": "friends.custom.js"
    }
  ]
}
```

Each entry should have a "MatchRegexString" key, where the value is either a regex string that will be matched against a Steam page title or a url that begins with `http://` or `https://` that will be matched against a url.

Each entry can also have a TargetCss key and a TargetJs key, which will be the css and js files that are applied to the page if the regex matches.

An entry can have both a TargetCss and a TargetJs key, or just one of them.

If you would like to use SFP's default config you can simply omit the skin.json file or include this:

```json
{
    "UseDefaultPatches": true
}
```

### Matching against Friends List

To match against the friends list and chat windows, simply put `Friends`in the MatchRegexString value as matching against the page title will not work across Steam language settings.

### Finding Steam Page Titles

To find steam page titles to match against, make sure Steam is running with `cef-enable-debugging` and then visit <http://localhost:8080> in your web browser.


## Todo

- Add Force Steam Arguments support for Linux and Mac
- Add ability to install and customize themes directly from SFP

## Known Issues

- None currently

## Dependencies

- [.NET 7.0](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) (Only if not using self contained version)

## Credits

- Darth from the Steam community forums for the method.
- [@henrikx for Steam directory detection code.](https://github.com/henrikx/metroskininstaller)
- [Sam Allen of Dot Net Perls for GZIP compression, decompression, and detection code.](https://www.dotnetperls.com/decompress)
- [@maxhauser for semver](https://github.com/maxhauser/semver)
- [The Avalonia team for the AvaloniaUI framework](https://github.com/AvaloniaUI/Avalonia)
- [@amwx for the FluentAvalonia theme](https://github.com/amwx/FluentAvalonia)
- [@d2phap for FileWatcherEx](https://github.com/d2phap/FileWatcherEx)
- [@alastairtree for LazyCache](https://github.com/alastairtree/LazyCache)
