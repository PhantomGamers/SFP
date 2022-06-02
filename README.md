# SFP (Formerly [SteamFriendsPatcher](https://github.com/PhantomGamers/SteamFriendsPatcher))

This utility is designed to allow you to apply themes to the modern Steam friends and library interfaces.

## Instructions

Download & extract [the latest zip file under Releases](https://github.com/PhantomGamers/SFP/releases/latest) for your given OS and run the SFP_UI application.

To use manually, simply click "Patch," by default this will run when you start the program.
You will be required to do this everytime Valve pushes a new version of the friends.css file used by Steam or they update the library.
To avoid this, use the "Start Scanner" option and leave the program running.
With the scanner running, whenever a new version of the friends.css or library css is pushed it will automatically be patched.

Once your files are patched follow the instructions in the program to install the skins of your choosing!

**SFP does not include skins by itself, it simply patches the required files to allow the loading of skins made by others!**

**If you have used the old patcher before make sure to use the Reset Steam option on this one on your first use!**

**If any of the above is unclear I highly recommend reading this fantastic guide available on the Steam community: <https://steamcommunity.com/sharedfiles/filedetails/?id=1941650801>**

## Todo

* Improve automatic detection of correct library CSS file to patch.

## Known Issues

* Currently breaks Big Picture Mode chat due to Big Picture not utilizing steamloopback.host
* Patching the friends list is currently unsupported on Linux and OSX as it uses a different file format from Windows. Contributions welcome!

## Dependencies

* [.NET 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)

## Credits

* Darth from the Steam community forums for the method.
* [@henrikx for Steam directory detection code.](https://github.com/henrikx/metroskininstaller)
* [Sam Allen of Dot Net Perls for GZIP compression, decompression, and detection code.](https://www.dotnetperls.com/decompress)
* [@maxhauser for semver](https://github.com/maxhauser/semver)
* [The Avalonia team for the AvaloniaUI framework](https://github.com/AvaloniaUI/Avalonia)
* [@amwx for the FluentAvalonia theme](https://github.com/amwx/FluentAvalonia)
