using System.Collections.Generic;

namespace WirelessDisplayClient.Services
{
    // Summary:
    //     Objects that implement this interface, are able to manipulate
    //     the screen-resolution of the local computer.
    public interface IScreenResolutionService
    {
        // Summary:
        //    A list of all available screen-resolutions on this computer.
        //    The screen-resolutions are strings similar to "1024x768".
        List<string> AvailableScreenResolutions { get; }

        // Summary:
        //     The inial screen-resolution is the one, that was set, when
        //     the program starts   . 
        string InitialScreenResolution { get; }

        // Summary:
        //     The current screen-resolution. A string similat to "1024x768"
        string CurrentScreenResolution { get; }

        // Summary:
        //     Changes to scree-resolution of the local computer.
        // Parameters:
        //   screenResolution:
        //     A string like "1980x1040" containting the screen-resolution, that
        //     shall be set.
        void SetScreenResolution(string screenResolution );
    }

}