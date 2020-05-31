/*
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Specialized;

namespace WirelessDisplayClient.Services
{
    public abstract class WDCServiceProviderBase : IWDCServciceProvider
    {
        // Used for making GET- and POST-Requests
        private static readonly HttpClient client = new HttpClient();


        // backup-field for IWDCServciceProvider.LastKnownRemoteIP
        private string _lastKnownRemoteIp = "";


        //
        // Summary:
        //     After a successfull call of Connect() LastKnownRemoteIP contains
        //     The IPv4-Address of the WirelessDisplayServer (projecting-computer).  
       string IWDCServciceProvider.LastKnownRemoteIP 
        { 
            get => _lastKnownRemoteIp;
            set => _lastKnownRemoteIp=value;
        }


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
        async Task IWDCServciceProvider.Connect(string ipAddress)
        {
            IPAddress ip;
            try
            {
                ip = IPAddress.Parse(ipAddress);
            }
            catch(FormatException)
            {
                _lastKnownRemoteIp = ""; 
                throw new WDCServiceException($"INFO: This is not a valid IP-Address: {ipAddress}");
            }

            _lastKnownRemoteIp = ip.ToString();
            
            // Perform a GET-Request only to see, if it worked (Otherwise an Exception
            // is thrown by performGET).
            await performGET<bool>("api/StreamPlayer/VncViewerStarted");
        }


        //
        // Summary:
        //     Returns the initial screen-resolulion of the remote computer.
        // Returns:
        //     A string with the initial screen-resolution, for example "1280x1024".
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        async Task<string> IWDCServciceProvider.GetInitialScreenResolution()
        {
            return await performGET<string>("api/ScreenRes/InitialScreenResolution");
        }


        //
        // Summary:
        //     Returns the current screen-resolulion of the remote computer.
        // Returns:
        //     A string with the current screen-resolution, for example "1024x768".
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        async Task<string> IWDCServciceProvider.GetCurrentScreenResolution()
        {
            return await performGET<string>("api/ScreenRes/CurrentScreenResolution");
        }

        
        //
        // Summary:
        //     Returns all available screen-resolutions of the remote computer.
        // Returns:
        //     A list of strings with the available screen-resolutions, 
        //     for example { "640x460", "1024x768", ...}.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        async Task<List<string>> IWDCServciceProvider.GetAvailableRemoteScreenResolutions() 
        { 
            return await performGET<List<string>>("api/ScreenRes/AvailableScreenResolutions");
        }


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
        async Task IWDCServciceProvider.SetRemoteScreenResolution(string resolution)
        {
            await performPOST<string>("api/ScreenRes/SetScreenResolution", resolution);
        }


        public abstract Task StartStreaming( StreamType typeOfStream,
                                     UInt16 portNo,
                                     string senderResolution = null,
                                     string streamResolution = null, 
                                     string receiverResolution = null );

        public abstract Task StopStreaming();  


        ///////////////////////////////////////////////////////////////
        // Helper functions needed by child-classes
        ///////////////////////////////////////////////////////////////

        // 
        // Summary:
        //     Starts the VNC-Viewer in reverse-connection on the remote computer.
        // Parameters:
        //   portNo:
        //     The port-numer, the remote VNC-Viewer listens on.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        protected async Task StartRemoteStreamingSink( StreamType typeOfStream, UInt16 portNo)
        {
            switch (typeOfStream)
            {
                case StreamType.VNC:
                {
                    // If WDCServiceException occurs, just let it handle the caller.
                    await performPOST<UInt16>("api/StreamPlayer/StartVncViewerReverse", portNo);

                    bool started = false;

                    // We have to do clean-up, if an exception occurs, so use finally
                    try
                    {
                        started = await performGET<bool>("api/StreamPlayer/VncViewerStarted");
                    }
                    finally
                    {
                        // If an exception occured, or the remote program could not be started
                        // successfully: Stop remote player again and throw exception. 
                        if (! started)
                        {
                            try
                            {
                                await StopRemoteStreamPlayers();
                            }
                            finally
                            {
                                throw new WDCServiceException($"ERROR: Could not start remote VNC-viewer listening on {_lastKnownRemoteIp}:{portNo}");
                            }
                        }   
                    }
                    break;
                }

                case StreamType.FFmpeg:
                {
                    // If WDCServiceException occurs, just let it handle the caller.
                    await performPOST<UInt16>("api/StreamPlayer/StartFfplay", portNo);

                    bool started = false;

                    // We have to do clean-up, if an exception occurs, so use finally
                    try
                    {
                        started = await performGET<bool>("api/StreamPlayer/FfplayStarted");
                    }
                    finally
                    {
                        // If an exception occured, or the remote program could not be started
                        // successfully: Stop remote player again and throw exception. 
                        if (! started)
                        {
                            try
                            {
                                await StopRemoteStreamPlayers();
                            }
                            finally
                            {
                                throw new WDCServiceException($"ERROR: Could not start remote FFplay listening on {_lastKnownRemoteIp}:{portNo}");
                            }
                        }      
                    }
                    break;
                }
            }
        }


        // 
        // Summary:
        //     Stops remote streaming-sink.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
         protected async Task StopRemoteStreamPlayers()
        {
            // This POST will contain the json "null" as posted data (No
            // data is required for this POST).
            await performPOST<object>("api/StreamPlayer/StopAllStreamPlayers", null);

            bool started;
            
            started =  await performGET<bool>("api/StreamPlayer/VncViewerStarted");
            if (started)
            {
                throw new WDCServiceException("ERROR: Remote VNC-Viewer could not be stopped.");
            }
            
            started = await performGET<bool>("api/StreamPlayer/FfplayStarted");
            if (started)
            {
                throw new WDCServiceException("ERROR: Remote ffplay could not be stopped.");
            }
        }

        ///////////////////////////////////////////////////////////////
        // Helper-methods for GET- and POST-Requests
        ///////////////////////////////////////////////////////////////

        //
        // Summary:
        //     Performs a GET-request. The response of this request is a
        //     Json-Object which is deserialized and returned.
        // Type parameters:
        //   T:
        //     The type, which the returned Json-object is deserialized to.
        // Parameters: 
        //   apiPath:
        //     The GET-request sent to http://{_lastKnownRemoteIp}/apiPath.
        //  Returns:
        //    The Json-object returned by the server, deserialize to type T.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        private  async Task<T> performGET<T>( string apiPath )
        {
            if( string.IsNullOrEmpty(_lastKnownRemoteIp))
            {
                throw new WDCServiceException("ERROR: No IP-Address has been set. This seems to be a BUG: Before performGET() is called, _lastKnownRemoteIp must contain a valid IP-Address.");
            }
                        
            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync($"http://{_lastKnownRemoteIp}/{apiPath}");

                //throw an HttpRequestException, if GET-Request was unsuccessful.
                response.EnsureSuccessStatusCode(); 
            }
            catch (HttpRequestException)
            {
                throw new WDCServiceException($"ERROR: Could not perform a GET-Request to 'http://{_lastKnownRemoteIp}/{apiPath}'");
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
                throw new WDCServiceException($"ERROR: After GET-Request: Could not covert the response from the server '{jsonresponse}' to an opject of type '{typeof(T).ToString()}'");
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
        //     The POST-request sent to http://{_lastKnownRemoteIp}/apiPath.
        //   jsonObject:
        //     Contains the data to be posted. jsonObject is serialized to
        //     a Json-Object before posting.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        private async Task performPOST<T>(string apiPath, T jsonObject)
        {
            // Remark: null is also allowed as jsonObject, in this case
            // potsData will become the string "null".
            string postData = JsonSerializer.Serialize<T>(jsonObject);

            HttpResponseMessage response;
            try 
            {
                response = await client.PostAsync(
                    $"http://{_lastKnownRemoteIp}/{apiPath}", 
                    new StringContent(postData, System.Text.Encoding.UTF8, "application/json")
                    );
                response.EnsureSuccessStatusCode();
            }
            catch(HttpRequestException)
            {
                throw new WDCServiceException($"ERROR: During POST-Request to 'http://{_lastKnownRemoteIp}/{apiPath}' with data='{jsonObject.ToString()}': Request failed.");
            }
        }



    }
}

*/