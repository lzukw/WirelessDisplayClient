using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WirelessDisplayClient.Services
{
    // Summary:
    //     Objects that implement this interface, are able to communicate
    //     with the remote 'projecting-computer'.
    public interface IRestApiClientService
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
        string LastKnownRemoteIP { get; }

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
        //     Start streaming-sink on the remote computer. 
        // Parameters:
        //   streamType:
        //      Either `StreamType.VNC` or `StreamType.FFmpeg`.
        //   portNo:
        //     The port-Number used for the remote streaming-sink to listen on.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        Task StartRemoteStreamingSink( StreamType streamType, UInt16 portNo );

        //
        // Summary:
        //   Stops streaming-sink on the remote-computer.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        Task StopRemoteStreamingSink();    

        //
        // Summary:
        //     Returns the type of the remote streaming-sink that has been
        //     started, or StreamType.None, if no streaming-sink has been started.
        // Returns:
        //     StreamType.None, if no streaming-sink has been started. Otherwise
        //     StreamType.VNC or StremType.FFmpeg is returned, depending on the
        //     type of the remote streaming-sink.
        Task<StreamType> TypeOfStartedRemoteStreamSink();
    }
}