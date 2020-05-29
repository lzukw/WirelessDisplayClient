#/usr/bin/bash

# The five command-line-arguments are provided by the C#-program starting this script
IP_ADDR=$1
PORT_NO=$2
WxH_SENDER=$3
WxH_STREAM=$4
WxH_RECEIVER=$5

# WxH_SENDER is overwritten, because on Linux this value is better found by xrandr.
# WxH_RECEIVER is not used by this script.

# Find out current screen-resolution. After this command WxH_SENDER is something
# like "1980x1200". 
WxH_SENDER=$(xrandr | grep '*' | grep -oP '\K(\d*x\d*)')

# Now start ffmpeg
echo "Executing: 'fmpeg -f x11grab -s ${WxH_SENDER} -r 30 -i :0.0 -vf scale=${WxH_STREAM} -vcodec libx264 -pix_fmt yuv420p -profile:v baseline -tune zerolatency -preset ultrafast -f mpegts "udp://${IP_ADDR}:${PORT_NO}"'"

ffmpeg -f x11grab -s ${WxH_SENDER} -r 30 -i :0.0 -vf scale=${WxH_STREAM} -vcodec libx264 -pix_fmt yuv420p -profile:v baseline -tune zerolatency -preset ultrafast -f mpegts "udp://${IP_ADDR}:${PORT_NO}"
