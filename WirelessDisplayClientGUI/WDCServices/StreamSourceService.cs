using System;
using System.IO;
using System.Net;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace WirelessDisplayClient.Services
{
    // Summary:
    //     Class for starting and stopping a script that executes commands
    //     to start the local streaming-source (VNC-Server or FFmpeg)
    public class StreamSourceService : IStreamSourceService
    {
        //////////////////////////////////////////////////////////////
        // private Fields and Constructor
        //////////////////////////////////////////////////////////////
        private readonly ILogger<StreamSourceService> logger;
        private readonly string shell;
        private readonly string shellArgsTemplate;
        private readonly string startStreamingSourceScriptPath;
        private readonly string startStreamingSourceScriptArgsTemplate;

        // The process executing the script that executes the streaming-source-commands
        private Process localStreamSourceProcess;

        //
        // Summary:
        //     Constructor.
        // Parameters:
        //   logger:
        //     A logger. Can be null, if no logging is used.
        //   shell:
        //     The command to execute (bash or cmd.exe)
        //   shellArgsTemplate:
        //     A string passed as command-line-arguments to the shell-command.
        //     Must contain the placeholders %SCRIPT and %ARGS.
        //   startStreamingSourceScriptPath:
        //     The file-path to the script to be executed for starting the local streaming-source
        //   startStreamingSourceScriptArgsTemplate:
        //     The command-line-arguments the script expects. Must contain the 
        //     placeholders %STREAMING_TYPE, "%IP_ADDR, %PORT_NO and %WxH_STREAM.
        // Exceptions:
        //   T:System.IO.FileNotFoundException:
        //     startStreamingSourceScriptPath is not found.
        //   T:System.ArgumentException:
        //     shellArgsTemplate or startStreamingSourceScriptArgsTemplate do not contain the
        //     expected placeholders.
        public StreamSourceService( ILogger<StreamSourceService> logger,
                                    string shell,
                                    string shellArgsTemplate,
                                    string startStreamingSourceScriptPath,
                                    string startStreamingSourceScriptArgsTemplate)
        {
            this.logger = logger;
            FileInfo scriptPath = new FileInfo( startStreamingSourceScriptPath );
            if ( ! scriptPath.Exists )
            {
                logger?.LogCritical($"Script-file does not exist: '{scriptPath.FullName}'");
                throw new FileNotFoundException($"Script-file does not exist: '{scriptPath.FullName}'");
            }

            if ( ! shellArgsTemplate.Contains("%SCRIPT") || ! shellArgsTemplate.Contains("%ARGS"))
            {
                logger?.LogCritical($"shellArgsTemplate must contain %SCRIPT and %ARGS, but is: '{shellArgsTemplate}'");
                throw new ArgumentException($"shellArgsTemplate must contain %SCRIPT and %ARGS, but is: '{shellArgsTemplate}'");
            }

            if ( ! startStreamingSourceScriptArgsTemplate.Contains("%STREAMING_TYPE") ||
                 ! startStreamingSourceScriptArgsTemplate.Contains("%IP_ADDR") ||
                 ! startStreamingSourceScriptArgsTemplate.Contains("%PORT_NO") ||
                 ! startStreamingSourceScriptArgsTemplate.Contains("%WxH_STREAM") )
            {
                logger?.LogCritical($"startStreamingSourceScriptPath must contain %STREAMING_TYPE, %IP_ADDR, %PORT_NO and %WxH_STREAM, but is: {startStreamingSourceScriptPath}");
                throw new ArgumentException($"startStreamingSourceScriptPath must contain %STREAMING_TYPE, %IP_ADDR, %PORT_NO and %WxH_STREAM, but is: {startStreamingSourceScriptPath}");
            }

            this.shell = shell;
            this.shellArgsTemplate = shellArgsTemplate;
            this.startStreamingSourceScriptPath = scriptPath.FullName;
            this.startStreamingSourceScriptArgsTemplate = startStreamingSourceScriptArgsTemplate;
        }

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
        void  IStreamSourceService.StartLocalStreamSource( StreamType streamType,
                                                            string remoteIpAddress,
                                                            UInt16 portNo,
                                                            string streamResolution )
        {
            FileInfo scriptPath = new FileInfo(startStreamingSourceScriptPath);
            if ( ! scriptPath.Exists)
            {
                logger?.LogCritical($"Script not found anymore: '{scriptPath.FullName}'");
                throw new WDCServiceException($"Script not found anymore: '{scriptPath.FullName}'");
            }

            IPAddress dummy;
            if ( string.IsNullOrWhiteSpace(remoteIpAddress) || 
                 ! IPAddress.TryParse( remoteIpAddress, out dummy ) )
            {
                logger?.LogWarning($"This is not a valid IP-Address: '{remoteIpAddress}'");
                throw new WDCServiceException($"This is not a valid IP-Address: '{remoteIpAddress}'");
            }

            string scriptArgs = startStreamingSourceScriptArgsTemplate;
            scriptArgs = scriptArgs.Replace("%STREAMING_TYPE", streamType.ToString());
            scriptArgs = scriptArgs.Replace("%IP_ADDR", remoteIpAddress);
            scriptArgs = scriptArgs.Replace("%PORT_NO", portNo.ToString());
            scriptArgs = scriptArgs.Replace("%WxH_STREAM", !string.IsNullOrEmpty(streamResolution) ? streamResolution : "null");

            string argsForProcess = shellArgsTemplate;
            argsForProcess = argsForProcess.Replace("%SCRIPT", scriptPath.FullName);
            argsForProcess = argsForProcess.Replace("%ARGS", scriptArgs);
            
            // First kill an eventually running local process
            ((IStreamSourceService) this).StopLocalStreaming();

            // Create new process
            localStreamSourceProcess = new Process();
            localStreamSourceProcess.StartInfo.FileName = shell;
            localStreamSourceProcess.StartInfo.Arguments = argsForProcess;
            localStreamSourceProcess.StartInfo.WorkingDirectory = scriptPath.Directory.FullName;
            localStreamSourceProcess.StartInfo.UseShellExecute = false;
            localStreamSourceProcess.StartInfo.CreateNoWindow = true;

            // This should never throw an expeption, if at least the name of the 
            // shell(bash, cmd.exe) is correct.
            localStreamSourceProcess.Start();

            // Check, that the process does not die within one second
            bool exitedTooEarly = localStreamSourceProcess.WaitForExit( 1000 );
            
            if (exitedTooEarly)
            {
                logger?.LogCritical($"Process terminated immediately: '{shell} {argsForProcess}'");
                throw new WDCServiceException($"Process terminated immediately: '{shell} {argsForProcess}'");
            }
        }


        //
        // Summary:
        //   Stops streaming-source on the local-computer
        // Exceptions:
        //   T:SWirelessDisplayClient.Services.WDCServiceException:
        //     If the process could not be killed (should never occur)
        void IStreamSourceService.StopLocalStreaming()
        {
            if (localStreamSourceProcess != null && ! localStreamSourceProcess.HasExited)
            {
                localStreamSourceProcess.Kill( entireProcessTree : true );
            }
            if (localStreamSourceProcess != null)
            {
                localStreamSourceProcess.Dispose();
                localStreamSourceProcess = null;
            }
        }           

    }

}