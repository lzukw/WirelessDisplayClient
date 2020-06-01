@echo off

REM The command-line-arguments passed to this script
SET STREAMING_TYPE=%1
SET IP_ADDR=%2
set PORT_NO=%3
set WxH_SENDER=%4
set WxH_RECEIVER=%5

IF "%STREAMING_TYPE%" == "VNC" (
    START ..\..\ThirdParty\tightvnc-1.3.10_x86\WinVNC.exe -run
    timeout /t 2
    ..\..\ThirdParty\tightvnc-1.3.10_x86\WinVNC.exe -connect %IP_ADDR%:%PORT_NO% -shareprimary
	
	REM prevent script vom terminating immediately (entless loop)
	FOR /L %%G IN () DO timeout /t 2
)

IF "%STREAMING_TYPE%" == "FFmpeg" (
    ..\..\ThirdParty\ffmpeg-20200528-c0f01ea-win64-static\bin\FFmpeg.exe -f dshow -i video="screen-capture-recorder" -r 30 -i :0.0 -vf scale=%WxH_RECEIVER% -vcodec libx264 -pix_fmt yuv420p -profile:v baseline -tune zerolatency -preset ultrafast -f mpegts "udp://%IP_ADDR%:%PORT_NO%"
)