# SFP (Formerly [SteamFriendsPatcher](https://github.com/PhantomGamers/SteamFriendsPatcher))

This utility is designed to allow you to apply themes to the new Steam beta client.

## Steam Stable Users Read

The newest version of this readme and program are intended for use on the Steam Beta as of 2023-04-23

For the old version of SFP that works with Steam Stable see https://github.com/PhantomGamers/SFP/tree/0.0.14

## Instructions

Download & extract [the latest zip file under Releases](https://github.com/PhantomGamers/SFP/releases/latest) for your
given OS and run the SFP_UI application.

If you have .net 7 installed you can download the _net7.zip release, otherwise download the _SelfContained.zip release.

Run SFP_UI. By default it will automatically wait for Steam to start and inject, or inject if Steam is already running.

For full functionality **SFP must be running with its injector started as long as Steam is running**.

**Steam must be running with the `-cef-enable-debugging` argument for SFP to work.**

If Steam is started with SFP it will do this automatically. Otherwise, **on Windows only** for now, **you can use the "
Force Steam arguments" setting to automatically restart Steam with the chosen arguments** if it does not already have
them.

Use the "Open File" button in SFP to access the files where your custom skins are applied from.

## Features

### Steam Skinning

- Reads and applies custom css to Steam files.
- Skins for the Steam client, library, and overlay go in `Steam/steamui/libraryroot.custom.css`
- Skins for the friends list and chat windows go in `Steam/steamui/friends.custom.css`
- Skins for the Steam store and community pages go in `Steam/steamui/webkit.css`
- Skins for Big Picture Mode go in `Steam/steamui/bigpicture.custom.css`

## Todo

- Add support for javascript injection
- Add Force Steam Arguments support for Linux and Mac

## Known Issues

* Sometimes when Steam is first started and starts with the store page opened, the store page will not be skinned until
  it navigates to a new page.

## Dependencies

* [.NET 7.0](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) (Only if not using self contained version)

## Credits

* Darth from the Steam community forums for the method.
* [@henrikx for Steam directory detection code.](https://github.com/henrikx/metroskininstaller)
* [Sam Allen of Dot Net Perls for GZIP compression, decompression, and detection code.](https://www.dotnetperls.com/decompress)
* [@maxhauser for semver](https://github.com/maxhauser/semver)
* [The Avalonia team for the AvaloniaUI framework](https://github.com/AvaloniaUI/Avalonia)
* [@amwx for the FluentAvalonia theme](https://github.com/amwx/FluentAvalonia)
* [@d2phap for FileWatcherEx](https://github.com/d2phap/FileWatcherEx)
* [@alastairtree for LazyCache](https://github.com/alastairtree/LazyCache)
