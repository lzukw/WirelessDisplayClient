#/usr/bin/bash

# The six command-line-arguments are provided by the C#-program starting this script
STREAMING_TYPE=$1
IP_ADDR=$2
PORT_NO=$3
WxH_SENDER=$4
WxH_RECEIVER=$5

echo "startStreamingSource.sh called with arguments STREAMING_TYPE=${STREAMING_TYPE=}, IP_ADDR=${IP_ADDR}, PORT_NO=${PORT_NO}, WxH_SENDER=${WxH_SENDER}, WxH_RECEIVER=${WxH_RECEIVER}"

if [ ${STREAMING_TYPE} == "VNC" ]
then

  # WxH_SENDER and WxH_RECEIVER are not used for VNC-streaming under Linux
  echo "Executing: 'x11vnc -viewonly -scale ${WxH_RECEIVER} -nopw -noxdamage -cursor arrow -scale_cursor 1 -connect ${IP_ADDR}:${PORT_NO}'"
  x11vnc -viewonly -scale ${WxH_RECEIVER} -nopw -noxdamage -cursor arrow -scale_cursor 1 -connect ${IP_ADDR}:${PORT_NO}


elif [ ${STREAMING_TYPE} == "FFmpeg" ]
then

  # WxH_SENDER is overwritten, because on Linux this value is better found by xrandr.
  # WxH_RECEIVER is not used by this script.
  
  # Find out current screen-resolution. After this command WxH_SENDER is something
  # like "1980x1200". 
  WxH_SENDER=$(xrandr | grep '*' | grep -oP '\K(\d*x\d*)')

  # Now start ffmpeg
  echo "Executing: 'fmpeg -f x11grab -s ${WxH_SENDER} -r 30 -i :0.0 -vf scale=${WxH_RECEIVER} -vcodec libx264 -pix_fmt yuv420p -profile:v baseline -tune zerolatency -preset ultrafast -f mpegts "udp://${IP_ADDR}:${PORT_NO}"'"

  ffmpeg -f x11grab -s ${WxH_SENDER} -r 30 -i :0.0 -vf scale=${WxH_RECEIVER} -vcodec libx264 -pix_fmt yuv420p -profile:v baseline -tune zerolatency -preset ultrafast -f mpegts "udp://${IP_ADDR}:${PORT_NO}"


else
  echo "Script-ERROR: Unknown Streaming Type ${STREAMING_TYPE}"
fi


