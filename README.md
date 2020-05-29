# WirelessDisplayClient

## Overview

This WirelessDispalyClient works togehter with the WirelessDisplayServer-program.

The WirelessDisplayServer-program runs on a 'projecting-computer', which is 
connected to a projector. The WirelessDispalyClient (this program) runs
on a 'presentation-computer'. The content of the desktop of the
'presentation-computer' is streamed either using VNC in reverse-connection
or ffmpeg to the 'projecting-computer' and therefore shown by the projector.

The WirelessDisplayServer implements a REST-API which is used by the
WirelessDispalyClient. By using the REST-API and starting local programs, 
the WirelessDispalyClient is able to

- change the screen-resolution of the 'projecting-computer' (and the
  projector),
- start the streaming-sink (VNC-viewer in listen-mode/reverse-connection 
  or ffplay) on the 'projecting-computer', and
- start the streaming-source on local computer ('presentation-computer'). 

## Running and Configuration

The C#-source-code of this program (WirelessDispalyClient) is in the folder   
`WirelessDispalyClientGUI`. From within this directory the program can be started using `dotnet run`. But first the necessary third-party executables must be installed. 

The starting-scripts used by WirelessDispalyClientGUI are in the folder
`External_Programs/<operating-system>`.

For Linux ffmpeg, x11vnc and xrandr must be available.

For Windows ffmpeg (ffmpeg.exe) and thightVNC-1.3.10 (WinVNC.exe) are
used. Portable versions can also be used, but then starting-batch-files
in  `External_Programs/Windows`must be configured to use these portable versions (Just replace ffmpeg.exe and WinVNC.exe with the absolute or
relative paths to the portable .exe-files).

For macOS ...TODO

## Installing

An executable version can be created with the following commands (from 
within the folder WirelessDispalyClientGUI, where Program.cs is):

```
mkdir ..\WirelessDisplayClient_executable 
dotnet publish -c Release -o ..\WirelessDisplayClient_executable -r linux-x64 --self-contained
```

On Windows, replace `linux-x64` with `win-x64` and on macOS with `osx-x64`.

The above command builds a "stand-alone"-Version of the program, which could 
be copied to another computer. If dotnet Core Version 3 is installed on the
presentation-computer, then omit both parameters `-r linux-x64 --self-contained`.


