using System;

namespace WirelessDisplayClient.Services
{
    //
    // Summary:
    //     The type of streaming, either VNC or FFmpeg. 'None' is used to
    //     indicate, that no streaming is performed at the moment,
    public enum StreamType
    {
        None,
        VNC,
        FFmpeg
    }


    //
    // Summary:
    //     The type of the expceptions thrown by the classes here.
    public class WDCServiceException : Exception
    {
        public WDCServiceException(string msg) :base (msg)
        {
        }
    }
}
