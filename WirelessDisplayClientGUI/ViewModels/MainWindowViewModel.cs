﻿using System;
using System.Collections.Generic;
using WirelessDisplayClient.Services;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WirelessDisplayClient.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        ///////////////////////////////////////////////////////////////////////
        // Constructor and private Fields
        //////////////////////////////////////////////////////////////////////
        #region

        private readonly ILogger<MainWindowViewModel> logger;
        private readonly IScreenResolutionService screenResolutionService;
        private readonly IStreamSourceService streamSourceService;
        private readonly IRestApiClientService restApiClientService;
        private readonly int preferredScreenWidth;

        // Summary:
        //     Constructor.
        // Parameters:
        //   serviceProvider:
        //     An object that does all the work, , like communicating
        //     with the REST-API, setting the remote screen-resolution, and
        //     starting/stopping the local VNC-Server or FFmpeg.
        //     The serviceProvider is "injected" to the constructor.
        public MainWindowViewModel( ILogger<MainWindowViewModel> logger,
                  IScreenResolutionService screenResolutionService, 
                  IStreamSourceService streamSourceService, 
                  IRestApiClientService restApiClientService, 
                  int preferredScreenWidth )
        {
            this.logger = logger;
            this.screenResolutionService = screenResolutionService;
            this.streamSourceService = streamSourceService;
            this.restApiClientService = restApiClientService;
            this.preferredScreenWidth = preferredScreenWidth;
        }

        #endregion

        /////////////////////////////////////////////////////////////////////////////
        // Boolean properties for enabling and disabling controls of the MainWindow
        /////////////////////////////////////////////////////////////////////////////
        #region 

        // Backup-field for property ConnectionEstablished.
        private bool _connectionEstablished = false; 

        // Summary:
        //   ConnectionEstablished is used to enable/disable the "Connect"-
        //   and "Disconnect"-Button and the TextBox for the IP-Address.
        public bool ConnectionEstablished
        {
            get => _connectionEstablished;
            set => this.RaiseAndSetIfChanged(ref _connectionEstablished, value);
        }


        // Backup-field for property StreamStarted
        private bool _streamStarted = false;

        // Summary:
        //   StreamStarted is false after connecting and before starting the stream.
        //   It becomes true after starting the stream.
        public bool StreamStarted
        {
            get => _streamStarted;
            set => this.RaiseAndSetIfChanged(ref _streamStarted, value);
        }

        #endregion

        ///////////////////////////////////////////////////////////
        // Value-properties bound to controls 
        /////////////////////////////////////////////////////////// 
        #region

        //
        // Summary:
        //     Bound to TextBox with IP-Address
        private string _ipAddress="";
        public string IpAddress
        {
            get => _ipAddress;
            set => this.RaiseAndSetIfChanged(ref _ipAddress, value);
        }

        //
        // Summary:
        //     Bound to TextBlock containing initial local screen-resolution
        private string _initialLocalScreenResolution="";
        public string InitialLocalScreenResolution
        {
            get => _initialLocalScreenResolution;
            set => this.RaiseAndSetIfChanged(ref _initialLocalScreenResolution, value);
        }

        //
        // Summary:
        //     Bound to TextBlock containing current local screen-resolution
        private string _currentLocalScreenResolution="";
        public string CurrentLocalScreenResolution
        {
            get => _currentLocalScreenResolution;
            set => this.RaiseAndSetIfChanged(ref _currentLocalScreenResolution, value);
        }

        //
        // Summary:
        //     Bound to TextBlock containing initial remote screen-resolution
        private string _initialRemoteScreenResolution="";
        public string InitialRemoteScreenResolution
        {
            get => _initialRemoteScreenResolution;
            set => this.RaiseAndSetIfChanged(ref _initialRemoteScreenResolution, value);
        }

        //
        // Summary:
        //     Bound to TextBlock containing current remote screen-resolution
        private string _currentRemoteScreenResolution="";
        public string CurrentRemoteScreenResolution
        {
            get => _currentRemoteScreenResolution;
            set => this.RaiseAndSetIfChanged(ref _currentRemoteScreenResolution, value);
        }

        //
        // Summary:
        //     Bound to items of the ComboBox with availabe screen-resolutions on
        //     the local computer.
        public ObservableCollection<string> AvailableLocalScreenResolutions { get; } = new ObservableCollection<string>();

        //
        // Summary:
        //     Bound to the index of the selected item in the ComboBox with availabe 
        //     screen-resolutions on the local computer (-1 if no item is selected).
        private int _selectedLocalScreenResolutionIndex = -1;
        public int SelectedLocalScreenResolutionIndex
        {
            get => _selectedLocalScreenResolutionIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedLocalScreenResolutionIndex, value);
        }

        //
        // Summary:
        //     Bound to items of the ComboBox with availabe screen-resolutions on
        //     the remote computer.
        public ObservableCollection<string> AvailableRemoteScreenResolutions { get; } = new ObservableCollection<string>();

        //
        // Summary:
        //     Bound to the index of the selected item in the ComboBox with availabe 
        //     screen-resolutions on the remote computer (-1 if no item is selected).
        private int _selectedRemoteScreenResolutionIndex = -1;
        public int SelectedRemoteScreenResolutionIndex
        {
            get => _selectedRemoteScreenResolutionIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedRemoteScreenResolutionIndex, value);
        }

        //
        // Summary:
        //     Bound to the IsSelected-Attribute of the "VNC"-Radio-Button
        private bool _vncSelected = true;
        public bool VncSelected 
        {
            get => _vncSelected;
            set => this.RaiseAndSetIfChanged(ref _vncSelected, value);
        }

        //
        // Summary:
        //     Bound to the IsSelected-Attribute of the "FFmpeg"-Radio-Button
        private bool _ffmpegSelected = false;
        public bool FFmpegSelected 
        {
            get => _ffmpegSelected;
            set => this.RaiseAndSetIfChanged(ref _ffmpegSelected, value);
        }

        //
        // Summary:
        //     Bound to the value of the NumericUpDown for the port-number
        private UInt16 _portNo = 5500;
        public UInt16 PortNo
        {
            get => _portNo;
            set => this.RaiseAndSetIfChanged(ref _portNo, value);
        }

        //
        // Summary:
        //     Bound to the status-text
        private string _statusText="Ready to connect...\n";
        public string StatusText
        {
            get => _statusText;
            set => this.RaiseAndSetIfChanged(ref _statusText, value);
        }

        #endregion
 
        ///////////////////////////////////////////////////////////
        // React on Button-Clicks and Window-Close-Button. 
        ///////////////////////////////////////////////////////////
        #region

        //
        // Summary: 
        //     React on Connect-Button. 
        public async Task ButtonConnect_Click()
        {
            // Sanity Check: Textbox for IP-Address emtpy?
            if (String.IsNullOrEmpty(IpAddress))
            {
                StatusText += "WARNING: Please enter the IP-Address of the projecting-computer before connecting!\n";
                return; // bail out
            }

            // Try to connect.
            try
            { 
                await restApiClientService.Connect(IpAddress);
            }
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText += "Maybe wrong IP-Address? Or WirelessDisplayServer not started on projecting-computer?\n";
                return; // bail out
            }

            // Get the initial screen-resolution of the local computer.
            try
            {
                InitialLocalScreenResolution = 
                        screenResolutionService.InitialScreenResolution;
            }       
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText += "ERROR: Could not get initial local screen-resolution! Scripting Error?\n";
            }

            // Get the initial screen-resolution of the remote computer.
            try
            {
                InitialRemoteScreenResolution = await 
                        restApiClientService.GetInitialRemoteScreenResolution();
            }       
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText += "WARNING: Could not get initial remote screen-resolution! Connection Lost?\n";
            }

            // Get the current screen-resolution of the local computer.
            try
            {
                CurrentLocalScreenResolution =  
                        screenResolutionService.CurrentScreenResolution;
            }
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText += "ERROR: Could not get local current screen-resolution! Scripting Error?\n";
            }

            // Get the current screen-resolution of the remote computer.
            try
            {
                CurrentRemoteScreenResolution = await 
                        restApiClientService.GetCurrentRemoteScreenResolution();
            }
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText += "WARNING: Could not get current screen-resolution! Connection Lost?\n";
            }

            // Fill the ComboBox-items with the available local screen-resolutions
            try 
            {
                List<string> resolutions =  screenResolutionService.AvailableScreenResolutions;
                SelectedLocalScreenResolutionIndex = -1;
                AvailableLocalScreenResolutions.Clear();
                foreach (string res in resolutions)
                {
                    AvailableLocalScreenResolutions.Add(res);
                }
            }
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText += "ERROR: Could not get available local screen-resolutions! Scripting Error?\n";
            }

            // Pre-select moderate screen-resolution for remote computer and streaming
            SelectedLocalScreenResolutionIndex = 
                        indexOfNearestResolution(AvailableLocalScreenResolutions);

            // Fill the ComboBox-items with the available remote screen-resolutions
            try 
            {
                List<string> resolutions = await 
                        restApiClientService.GetAvailableRemoteScreenResolutions();
                SelectedRemoteScreenResolutionIndex = -1;
                AvailableRemoteScreenResolutions.Clear();
                foreach (string res in resolutions)
                {
                    AvailableRemoteScreenResolutions.Add(res);
                }
            }
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText += "WARNING: Could not get available remote screen-resolutions! Connection Lost?\n";
            }

            // Pre-select moderate screen-resolution for remote computer and streaming
            SelectedRemoteScreenResolutionIndex = 
                        indexOfNearestResolution(AvailableRemoteScreenResolutions);

            // Finally switch the Window-State:
            ConnectionEstablished = true;
            StatusText += $"Successfully connected to {restApiClientService.LastKnownRemoteIP}\n";
        }

        //
        // Summary:
        //     React on Start-Streaming-Button
        public async Task ButtonStartStreaming_click()
        {  
            // First set local Screen Resolution:
            if (AvailableLocalScreenResolutions.Count > 0 && 
                            SelectedLocalScreenResolutionIndex != -1 )
            {
                string localResolution = 
                        AvailableLocalScreenResolutions[SelectedLocalScreenResolutionIndex];
                try
                {
                    screenResolutionService.SetScreenResolution(localResolution);
                    StatusText += $"Successfully set local screen-resolution to {localResolution}\n";
                    CurrentLocalScreenResolution = localResolution;
                }
                catch (WDCServiceException e)
                {
                    StatusText += $"{e.Message}\n";
                    StatusText += "ERROR: Could not set local screen-resolution. Scripting error?";
                }
            }

            // Then set remote Screen Resolution:
            string remoteResolution = null;
            if (AvailableRemoteScreenResolutions.Count > 0 && 
                            SelectedRemoteScreenResolutionIndex != -1 )
            {
                remoteResolution = 
                        AvailableRemoteScreenResolutions[SelectedRemoteScreenResolutionIndex];
                try
                {
                    await restApiClientService.SetRemoteScreenResolution(remoteResolution);
                    StatusText += $"Successfully set remote screen-resolution to {remoteResolution}\n";
                    CurrentRemoteScreenResolution = remoteResolution;
                }
                catch (WDCServiceException e)
                {
                    StatusText += $"{e.Message}\n";
                    StatusText += "WARNING: Could not set remote screen-resolution. Connection lost?";
                }
            }

            // Then stop eventually still ongoing streaming
            try 
            {
                await restApiClientService.StopRemoteStreamingSink();
                StatusText += "Sucessfully stopped eventually still running streaming.\n";
            }
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText += "ERROR: Could not stop eventually still running remote streaming-sink.\n"; 
                // Don't bail out here, just try to start the reomte streamin-sink first.
            }

            // Check if VNC or FFmpeg is desired:
            StreamType streamType;
            if (VncSelected)
            {
                streamType = StreamType.VNC;
            }
            else if (FFmpegSelected)
            {
                 streamType = StreamType.FFmpeg;   
            }
            else
            {
                logger?.LogCritical("BUG: Neither VNC nor FFmpeg selected. This should not be possible.");
                throw new Exception("BUG: Neither VNC nor FFmpeg selected. This should not be possible.");
            }  

            // Then start selected streaming-sink on remote computer, bailing out if unsuccessfull
            try
            {
                await restApiClientService.StartRemoteStreamingSink(streamType, PortNo);
                StatusText += $"Successfully started remote streaming-sink of type {streamType.ToString()} on {restApiClientService.LastKnownRemoteIP}:{PortNo}\n";
            }
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText += "ERROR: Could not start remote streaming-sink.\n"; 
                // Bail out
                return;
            }

            // Then start streaming-source on local computer, bailing out if unsuccessfull
            try
            {
                streamSourceService.StartLocalStreamSource( streamType,
                                                            IpAddress,
                                                            PortNo,
                                                            streamResolution : remoteResolution );
                StatusText += $"Successfully started local streaming-source of type {streamType.ToString()}\n";
            }
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText += "ERROR: Could not start local streaming-source.\n";
                try
                {
                    await restApiClientService.StopRemoteStreamingSink();
                     StatusText += $"Stopped remote streaming-sink again.\n";
                } 
                catch(WDCServiceException e2)
                {
                    StatusText += $"{e2.Message}\n";
                    StatusText += "ERROR: Could not stop remote sink again.\n";
                    return; // Bail out
                }
                return; // Bail out
            }

            // Last, preselect initial screen-resolutions
            SelectedLocalScreenResolutionIndex = 
                    indexOfResolution( AvailableLocalScreenResolutions, 
                                       InitialLocalScreenResolution);

            SelectedRemoteScreenResolutionIndex = 
                    indexOfResolution( AvailableRemoteScreenResolutions, 
                                       InitialRemoteScreenResolution);

            // and switch the window-state
            ConnectionEstablished = true;
            StreamStarted = true;
        }


        //
        // Summary:
        //     React on Stop-Streaming-Button
        public async Task ButtonStopStreaming_click()
        {
            // First stop local streaming-source
            // Only the underlying call to Process.kill() could throw an exception,
            // but this should never occur.
            streamSourceService.StopLocalStreaming();
            StatusText += "Successfully stopped local streaming-source.\n";
            
            // Then stop remote streaming-sink
            try 
            {
                await restApiClientService.StopRemoteStreamingSink();
                StatusText += "Successfully stopped remote streaming-sink.\n";
            }
            catch (WDCServiceException e)
            {
                StatusText += $"{e.Message}\n";
                StatusText = "ERROR: Could not stop streaming.\n";
                // Don't bail out, since local streaming-source has already been
                // stopped. But this leaves the remote streaming-sink running!
            }

            // Then set local Screen Resolution:
            if (AvailableLocalScreenResolutions.Count > 0  && 
                            SelectedLocalScreenResolutionIndex != -1)
            {
                string localResolution = 
                        AvailableLocalScreenResolutions[SelectedLocalScreenResolutionIndex];
                try {
                    screenResolutionService.SetScreenResolution(localResolution);
                    StatusText += $"Successfully set local screen-resolution to {localResolution}\n";
                    CurrentLocalScreenResolution = localResolution;
                }
                catch (WDCServiceException e)
                {
                    StatusText += $"{e.Message}\n";
                    StatusText += $"WARNING: Could not set local screen-resolution to {localResolution}\n";
                }
            }

            // Then set remote Screen Resolution:
            if (AvailableRemoteScreenResolutions.Count > 0  &&
                            SelectedRemoteScreenResolutionIndex != -1 )
            {
                string remoteResolution = 
                        AvailableRemoteScreenResolutions[SelectedRemoteScreenResolutionIndex];
                try {
                    await restApiClientService.SetRemoteScreenResolution(remoteResolution);
                    StatusText += $"Successfully tried to set remote screen-resolution to {remoteResolution}\n";
                    CurrentRemoteScreenResolution = remoteResolution;
                }
                catch (WDCServiceException e)
                {
                    StatusText += $"{e.Message}\n";
                    StatusText += $"WARNING: Could not set remote screen-resolution to {remoteResolution}\n";
                }
            }

            // Finally preselect moderate screen-resolutions, for next streaming
            SelectedLocalScreenResolutionIndex = 
                    indexOfNearestResolution( AvailableLocalScreenResolutions );

            SelectedRemoteScreenResolutionIndex = 
                    indexOfNearestResolution( AvailableRemoteScreenResolutions );

            // and switch the window-state
            ConnectionEstablished = true;
            StreamStarted = false;
        }

        //
        // Summary:
        //     React on the Disconnect-Button
        public async Task ButtonDisconnect_Click()
        {
            // First shut down streaming, if it is started.
            if (StreamStarted)
            {
                await ButtonStopStreaming_click();
            }

            // Finally switch the window-state
            ConnectionEstablished = false;
            StreamStarted = false;
            StatusText += "Disconnected.\n";
        }

        //
        // Summary:
        //     Executed, when the user uses the (X)-Button in the title-bar
        //     of the Main-Window. Shut down the connection before closing
        //     the application.
        public async Task OnWindowClose()
        {
            if (ConnectionEstablished)
            {
                await ButtonDisconnect_Click();
            }
        }

        #endregion
 
        ///////////////////////////////////////////////////////////
        // Helper methods
        ///////////////////////////////////////////////////////////
        #region

        //
        // Summary:
        //     Searches all provided screen-resolutions, and returns the index of 
        //     the one, whose width is nearest to desiredWidth. 
        // Parameters:
        //   resolutions:
        //     The list of screen-resolutions to search.
        //   desiredWidth:
        //     The screen-resolution to find, or the nearest available one. If no
        //     value is passed to this field, the desiredWidth is taken from the
        //     value passed via config to the constructor of this class.
        //   Returns:
        //     The index of the found resolution.
        private int indexOfNearestResolution( IEnumerable<string> resolutions,
                                              int desiredWidth = -1)
        {
            // Replace default value
            if (desiredWidth == -1)
            {
                desiredWidth = preferredScreenWidth;
            }

            int index = 0;
            int indexToSelect=-1; // worst-case: select none
            int smallestDeviation = Int32.MaxValue;

            foreach (string res in resolutions)
            {
                int width = Convert.ToInt32(res.Split('x')[0]);
                if ( Math.Abs(width-desiredWidth) <= smallestDeviation)
                {
                    smallestDeviation = Math.Abs(width-desiredWidth);
                    indexToSelect = index;
                }
                index++;
            }

            return indexToSelect;
        }

        //
        // Summary:
        //     Searches all provided screen-resolutions and returns the index
        //     the one that is given by resolutionToSelect.
        // Parameters:
        //   resolutions:
        //     The list of screen-resolutions to search.
        //   resolutionToSelect:
        //     The screen-resolution to find
        //   Returns:
        //     The index of the found resolution. If resolutionToSelect was
        //     not found, -1 is returned.
        private int indexOfResolution(IEnumerable<string> resolutions,
                                       string resolutionToSelect)
        {
            // Forgot LINQ, so search manually.

            int index = 0;

            foreach (string res in resolutions)
            {
                if (res == resolutionToSelect)
                {
                    return index;
                }
                index++;
            }

            return -1; // not found
        }

        #endregion
        
    }
}
