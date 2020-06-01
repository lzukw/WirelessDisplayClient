# WirelessDisplayClient

## Overview

This WirelessDispalyClient works togehter with the 
[WirelessDisplayServer](https://github.com/lzukw/WirelessDisplayServer)-program.

The WirelessDisplayServer-program runs on a 'projecting-computer', which is 
connected to a projector. The WirelessDispalyClient (this program) runs
on a 'presentation-computer'. The content of the desktop of the
'presentation-computer' is streamed either using VNC in reverse-connection
or ffmpeg to the 'projecting-computer' and therefore shown by the projector.

The WirelessDisplayServer implements a REST-API which is used by the
WirelessDispalyClient. By using the REST-API and starting local programs, 
the WirelessDispalyClient is able to

- change the screen-resolutions of both computers (and the projector),
- start the streaming-sink (VNC-viewer in listen-mode/reverse-connection 
  or ffplay) on the remote 'projecting-computer', and
- start the streaming-source on local 'presentation-computer'. 

## Running and Configuration

The C#-source-code of this program (WirelessDispalyClient) is in the folder
`WirelessDispalyClientGUI`. The project was created and edited using
Visual-Studio-Code and the platform-independent GUI-Fromwork 
[Avalonia](http://avaloniaui.net/). From within the directory 
`WirelessDispalyClientGUI` the program can be started using `dotnet run`. 
But first the necessary third-party executables should be installed. 

TODO: Maybe Avalonia has to be installed too. How?

WirelessDispalyClientGUI doesn't start third-party-executables directly, but
uses starting-scripts, which are in the folder `Scripts/<operating-system>`. 
See the [README.md] in the directory `Scripts` there for details.

For Linux ffmpeg, x11vnc and xrandr must be available. Since these tools are
normally installed using the package-manager of your Linux-distro, the 
`ThirdPary`-folder normally remains empty. But, see the [README.md] in the 
folder `ThirdParty` for more details.

For Windows ffmpeg (ffmpeg.exe), thightVNC-1.3.10 (WinVNC.exe) and ScreenRes
(screenres.exe) are used. The Script-files in `Scripts/Windows` look for
executables in the directory `ThirdParty`. See the [README.md] in `Scripts` and
the [README.md] in `ThirdParty` for details.

For macOS ...TODO

## Installing

An executable version can be created with the following commands (from 
within the folder WirelessDispalyClientGUI, where Program.cs is):

```
mkdir ..\WirelessDisplayClientGUI_executable 
dotnet publish -c Release -o ..\WirelessDisplayClientGUI_executable -r linux-x64 --self-contained
```

On Windows, replace `linux-x64` with `win-x64` and on macOS with `osx-x64`.

The above command builds a "stand-alone"-Version of the program, which could 
be copied to another computer. If dotnet Core Version 3 is installed on the
presentation-computer, then omit the parameters `-r linux-x64` and 
`--self-contained`.

After publishing, the complete program consists of the three folders:

- [Scripts]
- [ThirdParty]
- [WirelessDisplayClientGUI_executable]

Inside [WirelessDisplayClientGUI_executable] is the executable 
[WirelessDisplayClientGUI.exe].