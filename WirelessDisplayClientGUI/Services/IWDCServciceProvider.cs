using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WirelessDisplayClient.Services
{
    public enum StreamType
    {
        VNC,
        FFmpeg
    }

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
        //     Returns the initial screen-resolulion of the local computer.
        // Returns:
        //     A string with the initial screen-resolution, for example "1280x1024".
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The local process didn't start or not return a valid screen-resolution
        string GetInitialLocalScreenResolution();

        //
        // Summary:
        //     Returns the current screen-resolulion of the local computer.
        // Returns:
        //     A string with the current screen-resolution, for example "1024x768".
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The local process didn't start or not return a valid screen-resolution
        string GetCurrentLocalScreenResolution();

        //
        // Summary:
        //     Returns all available screen-resolutions of the local computer.
        // Returns:
        //     A list of strings with the available screen-resolutions, 
        //     for example { "640x460", "1024x768", ...}.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The local process didn't start or not return a valid screen-resolution
        List<string> GetAvailableLocalScreenResolutions();

        //
        // Summary:
        //     Changes the screen-resolution of the local computer.
        // Parameters:
        //   resolution:
        //     A string containing the screen-resolution to set, for example
        //     "1024x768" (The string is without quotes!).
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The local process didn't start or not return a valid screen-resolution
        void SetLocalScreenResolution(string resolution);

        //
        // Summary:
        //     Returns the initial screen-resolulion of the remote computer.
        // Returns:
        //     A string with the initial screen-resolution, for example "1280x1024".
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        Task<string> GetInitialRemoteScreenResolution();

        //
        // Summary:
        //     Returns the current screen-resolulion of the remote computer.
        // Returns:
        //     A string with the current screen-resolution, for example "1024x768".
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        Task<string> GetCurrentRemoteScreenResolution();

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
        //     Start streaming-source on the local computer and streaming-sink
        //     on the remote computer. 
        // Parameters:
        //   typeOfStream:
        //     One of the stream-types given in enum StreamType (VNC or FFmpeg)
        //   portNo:
        //     The port-Number used for the remote streaming-sink to listen on.
        //   senderResolution:
        //     A string contating the screen-resolution of the local computer.
        //     In some cases (operating-system / type of streaming) null could be alloewed.
        //   streamResolution:
        //     A string contating the screen-resolution used for streaming.
        //     In some cases (operating-system / type of streaming) null coudl be alloewed.
        //   receiverResolution:
        //     A string contating the screen-resolution of the remote computer.
        //     In some cases (operating-system / type of streaming) null coudl be alloewed.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        Task StartStreaming( StreamType typeOfStream,
                             UInt16 portNo,
                             string senderResolution = null,
                             string receiverResolution = null );


        //
        // Summary:
        //   Stops streaming-sink on the remote-computer, and stops
        //   the streaming-source on the local-computer
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        Task StopStreaming();                     
    }
}