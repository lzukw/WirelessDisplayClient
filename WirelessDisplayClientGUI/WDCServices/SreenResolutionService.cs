using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace WirelessDisplayClient.Services
{
    //
    // Summary:
    //     Class to run the external script for managing screenresolutions:
    //     Get current screen-resoltution, store initial screen-resolution in
    //     property, set screen-resolution and list all available 
    //     screen-resolutions.
    public class ScreenResolutionService : IScreenResolutionService
    {
        //
        // Readonly properties set by the constructor (dependency injection)
        //
        private readonly ILogger<ScreenResolutionService> logger;

        // The vales from the config passed to the constructor
        private readonly string shell;
        private readonly string shellArgsTemplate;
        private readonly string manageScreenResolutionsScriptPath;
        private readonly string manageScreenResolutionsScriptArgsTemplate;


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
        //   manageScreenResolutionsScriptPath:
        //     The file-path to the script to be executed when manipulating screen-resolutions
        //   manageScreenResolutionsScriptArgsTemplate:
        //     The command-line-arguments the script expects. Must contain the 
        //     placeholders %ACTION and %RESOLUTION.
        // Exceptions:
        //   T:System.IO.FileNotFoundException:
        //     manageScreenResolutionsScriptPath is not found.
        //   T:System.ArgumentException:
        //     shellArgsTemplate or manageScreenResolutionsScriptArgsTemplate do not contain the
        //     expected placeholders.
        public ScreenResolutionService(ILogger<ScreenResolutionService> logger, 
                                        string shell,
                                        string shellArgsTemplate,
                                        string manageScreenResolutionsScriptPath,
                                        string manageScreenResolutionsScriptArgsTemplate)
        {
            // logger and pathToScreenRes are injected by Dependcy-Injection 
            // by "the runtime" (Program.cs and Startup.cs).
            this.logger = logger;

            FileInfo scriptPath = new FileInfo( manageScreenResolutionsScriptPath );
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

            if ( ! manageScreenResolutionsScriptArgsTemplate.Contains("%ACTION") ||
                 ! manageScreenResolutionsScriptArgsTemplate.Contains("%RESOLUTION"))
            {
                logger?.LogCritical($"manageScreenResolutionsScriptArgsTemplate must contain %ACTION and %RESOLUTION, but is: {manageScreenResolutionsScriptArgsTemplate}");
                throw new ArgumentException($"manageScreenResolutionsScriptArgsTemplate must contain %ACTION and %RESOLUTION, but is: {manageScreenResolutionsScriptArgsTemplate}");
            }

            this.shell = shell;
            this.shellArgsTemplate = shellArgsTemplate;
            this.manageScreenResolutionsScriptPath = scriptPath.FullName;
            this.manageScreenResolutionsScriptArgsTemplate = manageScreenResolutionsScriptArgsTemplate;

            try 
            { 
                initialScreenResolution = runGetScreenResolution();
            }
            catch (WDCServiceException)
            {
                logger?.LogWarning("Couldn't get current screen-resoltution, to set initial screen-resolution. Scripting error?");
                initialScreenResolution ="???";
            }
        }


        // Backup-field for InitialScreenResolution
        private readonly string initialScreenResolution;

        //
        // Summary:
        //     The initial screen-resolution (a sting like "1024x768", without quotes)
        public string InitialScreenResolution { get => initialScreenResolution; }


        //
        // Summary:
        //     All available screen-resolutions as strings (for example '640x480') */
        public List<string> AvailableScreenResolutions 
        { 
            get 
            {
                List<string> availableScreenResolutions = new List<string>();

                string scriptArgs = manageScreenResolutionsScriptArgsTemplate;
                scriptArgs = scriptArgs.Replace("%ACTION", "ALL");
                scriptArgs = scriptArgs.Replace("%RESOLUTION", "null");

                List<string> outputLines;
                try 
                {
                    outputLines = runManageScreenResScript(scriptArgs);
                }
                catch(Exception)
                {
                    logger.LogWarning("Could not get available screen-resolutions. Scripting Error?");
                    availableScreenResolutions.Add("???");
                    return availableScreenResolutions;
                }
                
                foreach (string outputLine in outputLines)
                {
                    MatchCollection mc = Regex.Matches(outputLine, @"[^\d]*(\d+x\d+).*");
                    if (mc.Count != 1)
                    {
                        // Dismiss output-lines, that don't contain a scree-resolution.
                        continue;
                    }
                    Debug.Assert(mc[0].Groups.Count>=2);
                    string resolution = mc[0].Groups[1].ToString();
                    if ( ! availableScreenResolutions.Contains(resolution))
                    {
                        availableScreenResolutions.Add(resolution);
                    }
                }

                if (availableScreenResolutions.Count == 0)
                {
                    logger.LogWarning("Couldn't get any available screen-resolution. Scripting error?");
                    availableScreenResolutions.Add("???");
                }

                return availableScreenResolutions;
            }
        }

        //
        // Summary:
        //     The current screen-resolution (for example "1980x1040").
        public string CurrentScreenResolution
        {
            get
            {
                string outputLine;
                try
                {
                    outputLine = runGetScreenResolution();
                }
                catch (Exception)
                {
                    logger.LogWarning("Could not get current screen resolution.");
                    return "???";
                }
                return outputLine;
            }          
        }


        //
        // Summary:
        //     Set the screen-resolution.
        // Parmeters:
        //   screenResolution:
        //     A string containing the resolution to set, like "1024x768" 
        //     (wihtout quotes).
        public void SetScreenResolution(string screenResolution )
        {

            // Retrieve width and heigth information.
            MatchCollection mc = Regex.Matches(screenResolution, @"[^\d]*(\d+x\d+).*");
            if (mc.Count!=1 && mc[0].Groups.Count==1)
            {
                logger?.LogWarning($"SetScreenResolution called with invaild creen-resolution: '{screenResolution}'");
                return;
            }
            screenResolution = mc[0].Groups[0].ToString();

            string scriptArgs = manageScreenResolutionsScriptArgsTemplate;
            scriptArgs = scriptArgs.Replace("%ACTION", "SET");
            scriptArgs = scriptArgs.Replace ("%RESOLUTION", screenResolution);

            try
            {
                runManageScreenResScript(scriptArgs);
            }
            catch (Exception)
            {
                logger.LogWarning($"Could not screen-resolution to {screenResolution}");
            }
        }    


        //
        // Summary:
        //     Helper Method to get the screen-resolution
        private string runGetScreenResolution( int timeout = 10000 )
        {
            string scriptArgs = manageScreenResolutionsScriptArgsTemplate;
            scriptArgs = scriptArgs.Replace("%ACTION", "GET");
            scriptArgs = scriptArgs.Replace("%RESOLUTION", "null");

            List<string> outputLines = runManageScreenResScript(scriptArgs, timeout);

            //throw new WDSServiceException("ERROR: Script for getting local screen-resolution didn't return the expected value.");

            // Parse outputLines and return first line containing a screen-resolution
            foreach (string outputLine in outputLines)
            { 
                MatchCollection mc = Regex.Matches(outputLines[0], @"[^\d]*(\d+x\d+).*");
                if (mc.Count==1 && mc[0].Groups.Count==2)
                {
                    return mc[0].Groups[1].ToString();
                }
            }

            logger?.LogError("Could not parse a valid screen-resolution from script-output");
            throw new WDCServiceException("Could not parse a valid screen-resolution from script-output");
        } 

        //
        // Summary:
        //     Helper method to call the external program (screenres.exe)
        // Parameters:
        //   scriptArgs:
        //     The space separated arguments to pass to the script.
        // Returns:
        //   The lines written to stdout by the script.
        private List<string> runManageScreenResScript(string scriptArgs, int timeout = 10000)
        {
            FileInfo scriptPath = new FileInfo( manageScreenResolutionsScriptPath );
            if ( ! scriptPath.Exists )
            {
                logger?.LogError($"Script-file does not exist: '{scriptPath.FullName}'");
                throw new WDCServiceException($"Script-file does not exist: '{scriptPath.FullName}'");
            }

            List<string> outputLines = new List<string>();
            
            string argsForProcess = shellArgsTemplate;
            argsForProcess = argsForProcess.Replace("%SCRIPT", scriptPath.FullName);
            argsForProcess = argsForProcess.Replace("%ARGS", scriptArgs); 

            using (Process manageScreenResProcess = new Process())
            {
                manageScreenResProcess.StartInfo.FileName = shell;
                manageScreenResProcess.StartInfo.Arguments = argsForProcess;
                manageScreenResProcess.StartInfo.WorkingDirectory = scriptPath.Directory.FullName;
                manageScreenResProcess.StartInfo.UseShellExecute = false;
                manageScreenResProcess.StartInfo.CreateNoWindow = true;
                manageScreenResProcess.StartInfo.RedirectStandardOutput = true;
                manageScreenResProcess.StartInfo.RedirectStandardInput = true;

                try
                {
                    manageScreenResProcess.Start();
                }
                catch (Exception e)
                {
                    logger?.LogError($"Could not start script for managing screen-resoltuions: {e.Message}");
                    throw new WDCServiceException($"Could not start script for managing screen-resoltuions: {e.Message}");
                }

                //Wait until screenres-process exits.
                bool exited = manageScreenResProcess.WaitForExit(timeout);

                if (! exited )
                {
                    logger?.LogError($"Process not finished within {timeout} Milliseconds: '{shell} {argsForProcess}'. Scripting Error?");
                    throw new WDCServiceException($"Process not finished within {timeout} Milliseconds: '{shell} {argsForProcess}'. Scripting Error?");
                }               

                if (manageScreenResProcess.ExitCode != 0 )
                {
                    logger?.LogError($"Process failed with exit-code {manageScreenResProcess.ExitCode}: '{shell} {argsForProcess}'. Scripting Error?");
                    throw new WDCServiceException($"Process failed with exit-code {manageScreenResProcess.ExitCode}: '{shell} {argsForProcess}'. Scripting Error?");
                }

                string line;
                while ((line = manageScreenResProcess.StandardOutput.ReadLine()) != null)
                {
                    outputLines.Add(line);
                }

                logger.LogInformation($"Script '{scriptPath.Name} {scriptArgs}' returned successfully");

                return outputLines;
            }
        }


    }
}