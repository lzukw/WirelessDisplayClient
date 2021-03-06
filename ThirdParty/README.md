# Third-Party-programs

Three external tools are needed by WirelessDisplayClientGUI, and like
WirelessDisplayClientGUI itself, they all run on the presentation-computer:

- A VNC-server,
- ffmpeg,
- A tool to mangage screen-resolutions of the local computer.

These three tools are not started directly by WirelessDisplayClientGUI, but
they are started using the scripts in the directory 
[Scripts/<Operating-System>]. Have a look at the [README.md] in the directory
[Scripts] for furhter information.

Note: If you plan to use only VNC or only ffmpeg, it is not necessary to
install the other tool, since WirelessDisplayClientGUI does start a script
if you don't start it.

## For Linux

For Linux the [ThirdParty]-folder can remain empty. The scripts in 
[Scripts/Linux] use x11vnc, ffmpeg and xrandr. These programs are best
installed with the packagemanager from your Linux-distro.

## For macos

TODO

## For windows

The programs TightVNC, version 1.3.10 
[see here](https://www.tightvnc.com/download/1.3.10/tightvnc-1.3.10_x86.zip), 
and ffmpeg 
[see here](https://ffmpeg.zeranoe.com/builds/win64/static/ffmpeg-4.2.2-win64-static.zip) 
are used as streaming-source by the scripts in [Scripts\Windows]. The ffmpeg 
zip-File is about 60MB large. As long as github doesn't complain, I provide a 
copy of both zip-files in my repository
[Third-party-tools](https://github.com/lzukw/Third-party-tools).

For managing the local screen-resolution
[ScreenRes](https://github.com/lzukw/ScreenRes) is used. This repository
contains the executable screenres.exe, which depends on ??? Microsoft C++ 
Runtime 2015 TODO find out real name ???. On most Windows-systems this is 
installed. If not you can also download the few .dll-files manually and
place them in the same folder as sreenres.exe.

Download the zip-Files (ffmpeg, tightvnc) to the [ThirdParty]-folder and 
extract them.

Clone the screenres-repository or download it as zip and extract it - also
to the [ThirdParty]-folder. 

Double-check the paths to the executables. The original sripts (batch-files)
in [Scripts\Windows] look for the three executeables here:

- [..\..\ThirdParty\ffmpeg-4.2.2-win64-static\bin\ffmpeg.exe]
- [..\..\ThirdParty\tightvnc-1.3.10_x86\WinVNC.exe]
- [..\..\ThirdParty\ScreenRes\screenres.exe]

If the paths are not correct you can either modify the folder-structure in 
[ThirdParty] or modify the batch-files in [Scripts\Windows].

