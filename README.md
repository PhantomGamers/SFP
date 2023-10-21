# SFP (Formerly SteamFriendsPatcher)

This utility allows you to apply skins and scripts to the new Steam client.

- [SFP (Formerly SteamFriendsPatcher)](#sfp--formerly-steamfriendspatcher-)
    * [Instructions](#instructions)
    * [Features](#features)
        + [Steam Skinning](#steam-skinning)
        + [Scripting](#scripting)
        + [Skins and Scripts in Separate Folders](#skins-and-scripts-in-separate-folders)
        + [Enable JavaScript Injection](#enable-javascript-injection)
    * [Skin Authors](#skin-authors)
        + [Matching against pages with variable titles](#matching-against-pages-with-variable-titles)
        + [Finding Steam Page Titles](#finding-steam-page-titles)
    * [Todo](#todo)
    * [Known Issues](#known-issues)
    * [Dependencies](#dependencies)
        + [All](#all)
        + [Linux](#linux)
    * [Credits](#credits)

<small><i><a href='http://ecotrust-canada.github.io/markdown-toc/'>Table of contents generated with markdown-toc</a></i></small>

## Instructions

1. Download and extract the [latest zip file under Releases](https://github.com/PhantomGamers/SFP/releases/) for your
   operating system.
   - If you have .NET 7 installed, download the \_net7.zip release; otherwise, download the \_SelfContained.zip release.
2. Run the SFP_UI application.
3. By default, SFP_UI will automatically wait for Steam to start and inject, or inject if Steam is already running.
4. For full functionality, **SFP must be running with its injector started as long as Steam is running**.
5. Steam must be running with the `-cef-enable-debugging` argument for SFP to work.
    - If Steam is started with SFP, it will do this automatically. Otherwise, you can use the "Force Steam
      arguments" setting to automatically restart Steam with the chosen arguments if it does not already have them.
    - This setting is enabled by default
6. Use the "Open File" button in SFP to access the files where your custom skins and scripts are applied from.

For more information and links to existing skins see [Steam Skins Wiki](https://steamskins.pages.dev/)

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

- Skins and scripts can also be added to their own folders within `Steam/steamui/skins` and then selected in SFP's
  settings.

### Enable JavaScript Injection

- JavaScript injection is disabled by default and must be enabled in SFP's settings.
- JavaScript code can potentially be malicious, so only install scripts from people you trust!

## Skin Authors

If you want to customize which Steam pages are skinned and which files are applied to each page, include a `skin.json`
file in the root of your skin folder.

Default `skin.json`:

```json
{
  "Patches": [
    {
      "MatchRegexString": "https://.*.steampowered.com",
      "TargetCss": "webkit.css",
      "TargetJs": "webkit.js"
    },
    {
      "MatchRegexString": "https://steamcommunity.com",
      "TargetCss": "webkit.css",
      "TargetJs": "webkit.js"
    },
    {
      "MatchRegexString": "^Steam$",
      "TargetCss": "libraryroot.custom.css",
      "TargetJs": "libraryroot.custom.js"
    },
    {
      "MatchRegexString": "^OverlayBrowser_Browser$",
      "TargetCss": "libraryroot.custom.css",
      "TargetJs": "libraryroot.custom.js"
    },
    {
      "MatchRegexString": "^SP Overlay:",
      "TargetCss": "libraryroot.custom.css",
      "TargetJs": "libraryroot.custom.js"
    },
    {
      "MatchRegexString": "Supernav$",
      "TargetCss": "libraryroot.custom.css",
      "TargetJs": "libraryroot.custom.js"
    },
    {
      "MatchRegexString": "^notificationtoasts_",
      "TargetCss": "notifications.custom.css",
      "TargetJs": "notifications.custom.js"
    },
    {
      "MatchRegexString": "^SteamBrowser_Find$",
      "TargetCss": "libraryroot.custom.css",
      "TargetJs": "libraryroot.custom.js"
    },
    {
      "MatchRegexString": "^OverlayTab\\d+_Find$",
      "TargetCss": "libraryroot.custom.css",
      "TargetJs": "libraryroot.custom.js"
    },
    {
      "MatchRegexString": "^Steam Big Picture Mode$",
      "TargetCss": "bigpicture.custom.css",
      "TargetJs": "bigpicture.custom.js"
    },
    {
      "MatchRegexString": "^QuickAccess_",
      "TargetCss": "bigpicture.custom.css",
      "TargetJs": "bigpicture.custom.js"
    },
    {
      "MatchRegexString": "^MainMenu_",
      "TargetCss": "bigpicture.custom.css",
      "TargetJs": "bigpicture.custom.js"
    },
    {
      "MatchRegexString": ".friendsui-container",
      "TargetCss": "friends.custom.css",
      "TargetJs": "friends.custom.js"
    },
    {
      "MatchRegexString": "Menu$",
      "TargetCss": "libraryroot.custom.css",
      "TargetJs": "libraryroot.custom.js"
    },
    {
      "MatchRegexString": ".ModalDialogPopup",
      "TargetCss": "libraryroot.custom.css",
      "TargetJs": "libraryroot.custom.js"
    },
    {
      "MatchRegexString": ".FullModalOverlay",
      "TargetCss": "libraryroot.custom.css",
      "TargetJs": "libraryroot.custom.js"
    }
  ]
}
```

Each entry should have a "MatchRegexString" key, where the value is either a regex string that will be matched against a
Steam page title or a url that begins with `http://` or `https://` that will be matched against a url.

Each entry can also have a TargetCss key and a TargetJs key, which will be the css and js files that are applied to the
page if the regex matches.

An entry can have both a TargetCss and a TargetJs key, or just one of them.

Each target can only have one Css and one Js file injected at a time, with the first match taking precedence, so order
your patches correctly.

If you would like to use SFP's default config you can simply omit the skin.json file or include this:

```json
{
    "UseDefaultPatches": true
}
```

If that key is included along with custom patches, the custom patches will be applied first, followed by the default patches.

### Matching against pages with variable titles

Certain pages will have titles that change either depending on the user's language settings or some other factor.

In order to match against these pages, you can match against a selector that exists within the page. SFP will match
against a selector if MatchRegexString begins with `.`, `#`, or `[`.

For example:

- The Friends List and Chat windows can be matched against with `.friendsui-container`

- The library game properties dialog and most of the dialogs that pop up in the overlay menu can be matched against
  with `.ModalDialogPopup`

- The sign in page can be matched against with `.FullModalOverlay`

### Finding Steam Page Titles

To find steam page titles to match against, make sure Steam is running with `cef-enable-debugging` and then
visit <http://localhost:8080> in your web browser.

### Using System Accent Color

When the user has the UseAppTheme setting enabled, SFP passes the System Accent Color to Steam via CSS variables.

You can use these variables in your skin to match the system accent color.

The variables are as follows:

- `--SystemAccentColor`
- `--SystemAccentColorLight1`
- `--SystemAccentColorLight2`
- `--SystemAccentColorLight3`
- `--SystemAccentColorDark1`
- `--SystemAccentColorDark2`
- `--SystemAccentColorDark3`

When using these variables, make sure to fallback to sane defaults as these variables may not exist if the user does not enable UseAppTheme or uses a different patcher.

## Todo

- Add ability to install and customize skins directly from SFP

## Known Issues

- None currently

## Dependencies

### All

- [.NET 7.0](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) (Only if not using self contained version)

### Linux

- ttf-ms-fonts
  -  run `fc-cache --force` after installing

## Credits

- Darth from the Steam community forums for the method.
- [@henrikx for Steam directory detection code.](https://github.com/henrikx/metroskininstaller)
- [Sam Allen of Dot Net Perls for GZIP compression, decompression, and detection code.](https://www.dotnetperls.com/decompress)
- [@maxhauser for semver](https://github.com/maxhauser/semver)
- [The Avalonia team for the AvaloniaUI framework](https://github.com/AvaloniaUI/Avalonia)
- [@amwx for the FluentAvalonia theme](https://github.com/amwx/FluentAvalonia)
- [@d2phap for FileWatcherEx](https://github.com/d2phap/FileWatcherEx)
- [@alastairtree for LazyCache](https://github.com/alastairtree/LazyCache)
