using System;
using System.Threading.Tasks;

namespace WirelessDisplayClient.Services
{
    public interface IStreamSourceService
    {
        //
        // Summary:
        //     Start streaming-source on the local computer by starting a Process
        //     executing a script (shell /batch).
        // Parameters:
        //   streamType:
        //     One of the stream-types given in enum StreamType (VNC or FFmpeg)
        //   remoteIpAddress:
        //     The IP-Address of the remote 'projecting'-computer to send the stream to.
        //   portNo:
        //     The port-Number used for the remote streaming-sink to listen on.
        //   streamResolution:
        //     A string contating the screen-resolution used for streaming.
        //     In some cases (operating-system / type of streaming) null is alloewed.
        // Exceptions:
        //   T:WirelessDisplayClient.Services.WDCServiceException:
        //     The local streaming source could not be started, for example because of
        //     an error in the script starting the streaming-source
        void StartLocalStreamSource( StreamType streamType,
                                    string remoteIpAddress,
                                    UInt16 portNo,
                                    string streamResolution = null );

        //
        // Summary:
        //   Stops streaming-source on the local-computer
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     If the process could not be killed (should never occur)
        void StopLocalStreaming();     
    }

}