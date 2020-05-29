using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WirelessDisplayClient.Services
{
    public interface IWDCServciceProvider
    {
        //
        // Summary:
        //     Returns true, if a GET-Request to `ipAddress` is successfull.
        //     The GET-Request is made to api/StreamPlayer/VncViewerStarted
        // Parameters:
        //   ipAddress:
        //     The IPv4-Address of the WirelessDisplayServer (projecting-computer).
        //     as a string.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     ipAddress is not valid, or request was not successfull.
        Task Connect(string ipAddress);

        //
        // Summary:
        //     After a successfull call of Connect() LastKnownRemoteIP contains
        //     The IPv4-Address of the remote-computer (projecting-computer
        //     runnig the program 'WirelessDisplayServer').  
        string LastKnownRemoteIP { get; protected set; }
        
        //
        // Summary:
        //     Returns the initial screen-resolulion of the remote computer.
        // Returns:
        //     A string with the initial screen-resolution, for example "1280x1024".
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        Task<string> GetInitialScreenResolution();

        //
        // Summary:
        //     Returns the current screen-resolulion of the remote computer.
        // Returns:
        //     A string with the current screen-resolution, for example "1024x768".
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
         Task<string> GetCurrentScreenResolution();

        //
        // Summary:
        //     Returns all available screen-resolutions of the remote computer.
        // Returns:
        //     A list of strings with the available screen-resolutions, 
        //     for example { "640x460", "1024x768", ...}.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        Task<List<string>> GetAvailableRemoteScreenResolutions();

        //
        // Summary:
        //     Changes the screen-resolution of the remote computer.
        // Parameters:
        //   resolution:
        //     A string containing the screen-resolution to set, for example
        //     "1024x768" (The string is without quotes!).
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        Task SetRemoteScreenResolution(string resolution);

        //
        // Summary:
        //     Start VNC-Server on the local computer and VNC-viewer on the
        //     remote computer. The VNC-connection is in "reverse-connection"
        //     (VNC-viewer in listen-mode).
        // Paremeters:
        //   portNo:
        //     The port-Number used for the remote VNC-viewer to listen on.
        //   senderResolution:
        //     A string contating the screen-resolution of the local computer.
        //   streamResolution:
        //     A string contating the screen-resolution used for streaming.
        //     Remark: On linux this is the only required parameter.
        //   receiverResolution:
        //     A string contating the screen-resolution of the remote computer.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        Task StartVNCStreaming( UInt16 portNo,
                                string senderResolution = null,
                                string streamResolution = null, 
                                string receiverResolution = null );
                                    
        //
        // Summary:
        //     Start streaming using ffmpeg on the local computer as streaming-source
        //     and ffplay on the remote computer as streaming-sink.
        // Paremeters:
        //   portNo:
        //     The port-Number used for the UDP-Stream (ffplay will listen on this port).
        //   senderResolution:
        //     A string contating the screen-resolution of the local computer.
        //   streamResolution:
        //     A string contating the screen-resolution used for streaming.
        //     Remark: On linux this is the only required parameter.
        //   receiverResolution:
        //     A string contating the screen-resolution of the remote computer.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        Task StartFFmpegStreaming( UInt16 portNo,
                                   string senderResolution = null,
                                   string streamResolution = null, 
                                   string receiverResolution = null );        

        //
        // Summary:
        //   Stops VNC-viewer and ffplay on the remote-computer, and stops
        //   VNC-server and ffmpeg on the local-computer
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        Task StopStreaming();                     
    }
}