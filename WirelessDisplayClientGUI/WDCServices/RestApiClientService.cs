using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WirelessDisplayClient.Services
{
    public class RestApiClientService : IRestApiClientService
    {
        //////////////////////////////////////////////////////////////
        // private Fields and Constructor
        //////////////////////////////////////////////////////////////
        #region
        
        private readonly ILogger<RestApiClientService> logger;

        // private backup-field for IRestApiClientService.LastKnownRemoteIp
        private IPAddress lastKnownRemoteIp;

        // Used for making GET- and POST-Requests
        private static readonly HttpClient client = new HttpClient();

        public RestApiClientService(ILogger<RestApiClientService> logger)
        {
            this.logger = logger;
        }

        #endregion

        //////////////////////////////////////////////////////////////
        // Implementing the interface IRestApiClientService
        //////////////////////////////////////////////////////////////
        #region

        //
        // Summary:
        //     After a successfull call of Connect() LastKnownRemoteIp contains
        //     The IPv4-Address of the remote-computer (projecting-computer
        //     runnig the program 'WirelessDisplayServer').  
        string IRestApiClientService.LastKnownRemoteIP { get => lastKnownRemoteIp.ToString(); }

        //
        // Summary:
        //     Performs a GET-Request to `ipAddress` and stores `ipAddress` in
        //     lastKnownRemoteIp for later use.
        //     The GET-Request is made to api/StreamPlayer/VncViewerStarted.
        // Parameters:
        //   ipAddress:
        //     The IPv4-Address of the WirelessDisplayServer (projecting-computer).
        //     as a string.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     ipAddress is not valid, or request was not successfull.
        async Task IRestApiClientService.Connect(string ipAddress)
        {
            IPAddress ip;
            try
            {
                ip = IPAddress.Parse(ipAddress);
            }
            catch(FormatException)
            {
                lastKnownRemoteIp = null; 
                logger?.LogInformation($"This is not a valid IP-Address: {ipAddress}");
                throw new WDCServiceException($"This is not a valid IP-Address: {ipAddress}");
            }
            
            lastKnownRemoteIp = ip;

            // Perform a GET-Request only to see, if it worked (Otherwise an Exception
            // is thrown by performGET).
            try
            {
                await performGET<bool>("api/StreamPlayer/VncViewerStarted");
            }
            catch (WDCServiceException e)
            {
                lastKnownRemoteIp = null;
                logger?.LogInformation($"Could not Connect to {ip}");
                throw e;
            }
        }

        //
        // Summary:
        //     Returns the initial screen-resolulion of the remote computer.
        // Returns:
        //     A string with the initial screen-resolution, for example "1280x1024".
        //     If the request was not successfull, "???" is returned.
        async Task<string> IRestApiClientService.GetInitialRemoteScreenResolution()
        {
            try 
            {
                return await performGET<string>("api/ScreenRes/InitialScreenResolution");
            }
            catch (WDCServiceException)
            {
                logger?.LogWarning("Coudn't get initial screen-resolution of remote computer.");
                return "???";
            }
        }

        //
        // Summary:
        //     Returns the current screen-resolulion of the remote computer.
        // Returns:
        //     A string with the current screen-resolution, for example "1024x768".
        //     If the request was not successfull, "???" is returned.
       async Task<string> IRestApiClientService.GetCurrentRemoteScreenResolution()
       {
            try
            {
                return await performGET<string>("api/ScreenRes/CurrentScreenResolution");
            }
            catch (WDCServiceException)
            {
                logger?.LogWarning("Coudn't get current screen-resolution of remote computer.");
                return "???";
            }
       }

        //
        // Summary:
        //     Returns all available screen-resolutions of the remote computer.
        // Returns:
        //     A list of strings with the available screen-resolutions, 
        //     for example { "640x460", "1024x768", ...}.
        //     If the request was not successfull, a list containg "???" is returned.
        async Task<List<string>> IRestApiClientService.GetAvailableRemoteScreenResolutions()
        {
            try
            {
                return await performGET<List<string>>("api/ScreenRes/AvailableScreenResolutions");
            }
            catch (WDCServiceException)
            {
                logger?.LogWarning("Coudn't get available screen-resolutions of remote computer.");
                var dummyList = new List<string>();
                dummyList.Add("???");
                return dummyList;
            }
        }

        //
        // Summary:
        //     Changes the screen-resolution of the remote computer. If this fails
        //     this mehtod just returns after logging a warning.
        // Parameters:
        //   resolution:
        //     A string containing the screen-resolution to set, for example
        //     "1024x768" (The string is without quotes!).
        async Task IRestApiClientService.SetRemoteScreenResolution(string resolution)
        {
            try
            {
                await performPOST<string>("api/ScreenRes/SetScreenResolution", resolution);
            }
            catch (WDCServiceException)
            {
                logger?.LogWarning($"Coudn't set screen-resolutions of remote computer to {resolution}.");
            }
        }

        //
        // Summary:
        //     First stops eventually running remote streaming-sinks and then 
        //     start the desired streaming-sink on the remote computer. 
        // Parameters:
        //   streamType:
        //     Either `StreamType.VNC` or `StreamType.FFmpeg`.
        //   portNo:
        //     The port-Number used for the remote streaming-sink to listen on.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     Requests were not successfull, or LastKnownRemoteIP isn't valid anymore.
        async Task IRestApiClientService.StartRemoteStreamingSink( StreamType streamType, UInt16 portNo )
        {
            // first stop eventually running remote streaming-sinks. If a
            // WDCServiceException occurs, let it handle the caller.
            await ((IRestApiClientService) this).StopRemoteStreamingSink();

            // Depending on the type of streaming, set the correct api-paths.
            string apiPathStart;
            string apiPathStarted;

            switch (streamType)
            {
                case StreamType.VNC:
                {
                    apiPathStart = "api/StreamPlayer/StartVncViewerReverse";
                    apiPathStarted = "api/StreamPlayer/VncViewerStarted";
                    break;
                }
                case StreamType.FFmpeg:
                {
                    apiPathStart = "api/StreamPlayer/StartFfplay";
                    apiPathStarted = "api/StreamPlayer/FfplayStarted";
                    break;
                }
                default:
                {
                    logger?.LogCritical($"BUG: Tried to start not implemented Streaming-Type, or called with StreamType.None:  {streamType.ToString()}");
                    throw new WDCServiceException($"BUG: Tried to start completly implemented Streaming-Type , or called with StreamType.None: {streamType.ToString()}");
                }
            }

            // If a WDCServiceException occurs, just let it handle the caller.
            await performPOST<UInt16>(apiPathStart, portNo);

            bool started = await performGET<bool>(apiPathStarted);
            if (! started)
            {
                logger?.LogError($"Could not start remote {streamType.ToString()} listening on {lastKnownRemoteIp}:{portNo}");
                throw new WDCServiceException($"Could not start remote {streamType.ToString()} listening on {lastKnownRemoteIp}:{portNo}");
            }            
        }

        //
        // Summary:
        //   Stops streaming-sink on the remote-computer.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     Requests were not successfull, or LastKnownRemoteIP isn't valid anymore.
       async Task IRestApiClientService.StopRemoteStreamingSink()
        {
            // This POST will contain the json "null" as posted data (No
            // data is required for this POST).
            await performPOST<object>("api/StreamPlayer/StopAllStreamPlayers", null);

            bool started;
            
            started =  await performGET<bool>("api/StreamPlayer/VncViewerStarted");
            if (started)
            {
                logger?.LogError("Remote VNC-Viewer could not be stopped.");
                throw new WDCServiceException("Remote VNC-Viewer could not be stopped.");
            }
            
            started = await performGET<bool>("api/StreamPlayer/FfplayStarted");
            if (started)
            {
                logger?.LogError("Remote ffplay could not be stopped.");
                throw new WDCServiceException("Remote ffplay could not be stopped.");
            }
        }  

        //
        // Summary:
        //     Returns the type of the remote streaming-sink that has been
        //     started, or StreamType.None, if no streaming-sink has been started.
        // Returns:
        //     StreamType.None, if no streaming-sink has been started. Otherwise
        //     StreamType.VNC or StremType.FFmpeg is returned, depending on the
        //     type of the remote streaming-sink.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     Requests were not successfull, or LastKnownRemoteIP isn't valid anymore.
        async Task<StreamType> IRestApiClientService.TypeOfStartedRemoteStreamSink()
        {
            // If exceptions occur during the GET-Requests, let them handle the caller.
            bool vncStarted = await performGET<bool>("api/StreamPlayer/VncViewerStarted");
            if (vncStarted)
            {
                return StreamType.VNC;
            }

            bool ffmpegStarted = await performGET<bool>("api/StreamPlayer/FfplayStarted");
            if (ffmpegStarted)
            {
                return StreamType.FFmpeg;
            }

            return StreamType.None;
        }

        #endregion

        //////////////////////////////////////////////////////////////
        // Helper methods
        //////////////////////////////////////////////////////////////
        #region 

        //
        // Summary:
        //     Performs a GET-request. The response of this request is a
        //     Json-Object which is deserialized and returned.
        // Type parameters:
        //   T:
        //     The type, which the returned Json-object is deserialized to.
        // Parameters: 
        //   apiPath:
        //     The GET-request sent to http://{lastKnownRemoteIp}/apiPath.
        //  Returns:
        //    The Json-object returned by the server, deserialized to type T.
        // Exceptions:
        //   T:WirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        private  async Task<T> performGET<T>( string apiPath )
        {
            if( lastKnownRemoteIp == null)
            {
                logger?.LogCritical($"No IP-Address has been set. This seems to be a BUG: Before performGET(\"{apiPath}\") is called, lastKnownRemoteIp must contain a valid IP-Address.");
                throw new WDCServiceException($"No IP-Address has been set. This seems to be a BUG: Before performGET(\"{apiPath}\") is called, lastKnownRemoteIp must contain a valid IP-Address.");
            }
                        
            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync($"http://{lastKnownRemoteIp}/{apiPath}");

                //throw an HttpRequestException, if GET-Request was unsuccessful.
                response.EnsureSuccessStatusCode(); 
            }
            catch (HttpRequestException)
            {
                logger?.LogError($"Could not perform a GET-Request to 'http://{lastKnownRemoteIp}/{apiPath}'");
                throw new WDCServiceException($"Could not perform a GET-Request to 'http://{lastKnownRemoteIp}/{apiPath}'");
            }

            string jsonresponse = await response.Content.ReadAsStringAsync();

            T jsonContent;
            try
            {
                jsonContent = JsonSerializer.Deserialize<T>(jsonresponse);
            }
            catch(JsonException)
            {
                // Could not convert json-GET-response to list of strings, 
                // Return an empty List. 
                logger.LogError($"After GET-Request: Could not covert the response from the server '{jsonresponse}' to an opject of type '{typeof(T).ToString()}'");
                throw new WDCServiceException($"After GET-Request: Could not covert the response from the server '{jsonresponse}' to an opject of type '{typeof(T).ToString()}'");
            }

            return jsonContent;
        }


        //
        // Summary:
        //     Performs a POST-request.
        // Type parameters:
        //   T:
        //     The C#-type of the data to be posted. This type is serialied
        //     to a Json-object before posting.
        // Parameters: 
        //   apiPath:
        //     The POST-request sent to http://{lastKnownRemoteIp}/apiPath.
        //   jsonObject:
        //     Contains the data to be posted. jsonObject is serialized to
        //     a Json-Object before posting. If no data has to be posted, pass object
        //     for T and null for jsonObject.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or lastKnownRemoteIP isn't valid anymore.
        private async Task performPOST<T>(string apiPath, T jsonObject)
        {
            // Remark: If T is object, null is also allowed as jsonObject, in this case
            // potsData will become the string "null".
            // If this throws an exception, this must be a Bug in the calling code.
            string postData = JsonSerializer.Serialize<T>(jsonObject);

            HttpResponseMessage response;
            try 
            {
                response = await client.PostAsync(
                    $"http://{lastKnownRemoteIp}/{apiPath}", 
                    new StringContent(postData, System.Text.Encoding.UTF8, "application/json")
                    );
                response.EnsureSuccessStatusCode();
            }
            catch(HttpRequestException)
            {
                logger?.LogError($"During POST-Request to 'http://{lastKnownRemoteIp}/{apiPath}' with data='{jsonObject.ToString()}': Request failed.");
                throw new WDCServiceException($"During POST-Request to 'http://{lastKnownRemoteIp}/{apiPath}' with data='{jsonObject.ToString()}': Request failed.");
            }
        }

        #endregion
    }
}