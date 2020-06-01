using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace WirelessDisplayClient.Services
{
    public class WDCSercviceProviderGeneric : IWDCServciceProvider
    {
        //////////////////////////////////////////////////////////////
        // Fields and Constructor
        //////////////////////////////////////////////////////////////
        #region 

        // In this protected fields the configuration-values passed
        // to the constructor are stored premanently.
        private readonly string _shell;
        private readonly string _shell_Args_Template;
        private readonly string _startStreamingScriptPath;
        private readonly string _startStreamingScriptArgsTemplate;
        private readonly string _manageScreenResScriptPath;
        private readonly string _manageScreenResScriptArgsTemplate;

        // Used for starting local scripts from a shell
        private Process _localStreamSourceProcess;

        private readonly string _initialLocalScreenRes;

        // Used for making GET- and POST-Requests
        private static readonly HttpClient client = new HttpClient();


        //
        // Summary:
        //   Constructor.
        // Parameters:
        //   config:
        //     A key-value-collection whose name-value-pairs are read from App.config.
        //     They provide necessary values to start two scripts on the local computer.
        //     (ffmpeg-start-script and VNC-server-start-script).
        public WDCSercviceProviderGeneric(NameValueCollection config)
        {
            _shell = config["shell"];
            _shell_Args_Template = config["shell_Args_Template"];
            _startStreamingScriptPath = config["Start_Streaming_Script_Path"];
            _startStreamingScriptArgsTemplate = config["Start_Streaming_Script_Args_Template"];
            _manageScreenResScriptPath = config["Manage_Screen_Resolutions_Script_Path"];
            _manageScreenResScriptArgsTemplate = config["Manage_Screen_Resolutions_Script_Args_Template"];
            
             //Check, that external scripts can be found
            FileInfo script = new FileInfo(_startStreamingScriptPath);
            if (! script.Exists)
            {
                throw new FileNotFoundException($"Cannot find script '{script.FullName}'");
            }

            script = new FileInfo(_manageScreenResScriptPath);
            if (! script.Exists)
            {
                throw new FileNotFoundException($"Cannot find script '{script.FullName}'");
            }

            // Get actual local screen resolution and store it in _initialLocalScreenRes
            _initialLocalScreenRes = fetchCurrentScreenResolution();
        }
        #endregion

        //////////////////////////////////////////////////////////////
        // Porperties and Methods implementing IWDCServciceProvider
        //////////////////////////////////////////////////////////////
        #region

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
        //     Returns the initial screen-resolulion of the local computer.
        // Returns:
        //     A string with the initial screen-resolution, for example "1280x1024".
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The local process didn't start or not return a valid screen-resolution
        string IWDCServciceProvider.GetInitialLocalScreenResolution()
        {
            return _initialLocalScreenRes;
        }

        //
        // Summary:
        //     Returns the current screen-resolulion of the local computer.
        // Returns:
        //     A string with the current screen-resolution, for example "1024x768".
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The local process didn't start or not return a valid screen-resolution
        string IWDCServciceProvider.GetCurrentLocalScreenResolution()
        {
            return fetchCurrentScreenResolution();
        }

        //
        // Summary:
        //     Returns all available screen-resolutions of the local computer.
        // Returns:
        //     A list of strings with the available screen-resolutions, 
        //     for example { "640x460", "1024x768", ...}.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The local process didn't start or not return a valid screen-resolution
        List<string> IWDCServciceProvider.GetAvailableLocalScreenResolutions()
        {
            string scriptArgs = _manageScreenResScriptArgsTemplate;
            scriptArgs = scriptArgs.Replace("%ACTION", "ALL");
            scriptArgs = scriptArgs.Replace("%RESOLUTION", "null");
            
            List<string> outputLines = genericExecuteScreenResScript(scriptArgs);
            
            if (outputLines.Count == 0 )
            {
                throw new WDCServiceException("ERROR: Script for getting local screen-resolutions didn't return any value.");
            }

            return outputLines;
        }

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
        void IWDCServciceProvider.SetLocalScreenResolution(string resolution)
        {
            string scriptArgs = _manageScreenResScriptArgsTemplate;
            scriptArgs = scriptArgs.Replace("%ACTION", "SET");
            scriptArgs = scriptArgs.Replace("%RESOLUTION", resolution);
            
            genericExecuteScreenResScript(scriptArgs);
        }

        //
        // Summary:
        //     Returns the initial screen-resolulion of the remote computer.
        // Returns:
        //     A string with the initial screen-resolution, for example "1280x1024".
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        async Task<string> IWDCServciceProvider.GetInitialRemoteScreenResolution()
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
        async Task<string> IWDCServciceProvider.GetCurrentRemoteScreenResolution()
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


        //
        // Summary:
        //     Start the local Script for starting the streaming-source and start the
        //     the remote streaming-sink.
        // Parameters:
        //   typeOfStreaming:
        //     One of the stream-types given in enum StreamType (VNC or FFmpeg)
        //   portNo:
        //     The port-number used for the VNC-connection
        //   senderResolution:
        //     The screen-resolution of the local computer (can be null, if not needed
        //     by the script).
        //   streamResolution:
        //     The screen-resolution for the stream. Can normally not be null.
        //   receiverResolution:
        //     The screen-resolution of the remote computer (can be null, if not needed
        //     by the script).
        // Exceptions:
        //     Several Exceptions are thrown by Process.Start(), if the shell executing 
        //     the script cannot be executed.
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     Thrown, if the shell-script can be started, but terminates within 1 second,
        //     or if starting the remote VNC-viewer fails.
        async Task IWDCServciceProvider.StartStreaming( StreamType typeOfStreaming,
                                     UInt16 portNo,
                                     string senderResolution,
                                     string receiverResolution )
        {
            // First start remote streaming-sink
            // If this call is not successfull, an Exception is thrown
            await StartRemoteStreamingSink(typeOfStreaming, portNo);

            // Then try to start local streaming-source
            try
            {
                await genericStartStreamProcess( typeOfStreaming, 
                                    portNo, senderResolution, receiverResolution);
            }
            catch (Exception e)
            {
                // If the local process could noit be started, also terminate the
                // remote process
                await StopRemoteStreamPlayers();
                throw e;
            }           
        }


        //
        // Summary:
        //     Stops the remote straming sink and stops the process executing 
        //     the local script running the streaming-source.
        async Task IWDCServciceProvider.StopStreaming()
        {
            // First stop remote players
            await StopRemoteStreamPlayers();

            // Kill and Dispose local process
            genericStopStreamProcess();           
        }

        #endregion

        ///////////////////////////////////////////////////////////////
        // Helper functions 
        ///////////////////////////////////////////////////////////////
        #region

        // 
        // Summary:
        //     Starts the streaming sink on the remote computer.
        // Parameters:
        //   portNo:
        //     The port-numer, the remote streaming sink listens on.
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     The request was not successfull, or LastKnownRemoteIP isn't valid anymore.
        protected async Task StartRemoteStreamingSink( StreamType typeOfStream, UInt16 portNo)
        {
            string apiPathStart;
            string apiPathStarted;

            switch (typeOfStream)
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
                    throw new WDCServiceException($"BUG: Not completly implemented Streaming-Type {typeOfStream.ToString()}");
                }
            }

            // If WDCServiceException occurs, just let it handle the caller.
            await performPOST<UInt16>(apiPathStart, portNo);

            bool started = false;

            // We have to do clean-up, if an exception occurs, so use finally
            try
            {
                started = await performGET<bool>(apiPathStarted);
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
                        throw new WDCServiceException($"ERROR: Could not start remote {typeOfStream.ToString()} listening on {_lastKnownRemoteIp}:{portNo}");
                    }
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

        //
        // Summary:
        //    Get the current local screen-resolution
        // Returns:
        //     The current local screen-resolution as a string, like "1980x1040"
        private string fetchCurrentScreenResolution(int timeout=10000)
        {
            string scriptArgs = _manageScreenResScriptArgsTemplate;
            scriptArgs = scriptArgs.Replace("%ACTION", "GET");
            scriptArgs = scriptArgs.Replace("%RESOLUTION", "null");
            
            List<string> outputLines = genericExecuteScreenResScript(scriptArgs, timeout);
            
            if (outputLines.Count != 1)
            {
                throw new WDCServiceException("ERROR: Script for getting local screen-resolution didn't return the expected value.");
            }

            return outputLines[0];
        }

        //
        // Summary:
        //     Runs the Script for managing local screen-resolutions. This script
        //     is supposed to run only short time.
        // Parameters:
        //   scriptArgs:
        //     The arguments passed to the script (separated by blanks)
        // Returns:
        //     A Tuple with the Exit-Code of the script and the lines it wrote to
        //     its standard-output.
        private List<string> genericExecuteScreenResScript(string scriptArgs, int timeout = 10000)
        {
            FileInfo scriptPath = new FileInfo(_manageScreenResScriptPath);

            string argsForProcess = _shell_Args_Template;
            argsForProcess = argsForProcess.Replace("%SCRIPT", scriptPath.FullName);
            argsForProcess = argsForProcess.Replace("%ARGS",scriptArgs);
            
            List<string> outputLines = new List<string>();

            using (Process _manageScreenResProcess = new Process())
            {
            
                _manageScreenResProcess.StartInfo.FileName = _shell;
                _manageScreenResProcess.StartInfo.Arguments = argsForProcess;
                _manageScreenResProcess.StartInfo.WorkingDirectory = scriptPath.Directory.FullName;
                _manageScreenResProcess.StartInfo.UseShellExecute = false;
                _manageScreenResProcess.StartInfo.CreateNoWindow = true;
                _manageScreenResProcess.StartInfo.RedirectStandardOutput = true;
                _manageScreenResProcess.StartInfo.RedirectStandardError = true;

                try 
                {
                    _manageScreenResProcess.Start();
                }
                catch (Exception e)
                {
                    throw new WDCServiceException($"Could not start Script for managing screen-resoltuions: {e.Message}");
                }

                bool exited = _manageScreenResProcess.WaitForExit(timeout);
               
                if (! exited )
                {
                    throw new WDCServiceException($"Process not finished within {timeout} Milliseconds: '{_shell} {argsForProcess}'. Scripting Error?");
                }               

                if (_manageScreenResProcess.ExitCode != 0 )
                {
                    throw new WDCServiceException($"Process failed with exit-code {_manageScreenResProcess.ExitCode}: '{_shell} {argsForProcess}'. Scripting Error?");
                }

                string line;
                while ( (line = _manageScreenResProcess.StandardOutput.ReadLine()) != null)
                {
                    outputLines.Add(line);
                }

                return outputLines;
            }
            
        }

        //
        // Summary:
        //     Starts a script using the shell of the operating system,
        //     and passing arguments in a predifined way to this script.
        //     Arguments are: type of stream, port-number, and the three
        //     screen-resolutions (local, stream, remote sceen-resolution).
        // Parameters:
        //   scriptPath:
        //     Absolute or relative filepath to the script to be executed
        //   typeOfStreaming:
        //     One of the stream-types given in enum StreamType (VNC or FFmpeg)
        //   portNo:
        //     Argument for the script: The port-number for the streaming.
        //   senderResolution:
        //     Argument for the script: The screen-resolution of the local computer.
        //   streamResolution:
        //     Argument for the script: The screen-resolution used for streaming.
        //   receiverResolution:
        //     Argument for the script: The screen-resolution of the remote computer.
        // Exceptions:
        //     Several Exceptions are thrown by Process.Start(), if the shell executing 
        //     the script cannot be executed.
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     Thrown, if the shell-script can be started, but terminates within 1 second.
        protected async Task genericStartStreamProcess(
                                     StreamType typeOfStreaming,
                                     UInt16 portNo,
                                     string senderResolution = null,
                                     string receiverResolution = null )
        {
            FileInfo scriptPath = new FileInfo(_startStreamingScriptPath);

            string ipAddress = _lastKnownRemoteIp;
            if (string.IsNullOrEmpty(ipAddress))
            {
                throw new WDCServiceException("This seems to be a BUG: LastKnonIPAddress is not set when calling genericStartProcess()");
            }

            string scriptArgs = _startStreamingScriptArgsTemplate;
            scriptArgs = scriptArgs.Replace("%STREAMING_TYPE", typeOfStreaming.ToString());
            scriptArgs = scriptArgs.Replace("%IP_ADDR", ipAddress);
            scriptArgs = scriptArgs.Replace("%PORT_NO", portNo.ToString());
            scriptArgs = scriptArgs.Replace("%WxH_SENDER", !string.IsNullOrEmpty(senderResolution) ? senderResolution : "null");
            scriptArgs = scriptArgs.Replace("%WxH_RECEIVER", !string.IsNullOrEmpty(receiverResolution) ? receiverResolution : "null");

            string argsForProcess = _shell_Args_Template;
            argsForProcess = argsForProcess.Replace("%SCRIPT", scriptPath.FullName);
            argsForProcess = argsForProcess.Replace("%ARGS", scriptArgs);
            
            // Kill and Dispose old process, if it exists and is running
            genericStopStreamProcess();

            // Create new process
            _localStreamSourceProcess = new Process();
            _localStreamSourceProcess.StartInfo.FileName = _shell;
            _localStreamSourceProcess.StartInfo.Arguments = argsForProcess;
            _localStreamSourceProcess.StartInfo.WorkingDirectory = scriptPath.Directory.FullName;
            _localStreamSourceProcess.StartInfo.UseShellExecute = false;
            _localStreamSourceProcess.StartInfo.CreateNoWindow = true;

            // This should never throw an expeption, if at least the name of the 
            // shell(bash, cmd.exe) is correct.
            _localStreamSourceProcess.Start();

            // Check, that the process does not die within one second
            await Task.Delay(1000);
            
            if (_localStreamSourceProcess.HasExited)
            {
                throw new WDCServiceException($"Process terminated immediately: '{_shell} {argsForProcess}'");
            }
        }


        //
        // Summary:
        //   Stops the local process started by genericStartProcess (either 
        //   VNC-server-start-script or ffmpeg-start-script)
        protected void genericStopStreamProcess()
        {
            if (_localStreamSourceProcess != null && ! _localStreamSourceProcess.HasExited)
            {
                _localStreamSourceProcess.Kill( entireProcessTree : true );
            }
            if (_localStreamSourceProcess != null)
            {
                _localStreamSourceProcess.Dispose();
                _localStreamSourceProcess = null;
            }
        }


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

        #endregion
    }
}
