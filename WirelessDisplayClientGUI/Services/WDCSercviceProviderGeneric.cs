using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace WirelessDisplayClient.Services
{
    public class WDCSercviceProviderGeneric : WDCServiceProviderBase                             
    {

        // In this protected fields the configuration-values passed
        // to the constructor are stored premanently.
        protected readonly string _shell;
        protected readonly string _shell_Args_Template;
        protected readonly string _vncServerScriptPath;
        protected readonly string _vncServerScriptArgs;
        protected readonly string _ffmpegSriptPath;
        protected readonly string _ffpmpegScriptArgs;

        protected Process _localStreamSourceProcess;

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
            _vncServerScriptPath = config["VNCserver_Script_Path"];
            _vncServerScriptArgs = config["VNCserver_Script_Args"];
            _ffmpegSriptPath = config["ffmpeg_Script_Path"];
            _ffpmpegScriptArgs = config["ffmpeg_Script_Args"];

            //Check, that external scripts can be found
            FileInfo vncScript = new FileInfo(_vncServerScriptPath);
            if (! vncScript.Exists)
            {
                throw new FileNotFoundException($"Cannot find VNC-server-start-script '{vncScript.FullName}'");
            }

            FileInfo ffmpegScript = new FileInfo(_ffmpegSriptPath);
            if (! ffmpegScript.Exists)
            {
                throw new FileNotFoundException($"Cannout find FFmpeg-start-script '{ffmpegScript.FullName}'");
            }
        }


        //
        // Summary:
        //   Start the local Script for starting the VNC-Server and start the
        //   the remote VNC-client.
        // Parameters:
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
        public override async Task StartVNCStreaming( UInt16 portNo,
                                     string senderResolution = null,
                                     string streamResolution = null, 
                                     string receiverResolution = null )
        {
            // First start remote VNC-client (in listen-mode / reverse-connection)
            // If this call is not successfull, an Exception is thrown
            await base.StartRemoteVNCViewerReverse(portNo);

            // Then try to start local VNC-Server-start-script
            try
            {
                await genericStartProcess( _vncServerScriptPath, _vncServerScriptArgs, 
                          portNo, senderResolution, streamResolution, receiverResolution);
            }
            catch (Exception e)
            {
                // If the local process could noit be started, also terminate the
                // remote process
                await base.StopRemoteStreamPlayers();
                throw e;
            }           
        }


        //
        // Summary:
        //   Start the local Script for starting ffmpeg and start ffplay on the
        //   remote computer.
        // Parameters:
        //   portNo:
        //     The port-number used for the stream from ffmpeg to ffplay.
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
        //     or if starting ffplay on the remote computer fails.

        public override async Task StartFFmpegStreaming( UInt16 portNo,
                                     string senderResolution = null,
                                     string streamResolution = null, 
                                     string receiverResolution = null )
        {
            // First start ffplay on the remote computer.
            await base.StartRemoteFFplay(portNo);
            
            // Then try to start local ffmpeg-start-script
            try
            {
                await genericStartProcess( _ffmpegSriptPath, _ffpmpegScriptArgs, portNo, 
                                senderResolution, streamResolution, receiverResolution);
            }
            catch (Exception e)
            {
                // If the local process could noit be started, also terminate the
                // remote process
                await base.StopRemoteStreamPlayers();
                throw e;
            }
                   
        }

        public override async Task StopStreaming()
        {
            // First stop remote players
            await base.StopRemoteStreamPlayers();

            // Kill and Dispose local process
            genericStopProcess();           
        }


        ////////////////////////////////////////////////////////
        // Helper functions
        ////////////////////////////////////////////////////////

        //
        // Summary:
        //     Starts a script using the shell of the operating system,
        //     and passing argumetns to this script. The scripts takes as arguments
        //     the '_shell_Args_Template' defined in App.config. In this template
        //     some placeholders are replaced by the given parameters.
        // Parameters:
        //   scriptToExecutable:
        //     Absolute or relative filepath to the script to be executed.
        //   portNo:
        //     Argument for the script. Replaces %PPPPP in the _shell_Args_Template.
        //   senderResolution:
        //     Argument for the script. Replaces %WxH_SENDER in the _shell_Args_Template.
        //   streamResolution:
        //     Argument for the script. Replaces %WxH_STREAM in the _shell_Args_Template.
        //   receiverResolution:
        //     Argument for the script. Replaces %WxH_RECEIVER in the _shell_Args_Template.
        // Exceptions:
        //     Several Exceptions are thrown by Process.Start(), if the shell executing 
        //     the script cannot be executed.
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     Thrown, if the shell-script can be started, but terminates within 1 second.
        protected async Task genericStartProcess( string scriptPath,
                                     string scriptArgs,
                                     UInt16 portNo,
                                     string senderResolution = null,
                                     string streamResolution = null, 
                                     string receiverResolution = null )
        {
            string ipAddress = ((IWDCServciceProvider) this).LastKnownRemoteIP;
            if (string.IsNullOrEmpty(ipAddress))
            {
                throw new WDCServiceException("This seems to be a BUG: LastKnonIPAddress is not set when calling genericStartProcess()");
            }

            string argsForProcess = _shell_Args_Template.Replace("%COMMAND", $"{scriptPath} {scriptArgs}");

            argsForProcess = argsForProcess.Replace("%IIIIIIII", ipAddress);
            argsForProcess = argsForProcess.Replace("%PPPPP", portNo.ToString());
            argsForProcess = argsForProcess.Replace("%WxH_SENDER", string.IsNullOrEmpty(senderResolution) ? "dummyArg" : senderResolution);
            argsForProcess = argsForProcess.Replace("%WxH_STREAM", string.IsNullOrEmpty(streamResolution) ? "dummyArg" : streamResolution);
            argsForProcess = argsForProcess.Replace("%WxH_RECEIVER", string.IsNullOrEmpty(receiverResolution) ? "dummyArg" : receiverResolution);
            
            // Kill and Dispose old process, if it exists and is running
            genericStopProcess();

            // Create new process
            _localStreamSourceProcess = new Process();
            _localStreamSourceProcess.StartInfo.FileName = _shell;
            _localStreamSourceProcess.StartInfo.Arguments = argsForProcess;
            //_localStreamSourceProcess.StartInfo.UseShellExecute = true;

            // If this throws an exception, the program crashes, but shows
            // the exception in the terminal. In production state this
            // should never happen.
            _localStreamSourceProcess.Start();

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
        protected void genericStopProcess()
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
    }
}
