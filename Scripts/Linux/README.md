The shell-scripts in this folder perform the actual streaming on the 
presentation-computer (WirelessDisplayClient). For VNC-"streaming"
x11vnc is used and for FFmpeg-streaming, ffmpeg is used.

On Linux ffmpeg and x11vnc can be installed with the package-managers of the 
distribution.

The shell-scripts are called by the C#-Program with three 
command-line-arguments containing

- The screen-resolution to be used for the stream, 
- The IP-Address and the 
- The Port-number of the remote computer (projecting-computer, 
  WirelessDisplayServer).

Feel free to modify the shell-scripts to change the behaviour of the 
streaming-sources (ffmpeg / x11vnc). 
