#/usr/bin/bash

# The five command-line-arguments are provided by the C#-program starting this script
IP_ADDR=$1      # IP-Address of the remote computer (projecting computer)
PORT_NO=$2      # Port-Number used for VNC or ffmpeg
WxH_SENDER=$3   # The screen-resolution of the presentation computer (WirelessDisplayClient)
WxH_STREAM=$4   # The screen-resolution used for the stream
WxH_RECEIVER=$5 # The screen-resolution of the projecting computer (WirelessDisplayServer)

# WxH_SENDER and WxH_RECEIVER are not used by this script.

echo "Executing: 'x11vnc -viewonly -scale ${WxH_STREAM} -nopw -noxdamage -cursor arrow -scale_cursor 1 -connect ${IP_ADDR}:${PORT_NO}'"
x11vnc -viewonly -scale ${WxH_STREAM} -nopw -noxdamage -cursor arrow -scale_cursor 1 -connect ${IP_ADDR}:${PORT_NO}

