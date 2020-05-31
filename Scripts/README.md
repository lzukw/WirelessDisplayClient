# Scripts

WirelessDispalyClientGUI doesn't start third-party-executables directly, but
uses starting-scripts, which are in the folder [Scripts/<operating-system>].
This mechanism enures, that the user can modify the scripts and change the
way, the third-party-programs are executed, without changing or 
recompiling the C#-GUI-program (WirelessDispalyClientGUI).

On Linux `bash` is used to execute the scripts, on macOS TODO, and on 
Windows `cmd.exe`. The scripts themselves are bash-scripts on Linux, 
TODO on macOS, and batch-Files on Windows.

In [App.config] in the folder [WirelessDisplayClientGUI] the paths to
the executed scripts are predefined. Also templates for command-line-arguments
passed to these scripts are defined. It should never be necessary to change
the settings in [App.config], these settings are just used by 
WirelessDispalyClientGUI to start the scripts as a child-process. 

Two scripts are necessary for each operating-system:

- A script to manage the screen-resolution of the local computer.
- A script to start the streaming-source (VNC-server in reverse connection
  or FFmpeg) on the local computer.

The script to manage the screen-resolutions is expected to execute and 
finish in short time, since the GUI-program waits for its execution. The
script starting the streaming-source does not return until streaming is
stopped again by the user.

## Command-line-arguments passed to the scripts and exit-code

Each script are passed a predefined set of command-line-arguments. After
completion a script has to return an exit-code. An exit-code of 0 means
success, every other code indicates a failure.

### Command-line-arguments passed to the manage-screen-resolutions-script

The first command-line-argument is either 

- the string "GET", if the script should write the current screen-resolution
  to its standard-output,
- the string "SET", if the script should change the local screen-resolution, or
- the string "ALL", if the script should write all available local
  screen-resolutions to its standard-output (one line for each
  screen-resolution).

The second command-line-argument is a screen-resolution, and is only used,
it the first argument was "SET". This string must have the same form as the
strings returned by "GET" and "ALL". Normally they have a form like
"1024x768".

### Command-line-arguments passed to the start-streaming-source-sript

The first command-line-argument is either the string "VNC" or the string
"FFmpeg", depending on which streaming-source shall be started by the script.
Note all strings denoted here are passed to the script without the 
double-quotes here do denote strings.

The second argument is the IP-Address of the remote 'projecting-computer'
running the WirelessDisplayServer. Something like "192.168.1.34".

The third argument is the port-number used for streaming. The streaming-sink
on the remote 'projecting-computer' will listen on this port. So the only
requirement is, that this port is not used already by another program. Also
port-numbers for custom services must be in the range 1024...65535.

The fourth argument is a string containing the screen-resolution of the
local 'prenentation-computer'. Something like "1980x1040".

The fifth argument id a string containing the screen-resolution of the
remote 'projecting-computer'. Something like "1024x768".

