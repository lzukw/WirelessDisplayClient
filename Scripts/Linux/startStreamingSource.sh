#/usr/bin/bash

# The six command-line-arguments are provided by the C#-program starting this script
STREAMING_TYPE=$1
IP_ADDR=$2
PORT_NO=$3
WxH_STREAM=$4

echo "startStreamingSource.sh called with arguments STREAMING_TYPE=${STREAMING_TYPE=}, IP_ADDR=${IP_ADDR}, PORT_NO=${PORT_NO}, WxH_SENDER=${WxH_SENDER}, WxH_STREAM=${WxH_STREAM}"

if [ ${STREAMING_TYPE} == "VNC" ]
then

  echo "Executing: 'x11vnc -viewonly -scale ${WxH_STREAM} -nopw -noxdamage -cursor arrow -scale_cursor 1 -connect ${IP_ADDR}:${PORT_NO}'"
  x11vnc -viewonly -scale ${WxH_STREAM} -nopw -noxdamage -cursor arrow -scale_cursor 1 -connect ${IP_ADDR}:${PORT_NO}


elif [ ${STREAMING_TYPE} == "FFmpeg" ]
then
 
  # Find out current screen-resolution. After this command WxH_SENDER is something
  # like "1980x1200". 
  WxH_SENDER=$(xrandr | grep '*' | grep -oP '\K(\d*x\d*)')

  # Now start ffmpeg
  echo "Executing: 'fmpeg -f x11grab -s ${WxH_SENDER} -r 30 -i :0.0 -vf scale=${WxH_STREAM} -vcodec libx264 -pix_fmt yuv420p -profile:v baseline -tune zerolatency -preset ultrafast -f mpegts "udp://${IP_ADDR}:${PORT_NO}"'"

  ffmpeg -f x11grab -s ${WxH_SENDER} -r 30 -i :0.0 -vf scale=${WxH_STREAM} -vcodec libx264 -pix_fmt yuv420p -profile:v baseline -tune zerolatency -preset ultrafast -f mpegts "udp://${IP_ADDR}:${PORT_NO}"


else
  echo "Script-ERROR: Unknown Streaming Type ${STREAMING_TYPE}"
  exit 1
fi


